namespace DoorRestartSystem
{
    using System;
    using DoorRestartSystem.Shared;
    using Exiled.API.Enums;

    using EventHandler = Handlers.EventHandler;

    /// <summary>
    /// The main plugin class for the DoorRestartSystem, responsible for managing door lockdowns and related game mechanics.
    /// </summary>
    public class Plugin : Exiled.API.Features.Plugin<Config>
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

        /// <summary>
        /// Gets the author of the plugin.
        /// </summary>
        public override string Author => "iomatix";

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public override string Name => "DoorRestartSystem";

        /// <summary>
        /// Gets the prefix used for configuration and logging.
        /// </summary>
        public override string Prefix => "DRS";

        /// <summary>
        /// Gets the version of the plugin.
        /// </summary>
        public override Version Version => new Version(9, 0, 0);

        /// <summary>
        /// Gets the minimum required Exiled version for compatibility.
        /// </summary>
        public override Version RequiredExiledVersion => new Version(9, 9, 3);

        /// <summary>
        /// Gets the priority of the plugin, determining load order.
        /// </summary>
        public override PluginPriority Priority => PluginPriority.Medium;

        /// <summary>
        /// Called when the plugin is enabled, initializing handlers and starting the system.
        /// </summary>
        public override void OnEnabled()
        {
            Singleton = this;

            try
            {
                Config.Validate();
            }
            catch (Exception ex)
            {
                Library_ExiledAPI.LogError("Plugin.OnEnabled", $"Failed to validate config: {ex.Message}");
                Library_ExiledAPI.LogError("Plugin.OnEnabled", $"{Name} initialization aborted due to invalid configuration.");
                return;
            }

            try
            {
                InitializeComponents();
                RegisterEvents();
                Library_ExiledAPI.LogInfo("Plugin.OnEnabled", $"{Name} plugin enabled successfully.");
                base.OnEnabled();
            }
            catch (Exception ex)
            {
                Library_ExiledAPI.LogError("Plugin.OnEnabled", $"Failed to enable {Name}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Called when the plugin is disabled, cleaning up resources and unregistering handlers.
        /// </summary>
        public override void OnDisabled()
        {
            try
            {
                UnregisterEvents();
                Library_ExiledAPI.LogInfo("Plugin.OnDisabled", "Event handlers unregistered.");
            }
            catch (Exception ex)
            {
                Library_ExiledAPI.LogError("Plugin.OnDisabled", $"Error while unregistering events: {ex.Message}");
            }

            // Explicitly flush active background operations to guarantee zero memory or thread leak on hot-reloads
            if (_eventHandler != null)
            {
                try
                {
                    _eventHandler.Cleanup();
                }
                catch (Exception ex)
                {
                    Library_ExiledAPI.LogError("Plugin.OnDisabled", $"Error during event handler explicit reclamation: {ex.Message}");
                }
            }

            // Sever memory references immediately to allow the Garbage Collector to free the assembly instance allocation
            _eventHandler = null;
            _methods = null;
            Singleton = null;

            Library_ExiledAPI.LogInfo("Plugin.OnDisabled", $"{Name} plugin disabled successfully.");
            base.OnDisabled();
        }

        /// <summary>
        /// Initializes the plugin's core components, such as event handlers and methods.
        /// </summary>
        private void InitializeComponents()
        {
            _eventHandler = new EventHandler(this);
            _methods = new Methods(this);
            Library_ExiledAPI.LogDebug("Plugin.InitializeComponents", "Initialized event handler and methods.");
        }

        /// <summary>
        /// Registers event handlers for server-related events.
        /// </summary>
        private void RegisterEvents()
        {
            Exiled.Events.Handlers.Server.RoundStarted += _eventHandler.OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded += _eventHandler.OnRoundEnded;
            Exiled.Events.Handlers.Server.WaitingForPlayers += _eventHandler.OnWaitingForPlayers;
            Library_ExiledAPI.LogDebug("Plugin.RegisterEvents", "Registered server event handlers.");
        }

        /// <summary>
        /// Unregisters event handlers.
        /// </summary>
        private void UnregisterEvents()
        {
            if (_eventHandler != null)
            {
                Exiled.Events.Handlers.Server.RoundStarted -= _eventHandler.OnRoundStarted;
                Exiled.Events.Handlers.Server.RoundEnded -= _eventHandler.OnRoundEnded;
                Exiled.Events.Handlers.Server.WaitingForPlayers -= _eventHandler.OnWaitingForPlayers;
            }
        }
    }
}