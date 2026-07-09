using CommandSystem;
using LabApi.Extensions;
using MapGeneration;
using RemoteAdmin;
using System;

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
        public string[] Aliases => new[] { "lockdown", "doorrestart" };

        /// <inheritdoc />
        public string Description => "Administrative control interface for managing facility lockdown sequences.";

        /// <inheritdoc />
        public string[] Usage => new[] { "init/start", "trigger [seconds]", "zone [zoneName] [seconds]", "room [roomName] [seconds]", "stop/cancel" };

        /// <inheritdoc />
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            // Enforce access control to block standard players from manipulating critical network and door states
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

            switch (subAction)
            {
                case "init":
                case "start":
                    if (plugin.Config.IsEnabled)
                    {
                        response = "Initialization canceled: DoorRestartSystem mechanisms are already active and running within this round state.";
                        return false;
                    }

                    plugin.Config.IsEnabled = true;
                    plugin.Methods.Init();
                    response = "SUCCESS: DoorRestartSystem architecture has been forced online. Automated timers are now ticking.";
                    return true;

                case "trigger":
                    float? globalDuration = GetDurationArgument(arguments, 1);
                    plugin.Methods.ForceManualLockdown(globalDuration);

                    response = globalDuration > 0
                        ? $"SUCCESS: Forced global lockdown dispatched for {globalDuration} seconds."
                        : "SUCCESS: Forced global lockdown dispatched with configuration-defined random duration.";
                    return true;

                case "zone":
                    if (arguments.Count < 2)
                    {
                        response = $"Syntax error. Usage: drs zone [ {string.Join(" | ", Enum.GetNames(typeof(FacilityZone)))} ] [seconds]";
                        return false;
                    }

                    // Fluent API implementation utilizing ParseOrDefault to process raw text securely
                    FacilityZone targetZone = arguments.At(1).ParseOrDefault(FacilityZone.None);
                    if (targetZone == FacilityZone.None)
                    {
                        response = $"Interpretation failure: '{arguments.At(1)}' could not be resolved into a valid FacilityZone identifier.";
                        return false;
                    }

                    float? zoneDuration = GetDurationArgument(arguments, 2);
                    plugin.Methods.ForceManualLockdown(zoneDuration, targetZone: targetZone);

                    response = zoneDuration > 0
                        ? $"SUCCESS: Forced targeted lockdown dispatched onto Zone [{targetZone}] for {zoneDuration} seconds."
                        : $"SUCCESS: Forced targeted lockdown dispatched onto Zone [{targetZone}] with random duration.";
                    return true;

                case "room":
                    if (arguments.Count < 2)
                    {
                        response = "Syntax error. Usage: drs room [roomName] [seconds]";
                        return false;
                    }

                    // Surgical room parsing leveraging the source-only enum abstraction framework
                    RoomName targetRoom = arguments.At(1).ParseOrDefault(RoomName.Unnamed);
                    if (targetRoom == RoomName.Unnamed)
                    {
                        response = $"Interpretation failure: '{arguments.At(1)}' is not a recognized or supported structural RoomName token.";
                        return false;
                    }

                    float? roomDuration = GetDurationArgument(arguments, 2);
                    plugin.Methods.ForceManualLockdown(roomDuration, targetRoom: targetRoom);

                    response = roomDuration > 0
                        ? $"SUCCESS: Surgical lockdown dispatched onto individual Room [{targetRoom}] for {roomDuration} seconds."
                        : $"SUCCESS: Surgical lockdown dispatched onto individual Room [{targetRoom}] with random duration.";
                    return true;

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

        /// <summary>
        /// Resolves and validates an optional duration command token parameter.
        /// </summary>
        /// <param name="args">The segments structure array holding execution text tokens.</param>
        /// <param name="index">The designated argument position index tracked inside the parameters layout.</param>
        /// <returns>A validated tracking <see cref="Nullable{Single}"/> containing duration scales, or null if unprovided.</returns>
        private float? GetDurationArgument(ArraySegment<string> args, int index)
        {
            if (args.Count > index && float.TryParse(args.At(index), out float duration) && duration > 0)
            {
                return duration;
            }
            return null;
        }
    }
}