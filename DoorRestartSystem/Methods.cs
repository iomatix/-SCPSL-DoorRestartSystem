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
        private readonly DrsAudioManager _audioManager;

        private readonly HashSet<RoomName> _roomsToSkip = new();

        private readonly Dictionary<int, Room> _affectedRoomsMap = new();
        private readonly Dictionary<int, int> _roomSirenSessions = new();
        private readonly List<int> _activeBuzzSessions = new();

        private const string TagLockdownTimer = "DRS-LockdownTimer";
        private const string TagLockdownExec = "DRS-LockdownExec";
        private const string TagLockdownFinalize = "DRS-LockdownFinalize";
        private const string TagLockdownFlicker = "DRS-LockdownFlicker";

        public Methods(Plugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin instance cannot be null.");
            _config = _plugin.Config;
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
            ForceResetFacilityState();
            _roomsToSkip.Clear();
            _audioManager.Clean();

            Timing.KillCoroutines(TagLockdownTimer);
            Library_LabAPI.LogInfo("Methods.Clean", "DoorRestartSystem internal tracks flushed.");
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

        #region Core Loop & Manual Overrides

        public IEnumerator<float> StartLockdownTimer()
        {
            if (!_config.IsEnabled) yield break;

            yield return Timing.WaitForSeconds(_config.InitialDelay);

            while (true)
            {
                float delay = _config.RandomEvents
                    ? (float)Library_LabAPI.Loader_Random_Next(_config.DelayMin, _config.DelayMax)
                    : _config.InitialDelay;

                yield return Timing.WaitForSeconds(delay);
                yield return Timing.WaitUntilTrue(() => !Warhead.IsDetonated && !Warhead.IsDetonationInProgress);

                Timing.RunCoroutine(ExecuteLockdownPipeline(), TagLockdownExec);
            }
        }

        public void ForceManualLockdown(float customDuration)
        {
            InterruptActivePipelines();
            ForceResetFacilityState();

            float duration = customDuration > 0 ? customDuration : GetRandomLockdownDuration();
            Timing.RunCoroutine(ExecuteLockdownPipeline(duration, skipCountdown: true), TagLockdownExec);
        }

        public void ForceStopLockdown()
        {
            InterruptActivePipelines();
            Library_LabAPI.Cassie_Clear();
            ForceResetFacilityState();
        }

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

        #region Lockdown Core Pipeline

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
                if (_config.IsCountdownEnabled && !skipCountdown)
                {
                    Library_LabAPI.Cassie_GlitchyMessage(_config.CassieMessageCountdown, _config.GlitchChance / 100f, _config.JamChance / 100f);
                    yield return Timing.WaitForSeconds(_config.TimeBetweenSentenceAndStart);
                }

                string combinedPhrase = $"{_config.CassieMessageStart} {string.Join(" ", announcementParts)}";
                Library_LabAPI.Cassie_Message(combinedPhrase);

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
                Library_LabAPI.Cassie_GlitchyMessage(_config.CassieMessageWrong, _config.GlitchChance / 100f, _config.JamChance / 100f);
            }
        }

        #endregion

        #region Probability Evaluation

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
            HashSet<FacilityZone> addedZoneMessages = new();

            foreach (Room room in Room.List)
            {
                if (IsRoomSkipped(room)) continue;

                var (chance, message) = GetZoneSettings(room.Zone);
                if (Library_LabAPI.Loader_Random_NextDouble() * 100 < chance)
                {
                    targetRooms.Add(room);
                    if (addedZoneMessages.Add(room.Zone) && !string.IsNullOrWhiteSpace(message))
                    {
                        announcementParts.Add(message);
                    }
                }
            }
        }

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

        #region Environmental Control and Chaos

        private IEnumerator<float> FinalizeLockdownEvent(float duration, HashSet<Room> eventRooms)
        {
            if (_config.Flicker)
            {
                Timing.RunCoroutine(FlickerRoomLights(duration, eventRooms), TagLockdownFlicker);
            }

            yield return Timing.WaitForSeconds(duration);

            Library_LabAPI.Cassie_Message(_config.CassieMessageEnd);
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

            // Failsafe backup cleanup over remaining untracked keys
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

        #region Helpers & Mapping

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