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
            Logger.Info(nameof(Methods), "DoorRestartSystem successfully running under safe RoomName pipelines.");
        }

        public void Clean()
        {
            ForceResetFacilityState();
            _roomsToSkip.Clear();
            _triggeredZones.Clear();

            DrsRegistry.FlushAll();

            Logger.Info(nameof(Methods), "DoorRestartSystem internal execution tracks successfully flushed.");
        }

        private void InitializeRoomSkipList()
        {
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
        #endregion

        #region Core Timing Loops & Manual Overrides
        public IEnumerator<float> StartLockdownTimer()
        {
            if (!_config.IsEnabled) yield break;

            yield return Timing.WaitForSeconds(_config.InitialDelay);

            while (true)
            {
                float delay = _config.RandomEvents
                    ? SafeRandom.Range(_config.DelayMin, _config.DelayMax)
                    : _config.InitialDelay;

                yield return Timing.WaitForSeconds(delay);
                yield return Timing.WaitUntilTrue(() => !Warhead.IsDetonated && !Warhead.IsDetonationInProgress);

                CoroutineHandle execHandle = Timing.RunCoroutine(ExecuteLockdownPipeline(null, false, null, null), DrsRegistry.ExecutionTag);
                DrsRegistry.RegisterHandle(execHandle);
            }
        }

        public void ForceManualLockdown(float? customDuration, FacilityZone? targetZone = null, RoomName? targetRoom = null)
        {
            InterruptActivePipelines();
            ForceResetFacilityState();

            float duration = customDuration ?? GetRandomLockdownDuration();

            CoroutineHandle execHandle = Timing.RunCoroutine(ExecuteLockdownPipeline(duration, true, targetZone, targetRoom), DrsRegistry.ExecutionTag);
            DrsRegistry.RegisterHandle(execHandle);
        }

        public void ForceStopLockdown()
        {
            InterruptActivePipelines();
            CassieExtensions.CassieClear();
            ForceResetFacilityState();
        }

        private void InterruptActivePipelines()
        {
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
            HashSet<Room> targets = new HashSet<Room>();
            List<string> announcementParts = new List<string>();

            if (targetRoom.HasValue)
            {
                Room specificRoom = Room.List.FirstOrDefault(r => r.Name == targetRoom.Value);
                if (specificRoom != null && !IsRoomSkipped(specificRoom))
                {
                    targets.Add(specificRoom);
                    announcementParts.Add(GetZoneSettings(specificRoom.Zone).Message);
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
                List<RoomLockdownContext> activeContexts = new List<RoomLockdownContext>();

                foreach (Room room in targets)
                {
                    // Context Resolution: Performed exactly ONCE prior entering operational loops
                    RoomLockdownContext context = new RoomLockdownContext(room, _config.SkipCheckpointsGate, _config.SkipElevators);

                    if (ProcessRoomLockdownExecution(context, duration))
                    {
                        _affectedRoomsMap[context.RoomInstanceId] = context;
                        activeContexts.Add(context);
                    }
                }

                if (activeContexts.Count > 0)
                {
                    CoroutineHandle finalizeHandle = Timing.RunCoroutine(FinalizeLockdownEvent(duration, activeContexts, targets), DrsRegistry.FinalizationTag);
                    DrsRegistry.RegisterHandle(finalizeHandle);
                }
            }
            else
            {
                TriggerCassieMessage(_config.CassieMessageWrong, isGlitchy: true);
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

        private bool ProcessRoomLockdownExecution(RoomLockdownContext context, float duration)
        {
            context.Room.SetLightsColor(new Color(_config.LightsColorR, _config.LightsColorG, _config.LightsColorB));

            if (!_roomSirenSessions.ContainsKey(context.RoomInstanceId))
            {
                int sirenId = _audioManager.PlayAtPosition(DrsAudioKey.LockdownSirenLoop, context.Room.Position + new Vector3(0f, 3.5f, 0f), loop: true, customLifespan: duration);
                if (sirenId != 0)
                    _roomSirenSessions[context.RoomInstanceId] = sirenId;
            }

            bool hasProcessedTargets = false;

            // Phase 1: High-Performance Allocation-Free Room Gating Execution Pipeline
            int normalDoorCount = context.NormalDoors.Length;
            if (normalDoorCount > 0)
            {
                Door[] targetDoors;
                if (_config.UsePerDoorChance)
                {
                    List<Door> rollingDoors = new List<Door>();
                    for (int i = 0; i < normalDoorCount; i++)
                    {
                        if (_config.ChancePerDoor.RollChance())
                            rollingDoors.Add(context.NormalDoors[i]);
                    }
                    targetDoors = rollingDoors.ToArray();
                }
                else
                {
                    targetDoors = context.NormalDoors;
                }

                int targetDoorCount = targetDoors.Length;
                if (targetDoorCount > 0)
                {
                    if (_config.CloseDoors) targetDoors.Close();
                    targetDoors.SetLockState(DoorLockReason.Isolation, true);
                    hasProcessedTargets = true;
                }
            }

            // Phase 2: Anti-Exploit Safe Elevator Execution Pipeline (Zero-Allocation Array Span Loops)
            int elevatorCount = context.ConnectedElevators.Length;
            for (int i = 0; i < elevatorCount; i++)
            {
                Elevator elevator = context.ConnectedElevators[i];
                if (_config.CloseDoors)
                {
                    elevator.CloseActiveDoors(bypassLocks: true);
                }
                elevator.Doors.SetLockState(DoorLockReason.Isolation, true);
                hasProcessedTargets = true;
            }

            if (!hasProcessedTargets) return false;

            if (_config.UsePerDoorChance)
            {
                for (int i = 0; i < normalDoorCount; i++)
                {
                    if (context.NormalDoors[i].IsLocked)
                        _audioManager.PlayAtPosition(DrsAudioKey.MechanicalLockSlam, context.NormalDoors[i].Position);
                }
            }
            else
            {
                _audioManager.PlayAtPosition(DrsAudioKey.MechanicalLockSlam, context.Room.Position);
            }

            return true;
        }
        #endregion

        #region Environmental Finalization Loops
        private IEnumerator<float> FinalizeLockdownEvent(float duration, List<RoomLockdownContext> contexts, HashSet<Room> eventRooms)
        {
            if (_config.Flicker)
            {
                Color lockdownColor = new Color(_config.LightsColorR, _config.LightsColorG, _config.LightsColorB);
                int contextCount = contexts.Count;

                for (int i = 0; i < contextCount; i++)
                {
                    int buzzId = _audioManager.PlayAtPosition(DrsAudioKey.ElectricalBuzzLoop, contexts[i].Room.Position, loop: true, customLifespan: duration);
                    if (buzzId != 0)
                        _activeBuzzSessions.Add(buzzId);
                }

                eventRooms.FlickerLights(lockdownColor, duration, _config.FlickerFrequency, DrsRegistry.FlickerTag);
            }

            yield return Timing.WaitForSeconds(duration);

            TriggerCassieMessage(_config.CassieMessageEnd, isGlitchy: true, force: true);
            _audioManager.PlayGlobal(DrsAudioKey.LockdownReleaseGlobal);

            int affectedCount = contexts.Count;
            for (int i = 0; i < affectedCount; i++)
            {
                RoomLockdownContext context = contexts[i];
                _affectedRoomsMap.Remove(context.RoomInstanceId);
                ReleaseRoomState(context);
            }

            HandlePostLockdownChaos(contexts);
        }

        private void ForceResetFacilityState()
        {
            foreach (RoomLockdownContext context in _affectedRoomsMap.Values)
            {
                if (context != null) ReleaseRoomState(context);
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

        private void ReleaseRoomState(RoomLockdownContext context)
        {
            if (_roomSirenSessions.TryGetValue(context.RoomInstanceId, out int sirenId))
            {
                _audioManager.StopSession(sirenId);
                _roomSirenSessions.Remove(context.RoomInstanceId);
            }

            context.Room.SetLightsColor(Color.clear);
            context.NormalDoors.SetLockState(DoorLockReason.Isolation, false);

            int elevatorCount = context.ConnectedElevators.Length;
            for (int i = 0; i < elevatorCount; i++)
            {
                context.ConnectedElevators[i].Doors.SetLockState(DoorLockReason.Isolation, false);
            }
        }

        private void HandlePostLockdownChaos(List<RoomLockdownContext> affectedContexts)
        {
            if (!_config.OpenDoorsAfterLockdown || !_config.OpenDoorsChance.RollChance())
                return;

            int contextCount = affectedContexts.Count;
            for (int i = 0; i < contextCount; i++)
            {
                RoomLockdownContext context = affectedContexts[i];
                if (_config.OpenOnlyCheckpoints && !context.Room.Name.IsCheckpoint())
                    continue;

                List<Door> openableDoors = new List<Door>();
                int normalDoorCount = context.NormalDoors.Length;

                for (int d = 0; d < normalDoorCount; d++)
                {
                    Door door = context.NormalDoors[d];
                    if (door != null && !door.IsLocked)
                    {
                        openableDoors.Add(door);
                    }
                }

                if (openableDoors.Count > 0)
                {
                    openableDoors.Open();
                }

                int elevatorCount = context.ConnectedElevators.Length;
                for (int e = 0; e < elevatorCount; e++)
                {
                    context.ConnectedElevators[e].OpenActiveDoors(bypassLocks: false);
                }
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
            DrsRegistry.RegisterHandle(cooldownHandle);
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