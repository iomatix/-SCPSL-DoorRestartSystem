using DoorRestartSystem.Shared.Audio;
using DoorRestartSystem.Shared.Audio.Enums;
using DoorRestartSystem.Shared.Runtime;
using Interactables.Interobjects.DoorUtils;
using LabApi.Extensions;
using LabApi.Extensions.Misc;
using LabApi.Features.Wrappers;
using MapGeneration;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = LabApi.Extensions.Misc.iLogger;

namespace DoorRestartSystem
{
    /// <summary>
    /// Type-safe LabAPI-compliant manager leveraging structural RoomName configurations 
    /// and pre-filtered lockdown context pipelines to execute zero-allocation facility isolation.
    /// </summary>
    public class Methods
    {
        #region Private Repositories & Execution States
        private readonly Plugin _plugin;
        private readonly Config _config;
        private readonly DrsAudioManager _audioManager;

        private readonly HashSet<RoomName> _roomsToSkip = new HashSet<RoomName>();
        private readonly Dictionary<Room, RoomLockdownContext> _roomContextCache = new Dictionary<Room, RoomLockdownContext>();
        private readonly Dictionary<int, RoomLockdownContext> _affectedRoomsMap = new Dictionary<int, RoomLockdownContext>();
        private readonly HashSet<FacilityZone> _triggeredZones = new HashSet<FacilityZone>();
        private readonly Dictionary<int, int> _roomSirenSessions = new Dictionary<int, int>();
        private readonly List<int> _activeBuzzSessions = new List<int>();

        private enum CassieStatus
        {
            Idle,
            Playing,
            Cooldown
        }

        private CassieStatus _cassieState = CassieStatus.Idle;
        #endregion

        #region Public State Properties
        /// <summary>
        /// Indicates whether the automated background lockdown timer loop is actively running.
        /// </summary>
        public bool IsSystemActive { get; private set; }

        /// <summary>
        /// Indicates whether a physical lockdown sequence is actively being executed.
        /// </summary>
        public bool IsLockdownInProgress { get; private set; }
        #endregion

        #region Initialization
        public Methods(Plugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Structural runtime allocation failure: Parent plugin context evaluates to null.");
            _config = _plugin.Config;
            _audioManager = new DrsAudioManager(_plugin);
        }
        #endregion

        #region Subsystem Lifecycle Management
        public void Init()
        {
            if (!_config.IsEnabled)
            {
                Logger.Info(nameof(Methods), "DoorRestartSystem is disabled via configuration.");
                return;
            }

            InitializeRoomSkipList();
            BuildRoomContextCache();
            Logger.Info(nameof(Methods), "DoorRestartSystem successfully initialized and prepared for round engagement.");
        }

        public void Clean()
        {
            IsSystemActive = false;
            IsLockdownInProgress = false;

            DrsRegistry.FlushAll();
            ForceResetFacilityState();

            _roomContextCache.Clear();
            _roomsToSkip.Clear();
            _triggeredZones.Clear();

            Logger.Info(nameof(Methods), "DoorRestartSystem internal execution tracks successfully flushed.");
        }

        private void InitializeRoomSkipList()
        {
            _roomsToSkip.Clear();
            _roomsToSkip.Add(RoomName.Lcz914);

            if (_config.SkipNukeDoors) _roomsToSkip.Add(RoomName.HczWarhead);
            if (_config.SkipAirlocks) _roomsToSkip.Add(RoomName.LczAirlock);

            foreach (RoomName name in Enum.GetValues(typeof(RoomName)))
            {
                if (_config.SkipSCPRooms && name.IsScpRoom()) _roomsToSkip.Add(name);
                if (_config.SkipArmory && name.IsArmory()) _roomsToSkip.Add(name);
                if (_config.SkipCheckpoints && name.IsCheckpoint()) _roomsToSkip.Add(name);
            }
        }

        private void BuildRoomContextCache()
        {
            _roomContextCache.Clear();

            foreach (Room room in Room.List)
            {
                if (room != null && !_roomContextCache.ContainsKey(room))
                {
                    _roomContextCache.Add(room, new RoomLockdownContext(room, _config.SkipCheckpointsGate, _config.SkipElevators));
                }
            }
        }
        #endregion

        #region Core Timing Loops & Manual Overrides
        public IEnumerator<float> StartLockdownTimer()
        {
            if (!_config.IsEnabled) yield break;

            IsSystemActive = true;

            float initialTimer = 0f;
            while (initialTimer < _config.InitialDelay && IsSystemActive)
            {
                yield return Timing.WaitForSeconds(1f);
                initialTimer += 1f;
            }

            while (IsSystemActive)
            {
                float delay = _config.RandomEvents
                    ? SafeRandom.Range(_config.DelayMin, _config.DelayMax)
                    : _config.InitialDelay;

                float loopTimer = 0f;
                while (loopTimer < delay && IsSystemActive)
                {
                    yield return Timing.WaitForSeconds(1f);
                    loopTimer += 1f;
                }

                if (!IsSystemActive) yield break;

                yield return Timing.WaitUntilTrue(() => !Warhead.IsDetonated && !Warhead.IsDetonationInProgress);

                if (!IsSystemActive) yield break;

                yield return Timing.WaitUntilFalse(() => IsLockdownInProgress);

                if (!IsSystemActive) yield break;

                CoroutineHandle execHandle = Timing.RunCoroutine(ExecuteLockdownPipeline(null, false, null, null), DrsRegistry.ExecutionTag);
                yield return Timing.WaitUntilDone(execHandle);
            }
        }

        public void ForceManualLockdown(float? customDuration, FacilityZone? targetZone = null, RoomName? targetRoom = null)
        {
            if (_roomsToSkip.Count == 0)
                InitializeRoomSkipList();

            if (_roomContextCache.Count == 0)
                BuildRoomContextCache();

            InterruptActivePipelines();
            ForceResetFacilityState();

            float duration = customDuration ?? GetRandomLockdownDuration();
            Timing.RunCoroutine(ExecuteLockdownPipeline(duration, true, targetZone, targetRoom), DrsRegistry.ExecutionTag);
        }

        public void ForceStopLockdown()
        {
            IsSystemActive = false;
            IsLockdownInProgress = false;

            InterruptActivePipelines();
            CassieExtensions.CassieClear();
            ForceResetFacilityState();
        }

        private void InterruptActivePipelines()
        {
            IsLockdownInProgress = false;
            DrsRegistry.KillLockdownPipelines();

            int buzzCount = _activeBuzzSessions.Count;
            for (int i = 0; i < buzzCount; i++)
            {
                _audioManager.StopSession(_activeBuzzSessions[i]);
            }
            _activeBuzzSessions.Clear();
        }
        #endregion

        #region Operational Lockdown Engine
        private IEnumerator<float> ExecuteLockdownPipeline(
            float? customDuration,
            bool skipCountdown,
            FacilityZone? targetZone,
            RoomName? targetRoom)
        {
            IsLockdownInProgress = true;

            HashSet<Room> targets = new HashSet<Room>();
            List<string> announcementParts = new List<string>();

            if (targetRoom.HasValue)
            {
                bool messageAdded = false;
                foreach (Room room in Room.List)
                {
                    if (room.Name == targetRoom.Value && !IsRoomSkipped(room))
                    {
                        targets.Add(room);
                        if (!messageAdded)
                        {
                            announcementParts.Add(GetZoneSettings(room.Zone).Message);
                            messageAdded = true;
                        }
                    }
                }
            }
            else if (targetZone.HasValue)
            {
                announcementParts.Add(GetZoneSettings(targetZone.Value).Message);
                foreach (Room room in Room.List.Where(r => r.Zone == targetZone.Value && !IsRoomSkipped(r)))
                {
                    targets.Add(room);
                }
            }
            else
            {
                if (_config.UsePerRoomChances)
                    EvaluatePerRoomLockdown(targets, announcementParts);
                else
                    EvaluatePerZoneLockdown(targets, announcementParts);
            }

            if (targets.Count == 0 && _config.EnableFacilityLockdown && !targetRoom.HasValue && !targetZone.HasValue)
            {
                announcementParts.Add(_config.CassieMessageFacility);
                foreach (Room room in Room.List)
                {
                    if (!IsRoomSkipped(room)) targets.Add(room);
                }
            }

            if (targets.Count > 0)
            {
                if (_config.CassieMessageClearBeforeImportant)
                {
                    CassieExtensions.CassieClear();
                }

                if (_config.IsCountdownEnabled && !skipCountdown)
                {
                    double countdownDuration = TriggerCassieMessage(_config.CassieMessageCountdown, isGlitchy: true, force: true);
                    yield return Timing.WaitForSeconds((float)countdownDuration + 0.5f);
                }

                string combinedPhrase = $"{_config.CassieMessageStart} {string.Join(" ", announcementParts)}";
                TriggerCassieMessage(combinedPhrase, force: true);

                float duration = customDuration ?? GetRandomLockdownDuration();
                List<RoomLockdownContext> activeContexts = ApplyFacilityLockdown(targets, duration);

                if (activeContexts.Count > 0)
                {
                    if (_config.Flicker)
                    {
                        Color lockdownColor = new Color(_config.LightsColorR, _config.LightsColorG, _config.LightsColorB);
                        int contextCount = activeContexts.Count;

                        for (int i = 0; i < contextCount; i++)
                        {
                            int buzzId = _audioManager.PlayAtPosition(DrsAudioKey.ElectricalBuzzLoop, activeContexts[i].Room.Position, loop: true, customLifespan: duration);
                            if (buzzId != 0)
                                _activeBuzzSessions.Add(buzzId);
                        }

                        targets.FlickerLights(lockdownColor, duration, _config.FlickerFrequency, DrsRegistry.FlickerTag);
                    }

                    yield return Timing.WaitForSeconds(duration);

                    TriggerCassieMessage(_config.CassieMessageEnd, isGlitchy: true, force: true);
                    _audioManager.PlayGlobal(DrsAudioKey.LockdownReleaseGlobal);

                    Timing.KillCoroutines(DrsRegistry.FlickerTag);

                    int buzzCount = _activeBuzzSessions.Count;
                    for (int i = 0; i < buzzCount; i++)
                    {
                        _audioManager.StopSession(_activeBuzzSessions[i]);
                    }
                    _activeBuzzSessions.Clear();

                    ReleaseActiveLockdowns(activeContexts);
                    HandlePostLockdownChaos(activeContexts);
                }
            }
            else
            {
                TriggerCassieMessage(_config.CassieMessageWrong, isGlitchy: true);
            }

            IsLockdownInProgress = false;
        }

        private List<RoomLockdownContext> ApplyFacilityLockdown(HashSet<Room> targets, float duration)
        {
            List<RoomLockdownContext> activeContexts = new List<RoomLockdownContext>();
            HashSet<Door> processedDoors = new HashSet<Door>();
            HashSet<Elevator> processedElevators = new HashSet<Elevator>();
            List<Door> doorsToLock = new List<Door>();

            foreach (Room room in targets)
            {
                if (!_roomContextCache.TryGetValue(room, out RoomLockdownContext context))
                {
                    context = new RoomLockdownContext(room, _config.SkipCheckpointsGate, _config.SkipElevators);
                    _roomContextCache[room] = context;
                }

                room.SetLightsColor(new Color(_config.LightsColorR, _config.LightsColorG, _config.LightsColorB));

                if (!_roomSirenSessions.ContainsKey(context.RoomInstanceId))
                {
                    int sirenId = _audioManager.PlayAtPosition(DrsAudioKey.LockdownSirenLoop, room.Position + new Vector3(0f, 3.5f, 0f), loop: true, customLifespan: duration);
                    if (sirenId != 0)
                        _roomSirenSessions[context.RoomInstanceId] = sirenId;
                }

                _affectedRoomsMap[context.RoomInstanceId] = context;
                activeContexts.Add(context);

                int normalDoorCount = context.NormalDoors.Length;
                for (int i = 0; i < normalDoorCount; i++)
                {
                    Door door = context.NormalDoors[i];
                    if (door == null || !processedDoors.Add(door))
                        continue;

                    if (_config.UsePerDoorChance)
                    {
                        if (_config.ChancePerDoor.RollChance())
                        {
                            doorsToLock.Add(door);
                            _audioManager.PlayAtPosition(DrsAudioKey.MechanicalLockSlam, door.Position);
                        }
                    }
                    else
                    {
                        doorsToLock.Add(door);
                    }
                }

                if (!_config.UsePerDoorChance && normalDoorCount > 0)
                {
                    _audioManager.PlayAtPosition(DrsAudioKey.MechanicalLockSlam, room.Position);
                }

                int elevatorCount = context.ConnectedElevators.Length;
                for (int i = 0; i < elevatorCount; i++)
                {
                    Elevator elevator = context.ConnectedElevators[i];
                    if (elevator == null || !processedElevators.Add(elevator))
                        continue;

                    if (_config.CloseDoors)
                    {
                        elevator.CloseActiveDoors(bypassLocks: true);
                    }
                    elevator.Doors.SetLockState(DoorLockReason.Isolation, true);
                }
            }

            if (doorsToLock.Count > 0)
            {
                if (_config.CloseDoors)
                    doorsToLock.Close();

                doorsToLock.SetLockState(DoorLockReason.Isolation, true);
            }

            return activeContexts;
        }

        private void ReleaseActiveLockdowns(List<RoomLockdownContext> contexts)
        {
            HashSet<Door> releasedDoors = new HashSet<Door>();
            HashSet<Elevator> releasedElevators = new HashSet<Elevator>();
            List<Door> doorsToUnlock = new List<Door>();

            int count = contexts.Count;
            for (int i = 0; i < count; i++)
            {
                RoomLockdownContext context = contexts[i];
                if (context == null) continue;

                if (_roomSirenSessions.TryGetValue(context.RoomInstanceId, out int sirenId))
                {
                    _audioManager.StopSession(sirenId);
                    _roomSirenSessions.Remove(context.RoomInstanceId);
                }

                context.Room.SetLightsColor(Color.clear);

                int normalDoorCount = context.NormalDoors.Length;
                for (int d = 0; d < normalDoorCount; d++)
                {
                    Door door = context.NormalDoors[d];
                    if (door != null && releasedDoors.Add(door))
                    {
                        doorsToUnlock.Add(door);
                    }
                }

                int elevatorCount = context.ConnectedElevators.Length;
                for (int e = 0; e < elevatorCount; e++)
                {
                    Elevator elevator = context.ConnectedElevators[e];
                    if (elevator != null && releasedElevators.Add(elevator))
                    {
                        elevator.Doors.SetLockState(DoorLockReason.Isolation, false);
                    }
                }

                _affectedRoomsMap.Remove(context.RoomInstanceId);
            }

            if (doorsToUnlock.Count > 0)
            {
                doorsToUnlock.SetLockState(DoorLockReason.Isolation, false);
            }
        }
        #endregion

        #region Probability Evaluation Engines
        private void EvaluatePerZoneLockdown(HashSet<Room> targetRooms, List<string> announcementParts)
        {
            FacilityZone[] zones = { FacilityZone.HeavyContainment, FacilityZone.LightContainment, FacilityZone.Entrance, FacilityZone.Surface };

            foreach (FacilityZone zone in zones)
            {
                var (chance, message) = GetZoneSettings(zone);
                if (!chance.RollChance()) continue;

                if (!string.IsNullOrWhiteSpace(message)) announcementParts.Add(message);

                foreach (Room room in Room.List.Where(r => r.Zone == zone && !IsRoomSkipped(r)))
                {
                    targetRooms.Add(room);
                }
            }
        }

        private void EvaluatePerRoomLockdown(HashSet<Room> targetRooms, List<string> announcementParts)
        {
            _triggeredZones.Clear();

            foreach (Room room in Room.List)
            {
                if (IsRoomSkipped(room)) continue;

                var (chance, message) = GetZoneSettings(room.Zone);
                if (!chance.RollChance()) continue;

                targetRooms.Add(room);
                if (_triggeredZones.Add(room.Zone) && !string.IsNullOrWhiteSpace(message) && !announcementParts.Contains(message))
                {
                    announcementParts.Add(message);
                }
            }
        }
        #endregion

        #region State Reset & Post-Lockdown Handlers
        private void ForceResetFacilityState()
        {
            HashSet<Door> resetDoors = new HashSet<Door>();
            HashSet<Elevator> resetElevators = new HashSet<Elevator>();
            List<Door> doorsToUnlock = new List<Door>();

            foreach (RoomLockdownContext context in _affectedRoomsMap.Values)
            {
                if (context == null) continue;

                context.Room.SetLightsColor(Color.clear);

                int doorCount = context.NormalDoors.Length;
                for (int i = 0; i < doorCount; i++)
                {
                    Door door = context.NormalDoors[i];
                    if (door != null && resetDoors.Add(door))
                    {
                        doorsToUnlock.Add(door);
                    }
                }

                int elevatorCount = context.ConnectedElevators.Length;
                for (int i = 0; i < elevatorCount; i++)
                {
                    Elevator elevator = context.ConnectedElevators[i];
                    if (elevator != null && resetElevators.Add(elevator))
                    {
                        elevator.Doors.SetLockState(DoorLockReason.Isolation, false);
                    }
                }
            }

            if (doorsToUnlock.Count > 0)
            {
                doorsToUnlock.SetLockState(DoorLockReason.Isolation, false);
            }

            _affectedRoomsMap.Clear();

            int buzzCount = _activeBuzzSessions.Count;
            for (int i = 0; i < buzzCount; i++)
            {
                _audioManager.StopSession(_activeBuzzSessions[i]);
            }
            _activeBuzzSessions.Clear();

            foreach (var kvp in _roomSirenSessions.ToList())
            {
                _audioManager.StopSession(kvp.Value);
            }
            _roomSirenSessions.Clear();

            _audioManager.Clean();
        }

        private void HandlePostLockdownChaos(List<RoomLockdownContext> affectedContexts)
        {
            if (!_config.OpenDoorsAfterLockdown || !_config.OpenDoorsChance.RollChance())
                return;

            HashSet<Door> openedDoors = new HashSet<Door>();
            HashSet<Elevator> openedElevators = new HashSet<Elevator>();
            List<Door> openableDoors = new List<Door>();

            int contextCount = affectedContexts.Count;
            for (int i = 0; i < contextCount; i++)
            {
                RoomLockdownContext context = affectedContexts[i];
                if (_config.OpenOnlyCheckpoints && !context.Room.Name.IsCheckpoint())
                    continue;

                int normalDoorCount = context.NormalDoors.Length;
                for (int d = 0; d < normalDoorCount; d++)
                {
                    Door door = context.NormalDoors[d];
                    if (door != null && !door.IsLocked && openedDoors.Add(door))
                    {
                        openableDoors.Add(door);
                    }
                }

                int elevatorCount = context.ConnectedElevators.Length;
                for (int e = 0; e < elevatorCount; e++)
                {
                    Elevator elevator = context.ConnectedElevators[e];
                    if (elevator != null && openedElevators.Add(elevator))
                    {
                        elevator.OpenActiveDoors(bypassLocks: false);
                    }
                }
            }

            if (openableDoors.Count > 0)
            {
                openableDoors.Open();
            }

            Logger.Debug(nameof(Methods), "Post-lockdown phase completed. Mechanically forced regular doors and active elevator levels to restore baseline states.", _plugin.Debug);
        }
        #endregion

        #region Radio Broadcast State Machine
        private double TriggerCassieMessage(string message, bool isGlitchy = false, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Logger.Debug(nameof(Methods), "CASSIE message configuration evaluates to null or empty space, skipping.", _plugin.Debug);
                return 0.0;
            }

            if (!force && _cassieState != CassieStatus.Idle)
            {
                Logger.Debug(nameof(Methods), $"Vocal pipeline transmission blocked: State machine busy. Status: [{_cassieState}]. Dropping phrase.", _plugin.Debug);
                return 0.0;
            }

            _cassieState = CassieStatus.Playing;
            Logger.Debug(nameof(Methods), $"Vocal pipeline initialized. Transmitting phrase payload: {message}", _plugin.Debug);

            if (_config.CassieMessageClearBeforeImportant)
                CassieExtensions.CassieClear();

            double duration = isGlitchy
                ? CassieExtensions.DispatchGlitchyMessage(message, _config.GlitchChance, _config.JamChance)
                : CassieExtensions.DispatchMessage(message);

            new[] { DrsRegistry.CassieCooldownTag }.Kill();
            CoroutineHandle cooldownHandle = Timing.RunCoroutine(CassieCooldownRoutine(duration), DrsRegistry.CassieCooldownTag);
            return duration;
        }

        private IEnumerator<float> CassieCooldownRoutine(double duration)
        {
            yield return Timing.WaitForSeconds((float)duration + 0.5f);
            _cassieState = CassieStatus.Cooldown;
            yield return Timing.WaitForSeconds(1f);
            _cassieState = CassieStatus.Idle;
            Logger.Debug(nameof(Methods), "Radio transmission pipeline state machine safely returned to Idle baseline coordinates.", _plugin.Debug);
        }
        #endregion

        #region Helpers & Data Matrix Mappings
        private float GetRandomLockdownDuration() =>
            SafeRandom.Range(_config.DurationMin, _config.DurationMax);

        private bool IsRoomSkipped(Room room)
        {
            if (room == null) return true;
            return _roomsToSkip.Contains(room.Name) || (_config.SkipUnknownDoors && room.Name == RoomName.Unnamed);
        }

        private (float Chance, string Message) GetZoneSettings(FacilityZone zone) => zone switch
        {
            FacilityZone.HeavyContainment => (_config.ChanceHeavy, _config.CassieMessageHeavy),
            FacilityZone.LightContainment => (_config.ChanceLight, _config.CassieMessageLight),
            FacilityZone.Entrance => (_config.ChanceEntrance, _config.CassieMessageEntrance),
            FacilityZone.Surface => (_config.ChanceSurface, _config.CassieMessageSurface),
            _ => (_config.ChanceOther, _config.CassieMessageOther)
        };
        #endregion
    }
}