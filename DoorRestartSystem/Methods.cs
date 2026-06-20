namespace DoorRestartSystem
{
    using DoorRestartSystem.Shared;
    using DoorRestartSystem.Shared.Audio;
    using DoorRestartSystem.Shared.Audio.Enums;
    using Interactables.Interobjects.DoorUtils;
    using LabApi.Features.Wrappers;
    using MapGeneration;
    using MEC;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Type-safe LabAPI-compliant manager leveraging structural RoomName configurations to run facility lockdowns.
    /// </summary>
    public class Methods
    {
        private readonly Plugin _plugin;
        private readonly Config _config;
        private readonly HashSet<RoomName> _roomsToSkip;

        // Tracks the currently affected rooms during any active event to allow safe administrative overrides
        private readonly HashSet<Room> _activeAffectedRooms;

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
        private readonly DrsAudioManager _audioManager;
        public Methods(Plugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin instance cannot be null.");
            _config = _plugin.Config;
            _roomsToSkip = new HashSet<RoomName>();
            _activeAffectedRooms = new HashSet<Room>();
            _audioManager = new DrsAudioManager(_plugin);
        }

        #region Initialization and Cleanup

        public void Init()
        {
            if (!_config.IsEnabled)
            {
                Library_LabAPI.LogInfo("Methods.Init", "DoorRestartSystem is disabled via configuration.");
                return;
            }

            InitializeRoomSkipList();
            Library_LabAPI.LogInfo("Methods.Init", "DoorRestartSystem successfully running under safe RoomName pipelines.");
        }

        public void Clean()
        {
            _roomsToSkip.Clear();
            _activeAffectedRooms.Clear();
            _audioManager.Clean();

            // Prevent thread/coroutine accumulation across round restarts or plugin reloads
            Timing.KillCoroutines(TagLockdownTimer);
            Timing.KillCoroutines(TagLockdownExec);
            Timing.KillCoroutines(TagLockdownFinalize);
            Timing.KillCoroutines(TagLockdownFlicker);
            Timing.KillCoroutines(TagCassieCooldown);

            Library_LabAPI.LogInfo("Methods.Clean", "DoorRestartSystem internal tracks flushed.");
        }

        /// <summary>
        /// Populates the type-safe skip tables matching against explicit RoomName enum tokens.
        /// </summary>
        private void InitializeRoomSkipList()
        {
            _roomsToSkip.Add(RoomName.Lcz914);

            if (_config.SkipNukeDoors)
            {
                _roomsToSkip.Add(RoomName.HczWarhead);
            }

            if (_config.SkipAirlocks)
            {
                _roomsToSkip.Add(RoomName.LczAirlock);
            }

            // Map configuration rules onto specific structural layouts to avoid trapping gameplay-critical roles
            foreach (RoomName name in Enum.GetValues(typeof(RoomName)))
            {
                if (_config.SkipSCPRooms && name.IsScpRoom())
                    _roomsToSkip.Add(name);

                if (_config.SkipArmory && name.IsArmory())
                    _roomsToSkip.Add(name);

                if (_config.SkipCheckpoints && name.IsCheckpoint())
                    _roomsToSkip.Add(name);
            }
        }

        #endregion

        #region Lockdown Logic

        public IEnumerator<float> StartLockdownTimer()
        {
            if (!_config.IsEnabled)
                yield break;

            yield return Timing.WaitForSeconds(_config.InitialDelay);

            while (true)
            {
                float delay = _config.RandomEvents
                    ? Library_LabAPI.Loader_Random_Next(_config.DelayMin, _config.DelayMax)
                    : _config.InitialDelay;
                yield return Timing.WaitForSeconds(delay);

                // Hold execution if the round is ending via Alpha Warhead detonation sequence
                yield return Timing.WaitUntilTrue(() => !Warhead.IsDetonated && !Warhead.IsDetonationInProgress);

                Timing.RunCoroutine(ExecuteLockdownEvent(), TagLockdownExec);
            }
        }

        private IEnumerator<float> ExecuteLockdownEvent()
        {
            if (_config.CassieMessageClearBeforeImportant)
                Library_LabAPI.Cassie_Clear();

            if (_config.IsCountdownEnabled)
            {
                TriggerCassieMessage(_config.CassieMessageCountdown, true);
                yield return Timing.WaitForSeconds(_config.TimeBetweenSentenceAndStart);
            }

            TriggerCassieMessage(_config.CassieMessageStart, false);
            _audioManager.PlayGlobal(DrsAudioKey.LockdownSirenGlobal);
            float lockdownDuration = GetLockdownDuration();

            // Clear any stale references from prior events to guarantee state predictability
            _activeAffectedRooms.Clear();
            HashSet<FacilityZone> triggeredZonesContext = new HashSet<FacilityZone>();

            // Route execution flow dynamically based on whether the admin prefers macro (Zone) or micro (Room) RNG control
            bool lockdownOccurred = _config.UsePerRoomChances
                ? HandleRoomSpecificLockdown(lockdownDuration, _activeAffectedRooms, triggeredZonesContext)
                : HandleZoneSpecificLockdown(lockdownDuration, _activeAffectedRooms, triggeredZonesContext);

            Timing.RunCoroutine(FinalizeLockdownEvent(lockdownOccurred, lockdownDuration, _activeAffectedRooms), TagLockdownFinalize);
        }

        private bool HandleZoneSpecificLockdown(float lockdownDuration, HashSet<Room> roomsCtx, HashSet<FacilityZone> zonesCtx)
        {
            bool isLockdownTriggered = false;

            isLockdownTriggered |= AttemptZoneLockdown(FacilityZone.HeavyContainment, _config.ChanceHeavy, _config.CassieMessageHeavy, lockdownDuration, roomsCtx, zonesCtx);
            isLockdownTriggered |= AttemptZoneLockdown(FacilityZone.LightContainment, _config.ChanceLight, _config.CassieMessageLight, lockdownDuration, roomsCtx, zonesCtx);
            isLockdownTriggered |= AttemptZoneLockdown(FacilityZone.Entrance, _config.ChanceEntrance, _config.CassieMessageEntrance, lockdownDuration, roomsCtx, zonesCtx);
            isLockdownTriggered |= AttemptZoneLockdown(FacilityZone.Surface, _config.ChanceSurface, _config.CassieMessageSurface, lockdownDuration, roomsCtx, zonesCtx);

            // Fallback system to guarantee a facility event if independent probability checks rolled negative
            if (!isLockdownTriggered && _config.EnableFacilityLockdown)
            {
                TriggerFacilityWideLockdown(lockdownDuration, roomsCtx);
                isLockdownTriggered = true;
            }

            return isLockdownTriggered;
        }

        private bool AttemptZoneLockdown(FacilityZone zone, float chance, string cassieMessage, float lockdownDuration, HashSet<Room> roomsCtx, HashSet<FacilityZone> zonesCtx)
        {
            if (Library_LabAPI.Loader_Random_NextDouble() * 100 < chance)
            {
                foreach (Room room in Room.List.Where(r => r.Zone == zone))
                {
                    LockdownRoom(room, lockdownDuration, roomsCtx);
                }
                if (!zonesCtx.Contains(zone))
                {
                    TriggerCassieMessage(cassieMessage);
                    zonesCtx.Add(zone);
                }
                return true;
            }
            return false;
        }

        private bool HandleRoomSpecificLockdown(float lockdownDuration, HashSet<Room> roomsCtx, HashSet<FacilityZone> zonesCtx)
        {
            bool lockdownTriggered = false;

            foreach (Room room in Room.List)
            {
                if (AttemptRoomLockdown(room, lockdownDuration, roomsCtx, zonesCtx))
                {
                    lockdownTriggered = true;
                }
            }

            if (!lockdownTriggered && _config.EnableFacilityLockdown)
            {
                TriggerFacilityWideLockdown(lockdownDuration, roomsCtx);
                return true;
            }
            return lockdownTriggered;
        }

        private bool AttemptRoomLockdown(Room room, float lockdownDuration, HashSet<Room> roomsCtx, HashSet<FacilityZone> zonesCtx)
        {
            float chance = room.Zone switch
            {
                FacilityZone.HeavyContainment => _config.ChanceHeavy,
                FacilityZone.LightContainment => _config.ChanceLight,
                FacilityZone.Entrance => _config.ChanceEntrance,
                FacilityZone.Surface => _config.ChanceSurface,
                _ => _config.ChanceOther
            };

            string cassieMessage = room.Zone switch
            {
                FacilityZone.HeavyContainment => _config.CassieMessageHeavy,
                FacilityZone.LightContainment => _config.CassieMessageLight,
                FacilityZone.Entrance => _config.CassieMessageEntrance,
                FacilityZone.Surface => _config.CassieMessageSurface,
                _ => _config.CassieMessageOther,
            };

            if (Library_LabAPI.Loader_Random_NextDouble() * 100 < chance)
            {
                LockdownRoom(room, lockdownDuration, roomsCtx);
                if (!zonesCtx.Contains(room.Zone))
                {
                    TriggerCassieMessage(cassieMessage);
                    zonesCtx.Add(room.Zone);
                }
                return true;
            }
            return false;
        }

        private void LockdownRoom(Room room, float duration, HashSet<Room> roomsCtx)
        {
            if (_roomsToSkip.Contains(room.Name))
                return;

            // Prevent map generation artifacts or custom unzoned rooms from breaking the sequence
            if (_config.SkipUnknownDoors && room.Name == RoomName.Unnamed)
                return;

            bool anyDoorLocked = false;

            foreach (Door door in room.Doors)
            {
                if (door == null) continue;

                // Safeguard large transit gates from being locked to prevent soft-locking surface or cross-zone pathways
                if (_config.SkipCheckpointsGate && room.Name.IsCheckpoint() && door.GameObject.name.Contains("Gate"))
                    continue;

                bool shouldLock = !_config.UsePerDoorChance || (Library_LabAPI.Loader_Random_NextDouble() * 100 < _config.ChancePerDoor);
                if (shouldLock)
                {
                    if (_config.CloseDoors)
                    {
                        door.IsOpened = false;
                    }
                    if (!door.IsLocked)
                    {
                        door.Lock(DoorLockReason.Isolation, true);
                    }
                    anyDoorLocked = true;
                }
            }

            // Visually notify players inside the affected area by updating the environment state
            if (anyDoorLocked)
            {
                Color color = new Color(_config.LightsColorR, _config.LightsColorG, _config.LightsColorB);
                room.LightController.OverrideLightsColor = color;
                roomsCtx.Add(room);
                _audioManager.PlayAtPosition(DrsAudioKey.MechanicalLockSlam, room.Position);
            }
        }

        private void TriggerFacilityWideLockdown(float lockdownDuration, HashSet<Room> roomsCtx)
        {
            TriggerCassieMessage(_config.CassieMessageFacility);
            foreach (Room room in Room.List)
            {
                LockdownRoom(room, lockdownDuration, roomsCtx);
            }
        }

        private IEnumerator<float> FinalizeLockdownEvent(bool lockdownOccurred, float lockdownDuration, HashSet<Room> roomsCtx)
        {
            if (lockdownOccurred)
            {
                if (_config.Flicker)
                {
                    Timing.RunCoroutine(FlickerRoomLights(lockdownDuration, roomsCtx), TagLockdownFlicker);
                }

                yield return Timing.WaitForSeconds(lockdownDuration);
                TriggerCassieMessage(_config.CassieMessageEnd);
                _audioManager.PlayGlobal(DrsAudioKey.LockdownReleaseGlobal);

                // Revert all modified gameplay and environmental mechanics back to standard facility parameters
                foreach (Room room in roomsCtx)
                {
                    if (room == null) continue;

                    room.LightController.OverrideLightsColor = Color.clear;

                    foreach (Door door in room.Doors)
                    {
                        if (door != null)
                        {
                            door.Lock(DoorLockReason.Isolation, false);
                        }
                    }
                }

                // Optional post-event chaos system designed to shift map dynamics after a containment failure simulation
                if (_config.OpenDoorsAfterLockdown)
                {
                    if (Library_LabAPI.Loader_Random_NextDouble() * 100 < _config.OpenDoorsChance)
                    {
                        int openedDoorCount = 0;
                        foreach (Room room in roomsCtx)
                        {
                            if (room == null) continue;

                            if (_roomsToSkip.Contains(room.Name))
                                continue;

                            if (_config.OpenOnlyCheckpoints && !room.Name.IsCheckpoint())
                                continue;

                            foreach (Door door in room.Doors)
                            {
                                if (door == null || door.IsLocked) continue;

                                door.IsOpened = true;
                                openedDoorCount++;
                            }
                        }
                        Library_LabAPI.LogInfo("FinalizeLockdownEvent", $"Post-lockdown auto-open completed. Forced {openedDoorCount} structural doors open via RoomName matrix.");
                    }
                }
            }
            else
            {
                TriggerCassieMessage(_config.CassieMessageWrong, true);
            }
        }

        private IEnumerator<float> FlickerRoomLights(float lockdownDuration, HashSet<Room> roomsCtx)
        {
            float elapsedTime = 0f;
            float halfCycle = 0.5f / _config.FlickerFrequency;
            List<int> diagnosticSessions = new List<int>();

            // Deploy spatialized ambient shortcuts to sonically map environmental decay
            foreach (Room room in roomsCtx)
            {
                if (room == null) continue;
                int id = _audioManager.PlayAtPosition(DrsAudioKey.ElectricalBuzzLoop, room.Position, loop: true, customLifespan: lockdownDuration);
                if (id != 0) diagnosticSessions.Add(id);
            }

            while (elapsedTime < lockdownDuration)
            {
                yield return Timing.WaitForSeconds(halfCycle);
                elapsedTime += halfCycle;

                foreach (Room room in roomsCtx)
                {
                    if (room != null && room.LightController.LightsEnabled)
                    {
                        room.LightController.FlickerLights(halfCycle);
                    }
                }

                yield return Timing.WaitForSeconds(halfCycle);
                elapsedTime += halfCycle;
            }

            // Force release of looping channels to guarantee audio engine synchronization recovery
            foreach (int id in diagnosticSessions)
            {
                _audioManager.StopSession(id);
            }
        }

        /// <summary>
        /// Provides an on-demand override mechanism for gamemasters to bypass standard time intervals.
        /// </summary>
        public void ForceManualLockdown(float customDuration)
        {
            // Interrupt any running execution stacks to prevent overlapping event calculations
            Timing.KillCoroutines(TagLockdownExec);
            Timing.KillCoroutines(TagLockdownFinalize);

            Timing.RunCoroutine(ExecuteManualLockdownRoutine(customDuration), TagLockdownExec);
        }

        /// <summary>
        /// Instantly breaks active coroutines and restores the facility to its default gameplay state.
        /// </summary>
        public void ForceStopLockdown()
        {
            Timing.KillCoroutines(TagLockdownExec);
            Timing.KillCoroutines(TagLockdownFinalize);
            Timing.KillCoroutines(TagLockdownFlicker);

            // Revert all modified gameplay and environmental mechanics back to standard facility parameters immediately
            foreach (Room room in _activeAffectedRooms)
            {
                if (room == null) continue;
                room.LightController.OverrideLightsColor = Color.clear;

                foreach (Door door in room.Doors)
                {
                    if (door != null)
                    {
                        door.Lock(DoorLockReason.Isolation, false);
                    }
                }
            }

            _activeAffectedRooms.Clear();
        }

        private IEnumerator<float> ExecuteManualLockdownRoutine(float customDuration)
        {
            if (_config.CassieMessageClearBeforeImportant)
                Library_LabAPI.Cassie_Clear();

            TriggerCassieMessage(_config.CassieMessageStart, false);
            _audioManager.PlayGlobal(DrsAudioKey.LockdownSirenGlobal);

            // Determine duration based on whether the admin specified an explicit override or requested RNG rules
            float duration = customDuration > 0 ? customDuration : GetLockdownDuration();

            _activeAffectedRooms.Clear();
            HashSet<FacilityZone> triggeredZonesContext = new HashSet<FacilityZone>();

            bool lockdownOccurred = _config.UsePerRoomChances
                ? HandleRoomSpecificLockdown(duration, _activeAffectedRooms, triggeredZonesContext)
                : HandleZoneSpecificLockdown(duration, _activeAffectedRooms, triggeredZonesContext);

            Timing.RunCoroutine(FinalizeLockdownEvent(lockdownOccurred, duration, _activeAffectedRooms), TagLockdownFinalize);
            yield break;
        }

        #endregion

        #region CASSIE Management

        private void TriggerCassieMessage(string message, bool isGlitchy = false)
        {
            // Enforce a strict single-thread-like lock on audio announcements to avoid auditory overlaps
            if (string.IsNullOrWhiteSpace(message) || _cassieState != CassieStatus.Idle) return;

            _cassieState = CassieStatus.Playing;

            if (_config.CassieMessageClearBeforeImportant)
                Library_LabAPI.Cassie_Clear();

            if (isGlitchy)
                Library_LabAPI.Cassie_GlitchyMessage(message, _config.GlitchChance / 100, _config.JamChance / 100);
            else
                Library_LabAPI.Cassie_Message(message);

            Timing.KillCoroutines(TagCassieCooldown);
            Timing.RunCoroutine(CassieCooldownRoutine(), TagCassieCooldown);
        }

        private IEnumerator<float> CassieCooldownRoutine()
        {
            yield return Timing.WaitForSeconds(_config.TimeBetweenSentenceAndStart + 0.5f);
            _cassieState = CassieStatus.Cooldown;
            yield return Timing.WaitForSeconds(1f);
            _cassieState = CassieStatus.Idle;
        }

        #endregion

        #region Utility Methods

        private float GetLockdownDuration()
        {
            return (float)Library_LabAPI.Loader_Random_NextDouble() * (_config.DurationMax - _config.DurationMin) + _config.DurationMin;
        }

        #endregion
    }
}