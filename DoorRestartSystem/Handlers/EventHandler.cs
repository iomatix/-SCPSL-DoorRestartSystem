using DoorRestartSystem.Shared.Runtime;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Extensions.Misc;
using MEC;
using System;
using Logger = LabApi.Extensions.Misc.iLogger;

namespace DoorRestartSystem.Handlers
{
    /// <summary>
    /// Handles server-related events for the DoorRestartSystem plugin, managing the lifecycle of lockdown events.
    /// </summary>
    public class EventHandler
    {
        private readonly Plugin _plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandler"/> class.
        /// </summary>
        /// <param name="plugin">The plugin instance providing access to configuration and methods.</param>
        /// <exception cref="ArgumentNullException">Thrown if plugin is null.</exception>
        public EventHandler(Plugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin instance cannot be null.");
            Logger.Debug(nameof(EventHandler), "EventHandler framework layer fully initialized.", _plugin.Debug);
        }

        /// <summary>
        /// Called when the round starts, initiating the automated lockdown timer loop based on spawn probability.
        /// </summary>
        public void OnRoundStarted()
        {
            try
            {
                // Utilize thread-safe and allocation-free spawn probability evaluation
                if (!_plugin.Config.Spawnchance.RollSuccess())
                {
                    Logger.Info(nameof(EventHandler), "Lockdown execution sequence skipped due to spawn chance matrix roll.");
                    return;
                }

                _plugin.Methods.Init();

                // Track handle cleanly inside the unified registry using a centralized tag constant
                CoroutineHandle timerHandle = Timing.RunCoroutine(_plugin.Methods.StartLockdownTimer(), DrsRegistry.TimerTag);
                DrsRegistry.RegisterHandle(timerHandle);

                Logger.Info(nameof(EventHandler), "Lockdown chronological sequence successfully initialized for the current round cycle.");
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(EventHandler), $"Failed to initiate facility lockdown timer cascade: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the round ends, reclaiming active lockdown loops and state mappings.
        /// </summary>
        /// <param name="ev">The round ended event arguments context.</param>
        public void OnRoundEnded(RoundEndedEventArgs ev)
        {
            try
            {
                Cleanup();
                Logger.Info(nameof(EventHandler), "Lockdown sub-system evaluation structures safely reclaimed post round-finalization.");
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(EventHandler), $"Failed to execute round-end resource reclamation: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the server is waiting for players, ensuring full state reset prior to round launch.
        /// </summary>
        public void OnWaitingForPlayers()
        {
            try
            {
                Cleanup();
                Logger.Info(nameof(EventHandler), "Lockdown infrastructure context completely reset under waiting-for-players gate context.");
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(EventHandler), $"Failed to clear operational pipeline during warm-up standby: {ex.Message}");
            }
        }

        /// <summary>
        /// Triggers a total teardown of background tracks and active thread objects.
        /// </summary>
        internal void Cleanup()
        {
            // Flush all active pipelines, cached handlers, and runtime trackers atomically
            _plugin.Methods.Clean();
        }
    }
}