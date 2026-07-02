namespace DoorRestartSystem
{
    using System;
    using LabApi.Loader.Features.Plugins;
    using EventHandler = DoorRestartSystem.Handlers.EventHandler;
    using Logger = DoorRestartSystem.Shared.Library_LabAPI;

    /// <summary>
    /// Central initialization bootstrap layer for the DoorRestartSystem plugin inside LabAPI.
    /// </summary>
    public class Plugin : Plugin<Config>
    {
        private EventHandler _eventHandler;
        private Methods _methods;

        /// <summary>
        /// Gets the singleton instance of the DoorRestartSystem plugin.
        /// </summary>
        public static Plugin Singleton { get; private set; }

        /// <summary>
        /// Gets the event handler instance.
        /// </summary>
        internal EventHandler EventHandler => _eventHandler;

        /// <summary>
        /// Gets the methods instance for managing door lockdown logic.
        /// </summary>
        internal Methods Methods => _methods;

        // Mandated LabAPI Abstract Overrides
        public override string Author => "iomatix";
        public override string Name => "DoorRestartSystem";
        public override string Description => "Automated door lockdown and facility containment system.";
        public override Version Version => new Version(6, 0, 0);
        public override Version RequiredApiVersion => new Version(1, 0, 0);

        /// <summary>
        /// Native LabAPI configuration framework hook.
        /// </summary>
        public override void LoadConfigs()
        {
            base.LoadConfigs();
            Config.Validate();
        }

        /// <summary>
        /// LabAPI structural entry point. Allocates components and binds server events.
        /// </summary>
        public override void Enable()
        {
            Singleton = this;

            try
            {
                _eventHandler = new EventHandler(this);
                _methods = new Methods(this);

                // Bind listeners against the native LabAPI backend engine event matrix
                LabApi.Events.Handlers.ServerEvents.RoundStarted += _eventHandler.OnRoundStarted;
                LabApi.Events.Handlers.ServerEvents.RoundEnded += _eventHandler.OnRoundEnded;
                LabApi.Events.Handlers.ServerEvents.WaitingForPlayers += _eventHandler.OnWaitingForPlayers;

                Logger.LogInfo(nameof(Plugin), $"{Name} (v{Version}) has been initialized successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogError(nameof(Plugin), $"Critical failure during {Name} initialization: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// LabAPI structural teardown execution layer. Guarantees clean resource release.
        /// </summary>
        public override void Disable()
        {
            if (_eventHandler != null)
            {
                // Safely detach listeners to prevent permanent domain memory wrapping
                LabApi.Events.Handlers.ServerEvents.RoundStarted -= _eventHandler.OnRoundStarted;
                LabApi.Events.Handlers.ServerEvents.RoundEnded -= _eventHandler.OnRoundEnded;
                LabApi.Events.Handlers.ServerEvents.WaitingForPlayers -= _eventHandler.OnWaitingForPlayers;

                try
                {
                    _eventHandler.Cleanup();
                }
                catch (Exception ex)
                {
                    Logger.LogError(nameof(Plugin), ($"Error during event handler resource reclamation: {ex.Message}"));
                }
            }

            // Sever memory references immediately for the Garbage Collector
            _eventHandler = null;
            _methods = null;
            Singleton = null;

            Logger.LogInfo(nameof(Plugin), $"{Name} has been fully deactivated.");
        }
    }
}