namespace DoorRestartSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using MEC;
    using MapGeneration;
    using LabApi.Features.Wrappers;
    using Interactables.Interobjects.DoorUtils;
    using DoorRestartSystem.Shared;
    using DoorRestartSystem.Shared.Audio;
    using DoorRestartSystem.Shared.Audio.Enums;

    /// <summary>
    /// Type-safe LabAPI-compliant manager leveraging structural RoomName configurations 
    /// and dynamic audio tracking metrics to run facility-wide lockdown protocols.
    /// </summary>
    public class Methods
    {
        #region Private Repositories & Execution States
        private readonly Plugin _plugin;
        private readonly Config _config;
        private readonly DrsAudioManager _audioManager;

        private readonly HashSet<RoomName> _roomsToSkip = new();
        private readonly Dictionary<int, Room> _affectedRoomsMap = new();
        private readonly Dictionary<int, int> _roomSirenSessions = new();
        private readonly List<int> _activeBuzzSessions = new();
        private readonly HashSet<FacilityZone> _triggeredZones = new();

        private const string TagLockdownTimer = "DRS-LockdownTimer";
        private const string TagLockdownExec = "DRS-LockdownExec";
        private const string TagLockdownFinalize = "DRS-LockdownFinalize";
        private const string TagLockdownFlicker = "DRS-LockdownFlicker";
        private const string TagCassieCooldown = "DRS-CassieCooldown";

        private enum CassieStatus
        {
            Idle,
            Playing,
            Cooldown
        }

        private CassieStatus _cassieState = CassieStatus.Idle;
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes a new instance of the <see cref="Methods"/> class bound to the plugin root context.
        /// </summary>
        public Methods(Plugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Structural runtime allocation failure: Parent plugin context evaluates to null.");
            _config = _plugin.Config;
            _audioManager = new DrsAudioManager(_plugin);
        }
        #endregion

        #region Subsystem Lifecycle Management
        /// <summary>
        /// Registers structural room filters and prepares the subsystem environment for operational loops.
        /// </summary>
        public void Init()
        {
            if (!_config.IsEnabled)
            {
                Library_LabAPI.LogInfo(nameof(Methods), "DoorRestartSystem is disabled via configuration.");
                return;
            }

            InitializeRoomSkipList();
            Library_LabAPI.LogInfo(nameof(Methods), "DoorRestartSystem successfully running under safe RoomName pipelines.");
        }

        /// <summary>
        /// Drops active locks, restores room illumination vectors, and systematically flushes background loops.
        /// </summary>
        public void Clean()
        {
            ForceResetFacilityState();
            _roomsToSkip.Clear();
            _triggeredZones.Clear();
            _audioManager.Clean();

            Timing.KillCoroutines(TagLockdownTimer);
            Timing.KillCoroutines(TagLockdownExec);
            Timing.KillCoroutines(TagLockdownFinalize);
            Timing.KillCoroutines(TagLockdownFlicker);
            Timing.KillCoroutines(TagCassieCooldown);

            Library_LabAPI.LogInfo(nameof(Methods), "DoorRestartSystem internal execution tracks successfully flushed.");
        }

        /// <summary>
        /// Evaluates configuration rules to compile high-precision exclusion graphs mapping room objects.
        /// </summary>
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
        /// <summary>
        /// Background execution loop managing chronological thresholds before selecting and processing an event.
        /// </summary>
        public IEnumerator<float> StartLockdownTimer()
        {
            if (!_config.IsEnabled) yield break;

            yield return Timing.WaitForSeconds(_config.InitialDelay);

            while (true)
            {
                float delay = _config.RandomEvents
                    ? (float)Library_LabAPI.Loader_Random_Next((int)_config.DelayMin, (int)_config.DelayMax)
                    : _config.InitialDelay;

                yield return Timing.WaitForSeconds(delay);
                yield return Timing.WaitUntilTrue(() => !Warhead.IsDetonated && !Warhead.IsDetonationInProgress);

                Timing.RunCoroutine(ExecuteLockdownPipeline(), TagLockdownExec);
            }
        }

        /// <summary>
        /// Forces an immediate structural lockdown, dropping countdown blocks according to operational parameters.
        /// </summary>
        public void ForceManualLockdown(float customDuration)
        {
            InterruptActivePipelines();
            ForceResetFacilityState();

            float duration = customDuration > 0 ? customDuration : GetRandomLockdownDuration();
            Timing.RunCoroutine(ExecuteLockdownPipeline(duration, skipCountdown: true), TagLockdownExec);
        }

        /// <summary>
        /// Flushes active announcements and immediately vents emergency pneumatic locking pressure facility-wide.
        /// </summary>
        public void ForceStopLockdown()
        {
            InterruptActivePipelines();
            Library_LabAPI.Cassie_Clear();
            ForceResetFacilityState();
        }

        /// <summary>
        /// Aborts active coroutines and releases local positional audio instances.
        /// </summary>
        private void InterruptActivePipelines()
        {
            Timing.KillCoroutines(TagLockdownExec);
            Timing.KillCoroutines(TagLockdownFinalize);
            Timing.KillCoroutines(TagLockdownFlicker);

            foreach (int id in _activeBuzzSessions)
            {
                _audioManager.StopSession(id);
            }
            _activeBuzzSessions.Clear();
        }
        #endregion

        #region Operational Lockdown Engine
        /// <summary>
        /// Evaluates current structural state arrays, manages warning transmissions, and triggers mechanical locking routines.
        /// </summary>
        private IEnumerator<float> ExecuteLockdownPipeline(float? customDuration = null, bool skipCountdown = false)
        {
            HashSet<Room> targets = new();
            List<string> announcementParts = new();

            if (_config.UsePerRoomChances)
            {
                EvaluatePerRoomLockdown(targets, announcementParts);
            }
            else
            {
                EvaluatePerZoneLockdown(targets, announcementParts);
            }

            if (targets.Count == 0 && _config.EnableFacilityLockdown)
            {
                announcementParts.Add(_config.CassieMessageFacility);
                foreach (Room room in Room.List)
                {
                    if (!IsRoomSkipped(room)) targets.Add(room);
                }
            }

            bool lockdownOccurred = targets.Count > 0;

            if (_config.CassieMessageClearBeforeImportant)
            {
                Library_LabAPI.Cassie_Clear();
            }

            if (lockdownOccurred)
            {
                // CRITICAL REFACTOR: Routed completely through the central state-machine gatekeeper with explicit structural overrides
                if (_config.IsCountdownEnabled && !skipCountdown)
                {
                    double countdownDuration = TriggerCassieMessage(_config.CassieMessageCountdown, isGlitchy: true, force: true);
                    yield return Timing.WaitForSeconds((float)countdownDuration + 0.5f);
                }

                string combinedPhrase = $"{_config.CassieMessageStart} {string.Join(" ", announcementParts)}";
                TriggerCassieMessage(combinedPhrase, isGlitchy: false, force: true);

                float duration = customDuration ?? GetRandomLockdownDuration();
                HashSet<Room> successfullyProcessedRooms = new();

                foreach (Room room in targets)
                {
                    if (ProcessRoomLockdownExecution(room, duration))
                    {
                        successfullyProcessedRooms.Add(room);
                        _affectedRoomsMap[room.GameObject.GetInstanceID()] = room;
                    }
                }

                Timing.RunCoroutine(FinalizeLockdownEvent(duration, successfullyProcessedRooms), TagLockdownFinalize);
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
                if (Library_LabAPI.Loader_Random_NextDouble() * 100 < chance)
                {
                    if (!string.IsNullOrWhiteSpace(message)) announcementParts.Add(message);

                    foreach (Room room in Room.List.Where(r => r.Zone == zone))
                    {
                        if (!IsRoomSkipped(room)) targetRooms.Add(room);
                    }
                }
            }
        }

        private void EvaluatePerRoomLockdown(HashSet<Room> targetRooms, List<string> announcementParts)
        {
            foreach (Room room in Room.List)
            {
                if (IsRoomSkipped(room)) continue;

                var (chance, message) = GetZoneSettings(room.Zone);
                if (Library_LabAPI.Loader_Random_NextDouble() * 100 < chance)
                {
                    targetRooms.Add(room);
                    if (_triggeredZones.Add(room.Zone) && !string.IsNullOrWhiteSpace(message))
                    {
                        announcementParts.Add(message);
                    }
                }
            }
        }

        /// <summary>
        /// Executes mechanical grid modifications, overrides light matrices, and hooks environmental alert sounds into target zones.
        /// </summary>
        private bool ProcessRoomLockdownExecution(Room room, float duration)
        {
            int roomInstanceId = room.GameObject.GetInstanceID();

            room.LightController.OverrideLightsColor = new Color(_config.LightsColorR, _config.LightsColorG, _config.LightsColorB);

            if (!_roomSirenSessions.ContainsKey(roomInstanceId))
            {
                int id = _audioManager.PlayAtPosition(DrsAudioKey.LockdownSirenLoop, room.Position + new Vector3(0, 3.5f, 0), loop: true, customLifespan: duration);
                if (id != 0) _roomSirenSessions[roomInstanceId] = id;
            }

            _audioManager.PlayAtPosition(DrsAudioKey.MechanicalLockSlam, room.Position);

            foreach (Door door in room.Doors)
            {
                if (door == null) continue;

                if (_config.SkipElevators && (door.GameObject.name.Contains("Elevator") || door.GameObject.GetComponentInParent<Interactables.Interobjects.ElevatorDoor>() != null)) continue;
                if (_config.SkipCheckpointsGate && room.Name.IsCheckpoint() && door.GameObject.name.Contains("Gate")) continue;

                bool shouldLock = !_config.UsePerDoorChance || (Library_LabAPI.Loader_Random_NextDouble() * 100 < _config.ChancePerDoor);
                if (shouldLock)
                {
                    if (_config.CloseDoors) door.IsOpened = false;
                    if (!door.IsLocked) door.Lock(DoorLockReason.Isolation, true);
                }
            }

            return true;
        }
        #endregion

        #region Environmental Finalization Loops
        private IEnumerator<float> FinalizeLockdownEvent(float duration, HashSet<Room> eventRooms)
        {
            if (_config.Flicker)
            {
                Timing.RunCoroutine(FlickerRoomLights(duration, eventRooms), TagLockdownFlicker);
            }

            yield return Timing.WaitForSeconds(duration);

            // CRITICAL REFACTOR: Routed via unified state machine to lock out concurrent overrides smoothly
            TriggerCassieMessage(_config.CassieMessageEnd, isGlitchy: false, force: true);
            _audioManager.PlayGlobal(DrsAudioKey.LockdownReleaseGlobal);

            foreach (Room room in eventRooms)
            {
                if (room == null) continue;
                int instanceId = room.GameObject.GetInstanceID();
                _affectedRoomsMap.Remove(instanceId);
                ReleaseRoomState(room, instanceId);
            }

            HandlePostLockdownChaos(eventRooms);
        }

        private IEnumerator<float> FlickerRoomLights(float duration, HashSet<Room> eventRooms)
        {
            float elapsedTime = 0f;
            float halfCycle = 0.5f / _config.FlickerFrequency;

            foreach (Room room in eventRooms)
            {
                if (room == null) continue;
                int id = _audioManager.PlayAtPosition(DrsAudioKey.ElectricalBuzzLoop, room.Position, loop: true, customLifespan: duration);
                if (id != 0) _activeBuzzSessions.Add(id);
            }

            while (elapsedTime < duration)
            {
                yield return Timing.WaitForSeconds(halfCycle);
                elapsedTime += halfCycle;

                foreach (Room room in eventRooms)
                {
                    if (room != null && room.LightController.LightsEnabled)
                    {
                        room.LightController.FlickerLights(halfCycle);
                    }
                }

                yield return Timing.WaitForSeconds(halfCycle);
                elapsedTime += halfCycle;
            }

            foreach (int id in _activeBuzzSessions)
            {
                _audioManager.StopSession(id);
            }
            _activeBuzzSessions.Clear();
        }

        private void ForceResetFacilityState()
        {
            var activeRoomsToReset = _affectedRoomsMap.Values.ToList();
            foreach (Room room in activeRoomsToReset)
            {
                if (room == null) continue;
                ReleaseRoomState(room, room.GameObject.GetInstanceID());
            }

            foreach (var kvp in _roomSirenSessions.ToList())
            {
                _audioManager.StopSession(kvp.Value);
            }

            _roomSirenSessions.Clear();
            _affectedRoomsMap.Clear();

            foreach (int id in _activeBuzzSessions)
            {
                _audioManager.StopSession(id);
            }
            _activeBuzzSessions.Clear();
        }

        private void ReleaseRoomState(Room room, int roomInstanceId)
        {
            if (_roomSirenSessions.TryGetValue(roomInstanceId, out int id))
            {
                _audioManager.StopSession(id);
                _roomSirenSessions.Remove(roomInstanceId);
            }

            room.LightController.OverrideLightsColor = Color.clear;

            foreach (Door door in room.Doors)
            {
                door?.Lock(DoorLockReason.Isolation, false);
            }
        }

        private void HandlePostLockdownChaos(HashSet<Room> eventRooms)
        {
            if (!_config.OpenDoorsAfterLockdown || !(Library_LabAPI.Loader_Random_NextDouble() * 100 < _config.OpenDoorsChance))
                return;

            int openedDoorCount = 0;
            foreach (Room room in eventRooms)
            {
                if (room == null || IsRoomSkipped(room)) continue;
                if (_config.OpenOnlyCheckpoints && !room.Name.IsCheckpoint()) continue;

                foreach (Door door in room.Doors)
                {
                    if (door == null || door.IsLocked) continue;
                    door.IsOpened = true;
                    openedDoorCount++;
                }
            }
            Library_LabAPI.LogInfo("FinalizeLockdownEvent", $"Post-lockdown completed. Forced {openedDoorCount} doors open.");
        }
        #endregion

        #region Radio Broadcast State Machine (SCP-575 Optimized Pattern Upgrade)
        /// <summary>
        /// Validates transmission pathways, boots active tracking loops, and records explicit stream timeline widths.
        /// Supports chronological sequencing overrides via the force argument filter.
        /// </summary>
        private double TriggerCassieMessage(string message, bool isGlitchy = false, bool force = false)
        {
            double duration = 0.0;
            if (string.IsNullOrWhiteSpace(message))
            {
                Library_LabAPI.LogDebug("TriggerCassieMessage", "CASSIE message configuration evaluates to null or empty space, skipping.");
                return 0.0;
            }

            // CRITICAL UPGRADE: Added explicit verification against the sequence override parameter
            if (!force && _cassieState != CassieStatus.Idle)
            {
                Library_LabAPI.LogDebug("TriggerCassieMessage", $"Vocal pipeline transmission blocked: State machine busy. Status: [{_cassieState}]. Dropping phrase.");
                return 0.0;
            }

            _cassieState = CassieStatus.Playing;
            Library_LabAPI.LogDebug("TriggerCassieMessage", $"Vocal pipeline initialized. Transmitting phrase payload: {message}");

            if (_config.CassieMessageClearBeforeImportant)
                Library_LabAPI.Cassie_Clear();

            if (isGlitchy)
                duration = Library_LabAPI.Cassie_GlitchyMessage(message, _config.GlitchChance, _config.JamChance);
            else
                duration = Library_LabAPI.Cassie_Message(message);

            // Dynamically forward the explicit track execution time directly down into the tracking coroutine thread
            Timing.KillCoroutines(TagCassieCooldown);
            Timing.RunCoroutine(CassieCooldownRoutine(duration), TagCassieCooldown);
            return duration;
        }

        /// <summary>
        /// Monitors vocal playback states dynamically using physical audio length tracking.
        /// </summary>
        private IEnumerator<float> CassieCooldownRoutine(double duration)
        {
            // Fully detached from old static config delay. Using our newly approved solid 0.5s tail-buffer coordinate.
            yield return Timing.WaitForSeconds((float)duration + 0.5f);
            _cassieState = CassieStatus.Cooldown;
            yield return Timing.WaitForSeconds(1f);
            _cassieState = CassieStatus.Idle;
            Library_LabAPI.LogDebug("CassieCooldownRoutine", "Radio transmission pipeline state machine safely returned to Idle baseline coordinates.");
        }
        #endregion

        #region Helpers & Data Matrix Mappings
        private float GetRandomLockdownDuration() =>
            (float)Library_LabAPI.Loader_Random_NextDouble() * (_config.DurationMax - _config.DurationMin) + _config.DurationMin;

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