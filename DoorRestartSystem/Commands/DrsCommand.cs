using CommandSystem;
using DoorRestartSystem.Shared.Runtime;
using LabApi.Extensions;
using MapGeneration;
using MEC;
using RemoteAdmin;
using System;
using System.Collections.Generic;

namespace DoorRestartSystem.Commands
{
    /// <summary>
    /// Administrative command router for DoorRestartSystem lifecycles.
    /// Handles manual runtime initialization, emergency stops, and global or targeted lockdown execution.
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class DrsCommand : ICommand, IUsageProvider
    {
        /// <inheritdoc />
        public string Command => "drs";

        /// <inheritdoc />
        public string[] Aliases => new[] { "lockdown", "doorrestart", "doorrestartsystem" };

        /// <inheritdoc />
        public string Description => "Administrative control interface for managing facility lockdown sequences.";

        /// <inheritdoc />
        public string[] Usage => new[]
        {
            "init / start",
            "trigger [seconds]",
            "zone [zoneName] [seconds]",
            "room [roomName] [seconds]",
            "stop / cancel"
        };

        private static readonly IReadOnlyDictionary<string, FacilityZone> ZoneAliases =
            new Dictionary<string, FacilityZone>(StringComparer.OrdinalIgnoreCase)
            {
                { "lcz", FacilityZone.LightContainment },
                { "light", FacilityZone.LightContainment },
                { "hcz", FacilityZone.HeavyContainment },
                { "heavy", FacilityZone.HeavyContainment },
                { "ez", FacilityZone.Entrance },
                { "entrance", FacilityZone.Entrance },
                { "surface", FacilityZone.Surface },
                { "other", FacilityZone.Other }
            };

        private static bool TryResolveFacilityZone(string input, out FacilityZone zone)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                zone = FacilityZone.None;
                return false;
            }

            if (ZoneAliases.TryGetValue(input, out zone))
            {
                return true;
            }

            if (Enum.TryParse(input, ignoreCase: true, out zone) && Enum.IsDefined(typeof(FacilityZone), zone))
            {
                return true;
            }

            zone = FacilityZone.None;
            return false;
        }

        /// <inheritdoc />
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is PlayerCommandSender playerSender && !playerSender.CheckPermission(PlayerPermissions.FacilityManagement))
            {
                response = "Transhandling rejected. You do not possess the required administrative clearance (FacilityManagement) to call this command.";
                return false;
            }

            if (arguments.Count == 0)
            {
                response = "Invalid command configuration. Structural sub-commands available:\n" +
                           " - init / start                    : Force-initializes the system hooks if disabled by default config.\n" +
                           " - trigger [seconds]               : Requests an instantaneous global facility lockdown event.\n" +
                           " - zone [zoneName] [seconds]       : Requests a targeted lockdown on a specific facility zone partition.\n" +
                           " - room [roomName] [seconds]       : Requests a surgical lockdown on an individual room entity.\n" +
                           " - stop / cancel                   : Aborts active lockdowns, unlocks affected doors, and restores lights.";
                return false;
            }

            var plugin = Plugin.Singleton;
            if (plugin == null || plugin.Methods == null)
            {
                response = "Execution critical failure: The core DoorRestartSystem runtime singleton or method pipeline is unavailable.";
                return false;
            }

            string subAction = arguments.At(0).ToLower();

            // Intelligent Routing
            if (subAction == "trigger" && arguments.Count > 1)
            {
                string nextArg = arguments.At(1).ToLower();
                if (nextArg == "zone")
                {
                    // drs trigger zone [hcz] [seconds] -> drs zone [hcz] [seconds]
                    return HandleZoneLockdown(arguments, 2, plugin, out response);
                }
                if (nextArg == "room")
                {
                    // drs trigger room [roomName] [seconds] -> drs room [roomName] [seconds]
                    return HandleRoomLockdown(arguments, 2, plugin, out response);
                }
                if (TryResolveFacilityZone(nextArg, out _))
                {
                    // drs trigger [hcz] [seconds]
                    return HandleZoneLockdown(arguments, 1, plugin, out response);
                }
            }

            switch (subAction)
            {
                case "init":
                case "start":
                    if (plugin.Methods.IsSystemActive)
                    {
                        response = "Initialization canceled: DoorRestartSystem mechanisms are already active and running within this round state.";
                        return false;
                    }

                    plugin.Config.IsEnabled = true;
                    plugin.Methods.Init();
                    Timing.RunCoroutine(plugin.Methods.StartLockdownTimer(), DrsRegistry.TimerTag);

                    response = "SUCCESS: DoorRestartSystem architecture has been forced online. Automated timers are now ticking.";
                    return true;

                case "trigger":
                    if (!plugin.Config.IsEnabled)
                    {
                        response = "Command rejected: DoorRestartSystem is disabled in configuration. Run 'drs start' first.";
                        return false;
                    }

                    float? globalDuration = GetDurationArgument(arguments, 1);
                    plugin.Methods.ForceManualLockdown(globalDuration);

                    response = globalDuration > 0
                        ? $"SUCCESS: Forced global lockdown dispatched for {globalDuration} seconds."
                        : "SUCCESS: Forced global lockdown dispatched with configuration-defined random duration.";
                    return true;

                case "zone":
                    return HandleZoneLockdown(arguments, 1, plugin, out response);

                case "room":
                    return HandleRoomLockdown(arguments, 1, plugin, out response);

                case "stop":
                case "cancel":
                    plugin.Methods.ForceStopLockdown();
                    response = "SUCCESS: Explicit runtime teardown committed. Active visual flickers stopped, room matrices purged, and standard facility physics restored.";
                    return true;

                default:
                    response = $"Syntax interpretation failure: '{subAction}' is not a recognized operational subcommand.";
                    return false;
            }
        }

        private static bool HandleZoneLockdown(ArraySegment<string> arguments, int zoneArgIndex, Plugin plugin, out string response)
        {
            if (arguments.Count <= zoneArgIndex)
            {
                response = $"Syntax error. Usage: drs zone [ {string.Join(" | ", ZoneAliases.Keys)} ] [seconds]";
                return false;
            }

            if (!plugin.Config.IsEnabled)
            {
                response = "Command rejected: DoorRestartSystem is disabled in configuration. Run 'drs start' first.";
                return false;
            }

            string rawZoneInput = arguments.At(zoneArgIndex);
            if (!TryResolveFacilityZone(rawZoneInput, out FacilityZone targetZone) || targetZone == FacilityZone.None)
            {
                response = $"Interpretation failure: '{rawZoneInput}' could not be resolved into a valid FacilityZone identifier.";
                return false;
            }

            float? zoneDuration = GetDurationArgument(arguments, zoneArgIndex + 1);
            plugin.Methods.ForceManualLockdown(zoneDuration, targetZone: targetZone);

            response = zoneDuration > 0
                ? $"SUCCESS: Forced targeted lockdown dispatched onto Zone [{targetZone}] for {zoneDuration} seconds."
                : $"SUCCESS: Forced targeted lockdown dispatched onto Zone [{targetZone}] with random duration.";
            return true;
        }

        private static bool HandleRoomLockdown(ArraySegment<string> arguments, int roomArgIndex, Plugin plugin, out string response)
        {
            if (arguments.Count <= roomArgIndex)
            {
                response = "Syntax error. Usage: drs room [roomName] [seconds]";
                return false;
            }

            if (!plugin.Config.IsEnabled)
            {
                response = "Command rejected: DoorRestartSystem is disabled in configuration. Run 'drs start' first.";
                return false;
            }

            RoomName targetRoom = arguments.At(roomArgIndex).ParseOrDefault(RoomName.Unnamed);
            if (targetRoom == RoomName.Unnamed)
            {
                response = $"Interpretation failure: '{arguments.At(roomArgIndex)}' is not a recognized or supported structural RoomName token.";
                return false;
            }

            float? roomDuration = GetDurationArgument(arguments, roomArgIndex + 1);
            plugin.Methods.ForceManualLockdown(roomDuration, targetRoom: targetRoom);

            response = roomDuration > 0
                ? $"SUCCESS: Surgical lockdown dispatched onto Room [{targetRoom}] for {roomDuration} seconds."
                : $"SUCCESS: Surgical lockdown dispatched onto Room [{targetRoom}] with random duration.";
            return true;
        }

        private static float? GetDurationArgument(ArraySegment<string> args, int index)
        {
            if (args.Count > index && float.TryParse(args.At(index), out float duration) && duration > 0)
            {
                return duration;
            }
            return null;
        }
    }
}