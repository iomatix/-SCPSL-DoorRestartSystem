namespace DoorRestartSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MEC;
    using DoorRestartSystem.Shared;
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.API.Features.Doors;
    using UnityEngine;

    /// <summary>
    /// Manages the door restart system, handling room lockdowns, CASSIE announcements, and light flickering.
    /// </summary>
    public class Methods
    {
        private readonly Plugin _plugin;
        private readonly Config _config;
        private readonly HashSet<Room> _changedRooms;
        private readonly HashSet<DoorType> _doorTypesToSkip;
        private readonly HashSet<ZoneType> _triggeredZones;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="Methods"/> class.
        /// </summary>
        /// <param name="plugin">The plugin instance providing configuration and utilities.</param>
        /// <exception cref="ArgumentNullException">Thrown if plugin is null.</exception>
        public Methods(Plugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin instance cannot be null.");
            _config = _plugin.Config;
            _changedRooms = new HashSet<Room>();
            _doorTypesToSkip = new HashSet<DoorType>();
            _triggeredZones = new HashSet<ZoneType>();
        }

        #region Initialization and Cleanup

        /// <summary>
        /// Initializes the door skip list and prepares the system for operation.
        /// </summary>
        public void Init()
        {
            if (!_config.IsEnabled)
            {
                Library_ExiledAPI.LogInfo("Methods.Init", "DoorRestartSystem is disabled via config.");
                return;
            }

            InitializeDoorSkipList();
            Library_ExiledAPI.LogInfo("Methods.Init", "DoorRestartSystem methods initialized.");
        }

        /// <summary>
        /// Cleans up active coroutines, resets room states, and clears collections.
        /// </summary>
        public void Clean()
        {
            ResetRoomColors();

            _changedRooms.Clear();
            _doorTypesToSkip.Clear();
            _triggeredZones.Clear();

            Timing.KillCoroutines(TagLockdownExec);
            Timing.KillCoroutines(TagLockdownFinalize);
            Timing.KillCoroutines(TagLockdownFlicker);
            Timing.KillCoroutines(TagCassieCooldown);

            Library_ExiledAPI.LogInfo("Methods.Clean", "DoorRestartSystem methods cleaned.");
        }

        private void InitializeDoorSkipList()
        {
            _doorTypesToSkip.Add(DoorType.Scp914Door);

            if (_config.SkipNukeDoors)
            {
                _doorTypesToSkip.Add(DoorType.NukeSurface);
                _doorTypesToSkip.Add(DoorType.ElevatorNuke);
            }

            if (_config.SkipUnknownDoors)
            {
                _doorTypesToSkip.Add(DoorType.UnknownDoor);
                _doorTypesToSkip.Add(DoorType.UnknownElevator);
            }

            if (_config.SkipElevators)
            {
                _doorTypesToSkip.Add(DoorType.UnknownElevator);
                _doorTypesToSkip.Add(DoorType.ElevatorGateA);
                _doorTypesToSkip.Add(DoorType.ElevatorGateB);
                _doorTypesToSkip.Add(DoorType.ElevatorLczA);
                _doorTypesToSkip.Add(DoorType.ElevatorLczB);
                _doorTypesToSkip.Add(DoorType.ElevatorNuke);
                _doorTypesToSkip.Add(DoorType.ElevatorScp049);
            }

            if (_config.SkipAirlocks)
            {
                _doorTypesToSkip.Add(DoorType.Airlock);
            }

            if (_config.SkipSCPRooms)
            {
                _doorTypesToSkip.Add(DoorType.Scp079First);
                _doorTypesToSkip.Add(DoorType.Scp079Second);
                _doorTypesToSkip.Add(DoorType.Scp049Gate);
                _doorTypesToSkip.Add(DoorType.ElevatorScp049);
                _doorTypesToSkip.Add(DoorType.Scp096);
                _doorTypesToSkip.Add(DoorType.Scp106Primary);
                _doorTypesToSkip.Add(DoorType.Scp106Secondary);
                _doorTypesToSkip.Add(DoorType.Scp173Bottom);
                _doorTypesToSkip.Add(DoorType.Scp173Gate);
                _doorTypesToSkip.Add(DoorType.Scp173NewGate);
                _doorTypesToSkip.Add(DoorType.Scp330);
                _doorTypesToSkip.Add(DoorType.Scp330Chamber);
                _doorTypesToSkip.Add(DoorType.Scp914Gate);
                _doorTypesToSkip.Add(DoorType.Scp914Door);
                _doorTypesToSkip.Add(DoorType.Scp939Cryo);
            }

            if (_config.SkipArmory)
            {
                _doorTypesToSkip.Add(DoorType.CheckpointArmoryA);
                _doorTypesToSkip.Add(DoorType.CheckpointArmoryB);
                _doorTypesToSkip.Add(DoorType.HczArmory);
                _doorTypesToSkip.Add(DoorType.LczArmory);
                _doorTypesToSkip.Add(DoorType.Scp049Armory);
                _doorTypesToSkip.Add(DoorType.Scp079Armory);
                _doorTypesToSkip.Add(DoorType.Scp173Armory);
            }

            if (_config.SkipCheckpoints || _config.SkipCheckpointsGate)
            {
                _doorTypesToSkip.Add(DoorType.CheckpointLczA);
                _doorTypesToSkip.Add(DoorType.CheckpointLczB);
                _doorTypesToSkip.Add(DoorType.CheckpointEzHczA);
                _doorTypesToSkip.Add(DoorType.CheckpointEzHczB);
                if (_config.SkipCheckpointsGate)
                {
                    _doorTypesToSkip.Add(DoorType.CheckpointGateA);
                    _doorTypesToSkip.Add(DoorType.CheckpointGateB);
                }
            }

            Library_ExiledAPI.LogDebug("Methods.InitializeDoorSkipList", $"Initialized door skip list with {_doorTypesToSkip.Count} door types.", _config.Debug);
        }

        #endregion

        #region Lockdown Logic

        /// <summary>
        /// Runs the lockdown timer, triggering lockdown events at intervals.
        /// </summary>
        /// <returns>An enumerator for the coroutine.</returns>
        public IEnumerator<float> StartLockdownTimer()
        {
            if (!_config.IsEnabled)
            {
                Library_ExiledAPI.LogInfo("StartLockdownTimer", "Lockdown timer skipped as plugin is disabled.");
                yield break;
            }

            yield return Timing.WaitForSeconds(_config.InitialDelay);
            Library_ExiledAPI.LogDebug("StartLockdownTimer", "Lockdown timer started.", _config.Debug);

            while (true)
            {
                float delay = _config.RandomEvents
                    ? Library_ExiledAPI.Loader_Random_Next(_config.DelayMin, _config.DelayMax)
                    : _config.InitialDelay;
                yield return Timing.WaitForSeconds(delay);
                yield return Timing.WaitUntilTrue(() => !Warhead.IsDetonated && !Warhead.IsInProgress);

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
            float lockdownDuration = GetLockdownDuration();
            bool lockdownOccurred = _config.UsePerRoomChances
                ? HandleRoomSpecificLockdown(lockdownDuration)
                : HandleZoneSpecificLockdown(lockdownDuration);

            Timing.RunCoroutine(FinalizeLockdownEvent(lockdownOccurred, lockdownDuration), TagLockdownFinalize);
        }

        private bool HandleZoneSpecificLockdown(float lockdownDuration)
        {
            bool isLockdownTriggered = false;

            isLockdownTriggered |= AttemptZoneLockdown(ZoneType.HeavyContainment, _config.ChanceHeavy, _config.CassieMessageHeavy, lockdownDuration);
            isLockdownTriggered |= AttemptZoneLockdown(ZoneType.LightContainment, _config.ChanceLight, _config.CassieMessageLight, lockdownDuration);
            isLockdownTriggered |= AttemptZoneLockdown(ZoneType.Entrance, _config.ChanceEntrance, _config.CassieMessageEntrance, lockdownDuration);
            isLockdownTriggered |= AttemptZoneLockdown(ZoneType.Surface, _config.ChanceSurface, _config.CassieMessageSurface, lockdownDuration);
            isLockdownTriggered |= AttemptZoneLockdown(ZoneType.Other, _config.ChanceOther, _config.CassieMessageOther, lockdownDuration);

            if (!isLockdownTriggered && _config.EnableFacilityLockdown)
            {
                TriggerFacilityWideLockdown(lockdownDuration);
                Library_ExiledAPI.LogDebug("HandleZoneSpecificLockdown", "Facility-wide lockdown triggered.", _config.Debug);
                isLockdownTriggered = true;
            }

            return isLockdownTriggered;
        }

        private bool AttemptZoneLockdown(ZoneType zone, float chance, string cassieMessage, float lockdownDuration)
        {
            if (Library_ExiledAPI.Loader_Random_NextDouble() * 100 < chance)
            {
                foreach (Room room in Room.List.Where(r => r.Zone == zone))
                {
                    LockdownRoom(room, lockdownDuration);
                }
                Library_ExiledAPI.LogDebug("AttemptZoneLockdown", $"Lockdown triggered in zone {zone} for {lockdownDuration} seconds.", _config.Debug);
                if (!_triggeredZones.Contains(zone))
                {
                    TriggerCassieMessage(cassieMessage);
                    _triggeredZones.Add(zone);
                }
                return true;
            }
            return false;
        }

        private bool HandleRoomSpecificLockdown(float lockdownDuration)
        {
            bool lockdownTriggered = false;

            foreach (Room room in Room.List)
            {
                if (AttemptRoomLockdown(room, lockdownDuration))
                {
                    lockdownTriggered = true;
                    Library_ExiledAPI.LogDebug("HandleRoomSpecificLockdown", $"Lockdown triggered in room {room.Name}.", _config.Debug);
                }
            }

            if (!lockdownTriggered && _config.EnableFacilityLockdown)
            {
                TriggerFacilityWideLockdown(lockdownDuration);
                Library_ExiledAPI.LogDebug("HandleRoomSpecificLockdown", "Facility-wide lockdown triggered.", _config.Debug);
                return true;
            }
            return lockdownTriggered;
        }

        private bool AttemptRoomLockdown(Room room, float lockdownDuration)
        {
            float chance;
            string cassieMessage;

            switch (room.Zone)
            {
                case ZoneType.HeavyContainment:
                    chance = _config.ChanceHeavy;
                    cassieMessage = _config.CassieMessageHeavy;
                    break;
                case ZoneType.LightContainment:
                    chance = _config.ChanceLight;
                    cassieMessage = _config.CassieMessageLight;
                    break;
                case ZoneType.Entrance:
                    chance = _config.ChanceEntrance;
                    cassieMessage = _config.CassieMessageEntrance;
                    break;
                case ZoneType.Surface:
                    chance = _config.ChanceSurface;
                    cassieMessage = _config.CassieMessageSurface;
                    break;
                default:
                    chance = _config.ChanceOther;
                    cassieMessage = _config.CassieMessageOther;
                    break;
            }

            if (Library_ExiledAPI.Loader_Random_NextDouble() * 100 < chance)
            {
                LockdownRoom(room, lockdownDuration);
                if (!_triggeredZones.Contains(room.Zone))
                {
                    TriggerCassieMessage(cassieMessage);
                    _triggeredZones.Add(room.Zone);
                }
                return true;
            }
            return false;
        }

        private void LockdownRoom(Room room, float duration)
        {
            bool anyDoorLocked = false;
            int lockedDoorCount = 0;

            foreach (Door door in room.Doors)
            {
                if (_doorTypesToSkip.Contains(door.Type))
                    continue;

                bool shouldLock = !_config.UsePerDoorChance || (Library_ExiledAPI.Loader_Random_NextDouble() * 100 < _config.ChancePerDoor);
                if (shouldLock)
                {
                    if (_config.CloseDoors)
                    {
                        door.IsOpen = false;
                        door.PlaySound(DoorBeepType.PermissionDenied);
                    }
                    if (!door.IsLocked)
                    {
                        door.Lock(duration, DoorLockType.Isolation);
                        door.PlaySound(DoorBeepType.LockBypassDenied);
                    }
                    anyDoorLocked = true;
                    lockedDoorCount++;
                }
            }

            if (anyDoorLocked)
            {
                room.Color = new Color(_config.LightsColorR, _config.LightsColorG, _config.LightsColorB);
                _changedRooms.Add(room);
                Library_ExiledAPI.LogDebug("LockdownRoom", $"Locked down room {room.Name} with {lockedDoorCount} doors affected for {duration} seconds.", _config.Debug);
            }
            else
            {
                Library_ExiledAPI.LogDebug("LockdownRoom", $"No doors were locked in room {room.Name} due to chance.", _config.Debug);
            }
        }

        private void TriggerFacilityWideLockdown(float lockdownDuration)
        {
            TriggerCassieMessage(_config.CassieMessageFacility);
            foreach (Room room in Room.List)
            {
                LockdownRoom(room, lockdownDuration);
            }
        }

        private IEnumerator<float> FinalizeLockdownEvent(bool lockdownOccurred, float lockdownDuration)
        {
            if (lockdownOccurred)
            {
                if (_config.Flicker)
                {
                    Timing.RunCoroutine(FlickerRoomLights(lockdownDuration), TagLockdownFlicker);
                }

                yield return Timing.WaitForSeconds(lockdownDuration);
                TriggerCassieMessage(_config.CassieMessageEnd);
                ResetRoomColors();
                yield return Timing.WaitForSeconds(8.0f);


                _changedRooms.Clear();
                _triggeredZones.Clear();
                Library_ExiledAPI.LogDebug("FinalizeLockdownEvent", "Lockdown completed. Systems reset.", _config.Debug);
            }
            else
            {
                TriggerCassieMessage(_config.CassieMessageWrong, true);
            }
        }

        private IEnumerator<float> FlickerRoomLights(float lockdownDuration)
        {
            float elapsedTime = 0f;
            float flickerFreq = _config.FlickerFrequency;
            float halfCycle = lockdownDuration / (2 * flickerFreq);

            while (elapsedTime < lockdownDuration)
            {
                yield return Timing.WaitForSeconds(halfCycle);
                elapsedTime += halfCycle;

                foreach (Room room in _changedRooms.ToList())
                {
                    if (!room.AreLightsOff)
                    {
                        room.TurnOffLights(halfCycle);
                    }

                    // Flicker lights in elevators connected to this room
                    LabApi.Features.Wrappers.Room labRoom = LabApi.Features.Wrappers.Room.Get(room.Identifier);
                    foreach (LabApi.Features.Wrappers.Elevator elevator in LabApi.Features.Wrappers.Elevator.List)
                    {
                        if (elevator.Rooms.Contains(labRoom))
                        {
                            foreach (LabApi.Features.Wrappers.Room elevatorRoom in elevator.Rooms)
                            {
                                foreach (LabApi.Features.Wrappers.LightsController controller in elevatorRoom.AllLightControllers)
                                {
                                    controller.FlickerLights(halfCycle);
                                }
                            }
                        }
                    }
                }


                yield return Timing.WaitForSeconds(halfCycle);
                elapsedTime += halfCycle;
            }
            Library_ExiledAPI.LogDebug("FlickerRoomLights", $"Completed flickering lights for {lockdownDuration} seconds.", _config.Debug);
        }

        #endregion

        #region CASSIE Management

        private void TriggerCassieMessage(string message, bool isGlitchy = false)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Library_ExiledAPI.LogDebug("TriggerCassieMessage", "CASSIE message is empty, skipping.", _config.Debug);
                return;
            }

            if (_cassieState != CassieStatus.Idle)
            {
                Library_ExiledAPI.LogDebug("TriggerCassieMessage", $"CASSIE busy ({_cassieState}), skipping: {message}", _config.Debug);
                return;
            }

            _cassieState = CassieStatus.Playing;
            Library_ExiledAPI.LogDebug("TriggerCassieMessage", $"Triggering CASSIE: {message}", _config.Debug);

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
            Library_ExiledAPI.LogDebug("CassieCooldownRoutine", "CASSIE cooldown completed.", _config.Debug);
        }

        #endregion

        #region Utility Methods

        private float GetLockdownDuration()
        {
            float duration = (float)Library_ExiledAPI.Loader_Random_NextDouble() * (_config.DurationMax - _config.DurationMin) + _config.DurationMin;
            Library_ExiledAPI.LogDebug("GetLockdownDuration", $"Calculated lockdown duration: {duration} seconds.", _config.Debug);
            return duration;
        }

        private void ResetRoomColors()
        {

            foreach (Room room in _changedRooms.ToList())
            {
                room.ResetColor();
                Library_ExiledAPI.LogDebug("ResetRoomColors", $"Reset color for room {room.Name}.", _config.Debug);
            }
        }

        #endregion
    }
}