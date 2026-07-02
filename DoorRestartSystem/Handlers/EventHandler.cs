namespace DoorRestartSystem.Handlers
{
    using System;
    using System.Collections.Generic;
    using MEC;
    using DoorRestartSystem.Shared;
    using LabApi.Events.Arguments.ServerEvents;

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
            Library_LabAPI.LogDebug("EventHandler.Constructor", "EventHandler initialized.");
        }

        /// <summary>
        /// Called when the round starts, potentially initiating the lockdown timer based on spawn chance.
        /// </summary>
        public void OnRoundStarted()
        {
            try
            {
                if (Exiled.Loader.Loader.Random.NextDouble() * 100 > _plugin.Config.Spawnchance)
                {
                    Library_LabAPI.LogDebug("EventHandler.OnRoundStarted", "Lockdown skipped due to spawn chance.");
                    return;
                }

                _plugin.Methods.Init();
                _coroutines.Add(Timing.RunCoroutine(_plugin.Methods.StartLockdownTimer(), "LockdownTimer"));
                Library_LabAPI.LogInfo("EventHandler.OnRoundStarted", "Lockdown timer started for the round.");
            }
            catch (Exception ex)
            {
                Library_LabAPI.LogError("EventHandler.OnRoundStarted", $"Failed to start lockdown timer: {ex.Message}");
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
                Library_LabAPI.LogInfo("EventHandler.OnRoundEnded", "Lockdown system cleaned up after round end.");
            }
            catch (Exception ex)
            {
                Library_LabAPI.LogError("EventHandler.OnRoundEnded", $"Failed to clean up lockdown system: {ex.Message}");
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
                Library_LabAPI.LogInfo("EventHandler.OnWaitingForPlayers", "Lockdown system reset while waiting for players.");
            }
            catch (Exception ex)
            {
                Library_LabAPI.LogError("EventHandler.OnWaitingForPlayers", $"Failed to reset lockdown system: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up active coroutines and resets the lockdown system state data.
        /// </summary>
        internal void Cleanup()
        {
            // Terminate thread structures securely to prevent dangling references inside the MEC system core
            foreach (CoroutineHandle handle in _coroutines)
            {
                if (handle.IsRunning)
                {
                    Timing.KillCoroutines(handle);
                }
            }
            _coroutines.Clear();

            // Delegate secondary deep-cleaning routines to flush down structural dictionaries and tags
            _plugin.Methods.Clean();
            Library_LabAPI.LogDebug("EventHandler.Cleanup", "All coroutines terminated and system cleaned.");
        }
    }
}