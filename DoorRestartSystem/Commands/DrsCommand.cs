namespace DoorRestartSystem.Commands
{
    using System;
    using CommandSystem;
    using RemoteAdmin;

    /// <summary>
    /// Administrative command router for DoorRestartSystem lifecycles.
    /// Handles manual runtime initialization, lockdown execution with custom durations, and emergency stops.
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
        public string[] Usage => new[] { "init/start", "trigger [seconds]", "stop/cancel" };

        /// <inheritdoc />
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            // Enforce access control to block standard players from manipulating critical network and door states mid-round
            if (sender is PlayerCommandSender playerSender && !playerSender.CheckPermission(PlayerPermissions.FacilityManagement))
            {
                response = "Transhandling rejected. You do not possess the required administrative clearance (FacilityManagement) to call this command.";
                return false;
            }

            if (arguments.Count == 0)
            {
                response = "Invalid command configuration. Structural sub-commands available:\n" +
                           " - init / start       : Force-initializes the system hooks if disabled by default config.\n" +
                           " - trigger [seconds]  : Requests an instantaneous lockdown event with an optional custom duration.\n" +
                           " - stop / cancel      : Safely aborts any active lockdown, unlocks all doors, and restores environment lights.";
                return false;
            }

            // Guard against null-reference exceptions if the command is fired before the framework assembly finishes loading
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

                    // Permit manual recovery if the automatic lifecycle initialization sequence was skipped or aborted
                    plugin.Config.IsEnabled = true;
                    plugin.Methods.Init();
                    response = "SUCCESS: DoorRestartSystem architecture has been forced online. Automated timers are now ticking.";
                    return true;

                case "trigger":
                    float customDuration = -1f;

                    // Evaluate optional parameters to allow gamemasters to inject dynamic horror pacing or event testing lengths
                    if (arguments.Count >= 2)
                    {
                        if (!float.TryParse(arguments.At(1), out customDuration) || customDuration <= 0)
                        {
                            response = "Syntax interpretation failure: Expected a positive numerical value for lockdown duration.";
                            return false;
                        }
                    }

                    plugin.Methods.ForceManualLockdown(customDuration);
                    response = customDuration > 0
                        ? $"SUCCESS: Forced lockdown event dispatched for {customDuration} seconds. Processing room states."
                        : "SUCCESS: Forced lockdown event dispatched with configuration-defined random duration. Processing room states.";
                    return true;

                case "stop":
                case "cancel":
                    // Emergency override to resolve soft-locked matches or clear administrative setups instantly
                    plugin.Methods.ForceStopLockdown();
                    response = "SUCCESS: Explicit runtime teardown committed. Active visual flickers stopped, room matrices purged, and standard facility physics restored.";
                    return true;

                default:
                    response = $"Syntax interpretation failure: '{subAction}' is not a recognized operational subcommand. Use 'init', 'trigger', or 'stop'.";
                    return false;
            }
        }
    }
}