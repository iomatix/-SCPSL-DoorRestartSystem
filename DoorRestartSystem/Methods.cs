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
using DoorRestartSystem.Shared.Audio;
using DoorRestartSystem.Shared.Audio.Enums;

using Logger = LabApi.Extensions.Misc.iLogger;

namespace DoorRestartSystem
{
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
        private readonly HashSet<FacilityZone> _triggeredZones = new();
        private readonly Dictionary<int, int> _roomSirenSessions = new();
        private readonly List<int> _activeBuzzSessions = new();

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

            // Bulk clear all active background tracking loops via NuGet collection extension
            new[] { TagLockdownTimer, TagLockdownExec, TagLockdownFinalize, TagLockdownFlicker, TagCassieCooldown }.KillCoroutines();

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

                Timing.RunCoroutine(ExecuteLockdownPipeline(), TagLockdownExec);
            }
        }

        public void ForceManualLockdown(float customDuration, FacilityZone? targetZone = null, RoomName? targetRoom = null)
        {
            InterruptActivePipelines();
            ForceResetFacilityState();

            float duration = customDuration > 0 ? customDuration : GetRandomLockdownDuration();
            Timing.RunCoroutine(ExecuteLockdownPipeline(duration, skipCountdown: true, targetZone, targetRoom), TagLockdownExec);
        }

        public void ForceStopLockdown()
        {
            InterruptActivePipelines();
            CassieExtensions.CassieClear();
            ForceResetFacilityState();
        }

        private void InterruptActivePipelines()
        {
            new[] { TagLockdownExec, TagLockdownFinalize, TagLockdownFlicker }.KillCoroutines();

            foreach (int id in _activeBuzzSessions)
            {
                _audioManager.StopSession(id);
            }
            _activeBuzzSessions.Clear();
        }
        #endregion

        #region Operational Lockdown Engine
        private IEnumerator<float> ExecuteLockdownPipeline(
            float? customDuration = null,
            bool skipCountdown = false,
            FacilityZone? targetZone = null,
            RoomName? targetRoom = null)
        {
            HashSet<Room> targets = new();
            List<string> announcementParts = new();

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
                    double countdownDuration = TriggerCassieMessage(_config.CassieMessageCountdown, force: true);
                    yield return Timing.WaitForSeconds((float)countdownDuration + 0.5f);
                }

                string combinedPhrase = $"{_config.CassieMessageStart} {string.Join(" ", announcementParts)}";
                TriggerCassieMessage(combinedPhrase, force: true);

                float duration = customDuration ?? GetRandomLockdownDuration();

                foreach (Room room in targets)
                {
                    if (ProcessRoomLockdownExecution(room, duration))
                    {
                        _affectedRoomsMap[room.GameObject.GetInstanceID()] = room;
                    }
                }

                Timing.RunCoroutine(FinalizeLockdownEvent(duration, targets), TagLockdownFinalize);
            }
            else
            {
                TriggerCassieMessage(_config.CassieMessageWrong);
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
                if (!chance.RollSuccess()) continue;

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
                if (!chance.RollSuccess()) continue;

                targetRooms.Add(room);
                if (_triggeredZones.Add(room.Zone) && !string.IsNullOrWhiteSpace(message))
                {
                    announcementParts.Add(message);
                }
            }
        }

        private bool ProcessRoomLockdownExecution(Room room, float duration)
        {
            int roomInstanceId = room.GameObject.GetInstanceID();
            room.SetLightsColor(new Color(_config.LightsColorR, _config.LightsColorG, _config.LightsColorB));

            if (!_roomSirenSessions.ContainsKey(roomInstanceId))
            {
                int sirenId = _audioManager.PlayAtPosition(DrsAudioKey.LockdownSirenLoop, room.Position + new Vector3(0f, 3.5f, 0f), loop: true, customLifespan: duration);
                if (sirenId != 0)
                    _roomSirenSessions[roomInstanceId] = sirenId;
            }

            var eligibleDoors = room.Doors.Where(door => door != null
                && !(_config.SkipElevators && (door.GameObject.name.Contains("Elevator") || door.IsElevatorDoor()))
                && !(_config.SkipCheckpointsGate && room.Name.IsCheckpoint() && door.IsGate()));

            var targets = _config.UsePerDoorChance
                ? eligibleDoors.Where(_ => ((float)_config.ChancePerDoor).RollSuccess()).ToList()
                : eligibleDoors.ToList();

            if (targets.Count == 0) return false;

            if (_config.CloseDoors) targets.SetOpenState(false);
            targets.SetLockState(DoorLockReason.Isolation, true);

            if (_config.UsePerDoorChance)
            {
                foreach (Door door in targets)
                {
                    _audioManager.PlayAtPosition(DrsAudioKey.MechanicalLockSlam, door.Position);
                }
            }
            else
            {
                _audioManager.PlayAtPosition(DrsAudioKey.MechanicalLockSlam, room.Position);
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

            TriggerCassieMessage(_config.CassieMessageEnd, force: true);

            _audioManager.PlayGlobal(DrsAudioKey.LockdownReleaseGlobal);

            foreach (Room room in eventRooms)
            {
                if (room == null) continue;
                _affectedRoomsMap.Remove(room.GameObject.GetInstanceID());
                ReleaseRoomState(room);
            }

            HandlePostLockdownChaos(eventRooms);
        }

        private IEnumerator<float> FlickerRoomLights(float duration, HashSet<Room> eventRooms)
        {
            float elapsedTime = 0f;
            float halfCycle = 0.5f / _config.FlickerFrequency;

            foreach (Room room in eventRooms.Where(r => r != null))
            {
                int buzzId = _audioManager.PlayAtPosition(DrsAudioKey.ElectricalBuzzLoop, room.Position, loop: true, customLifespan: duration);
                if (buzzId != 0)
                    _activeBuzzSessions.Add(buzzId);
            }

            while (elapsedTime < duration)
            {
                yield return Timing.WaitForSeconds(halfCycle);
                elapsedTime += halfCycle;

                foreach (Room room in eventRooms.Where(r => r != null && r.LightController.LightsEnabled))
                {
                    room.LightController.FlickerLights(halfCycle);
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
            foreach (Room room in _affectedRoomsMap.Values.Where(r => r != null))
            {
                ReleaseRoomState(room);
            }
            _affectedRoomsMap.Clear();

            foreach (int id in _activeBuzzSessions)
            {
                _audioManager.StopSession(id);
            }
            _activeBuzzSessions.Clear();

            foreach (var kvp in _roomSirenSessions.ToList())
            {
                _audioManager.StopSession(kvp.Value);
            }
            _roomSirenSessions.Clear();

            _audioManager.Clean();
        }

        private void ReleaseRoomState(Room room)
        {
            int roomInstanceId = room.GameObject.GetInstanceID();

            if (_roomSirenSessions.TryGetValue(roomInstanceId, out int sirenId))
            {
                _audioManager.StopSession(sirenId);
                _roomSirenSessions.Remove(roomInstanceId);
            }

            room.SetLightsColor(Color.clear);
            room.Doors.SetLockState(DoorLockReason.Isolation, false);
        }

        private void HandlePostLockdownChaos(HashSet<Room> eventRooms)
        {
            if (!_config.OpenDoorsAfterLockdown || !((float)_config.OpenDoorsChance).RollSuccess())
                return;

            var targetDoors = eventRooms
                .Where(room => room != null && !IsRoomSkipped(room) && !(_config.OpenOnlyCheckpoints && !room.Name.IsCheckpoint()))
                .SelectMany(room => room.Doors)
                .Where(door => door != null && !door.IsLocked)
                .ToList();

            targetDoors.SetOpenState(true);

            Logger.Debug(nameof(Methods), $"Post-lockdown phase completed. Mechanically forced {targetDoors.Count} doors to open.", _plugin.Debug);
        }
        #endregion

        #region Radio Broadcast State Machine
        private double TriggerCassieMessage(string message, bool force = false)
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

            CassieExtensions.Cassie_Message(message);
            double duration = CassieExtensions.CalculateCassieMessageDuration(message);

            TagCassieCooldown.KillCoroutine();
            Timing.RunCoroutine(CassieCooldownRoutine(duration), TagCassieCooldown);
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