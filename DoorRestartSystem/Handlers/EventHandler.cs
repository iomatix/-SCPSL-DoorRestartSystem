using LabApi.Events.Arguments.ServerEvents;
using LabApi.Extensions;
using LabApi.Extensions.Misc;
using MEC;
using System;
using System.Collections.Generic;
using Logger = LabApi.Extensions.Misc.iLogger;

namespace DoorRestartSystem.Handlers
{
    /// <summary>
    /// Handles server-related events for the DoorRestartSystem plugin, managing the lifecycle of lockdown events.
    /// </summary>
    public class EventHandler
    {
        private readonly Plugin _plugin;
        private readonly List<CoroutineHandle> _coroutines;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandler"/> class.
        /// </summary>
        /// <param name="plugin">The plugin instance providing access to configuration and methods.</param>
        /// <exception cref="ArgumentNullException">Thrown if plugin is null.</exception>
        public EventHandler(Plugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin instance cannot be null.");
            _coroutines = new List<CoroutineHandle>();

            Logger.Debug(nameof(EventHandler), "EventHandler framework layer fully initialized.", _plugin.Debug);
        }

        /// <summary>
        /// Called when the round starts, potentially initiating the lockdown timer based on spawn chance.
        /// </summary>
        public void OnRoundStarted()
        {
            try
            {
                // Wykorzystujemy bezpieczny wątkowo i bezalokacyjny rzut prawdopodobieństwa
                if (!_plugin.Config.Spawnchance.RollSuccess())
                {
                    Logger.Debug(nameof(EventHandler), "Lockdown execution sequence skipped due to spawn chance matrix roll.", _plugin.Debug);
                    return;
                }

                _plugin.Methods.Init();
                _coroutines.Add(Timing.RunCoroutine(_plugin.Methods.StartLockdownTimer(), "LockdownTimer"));

                Logger.Info(nameof(EventHandler), "Lockdown chronological sequence successfully initialized for the current round cycle.");
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(EventHandler), $"Failed to initiate facility lockdown timer cascade: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the round ends, cleaning up active lockdown resources.
        /// </summary>
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
        /// Called when the server is waiting for players, resetting the lockdown system.
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
        /// Cleans up active coroutines and resets the lockdown system state data.
        /// </summary>
        internal void Cleanup()
        {
            // Bezalokacyjne uderzenie i wyczyszczenie pamięci podręcznej coroutine z naszego NuGeta
            _coroutines.KillAndClear();

            // Delegate secondary deep-cleaning routines to flush down structural dictionaries and tags
            _plugin.Methods.Clean();

            Logger.Debug(nameof(EventHandler), "System state cleanup completed. Structural threads aborted cleanly.", _plugin.Debug);
        }
    }
}