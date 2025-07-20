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
        public override Version Version => new Version(7, 0, 1);

        /// <summary>
        /// Gets the minimum required Exiled version for compatibility.
        /// </summary>
        public override Version RequiredExiledVersion => new Version(9, 6, 0);

        /// <summary>
        /// Gets the priority of the plugin, determining load order.
        /// </summary>
        public override PluginPriority Priority => PluginPriority.Medium;

        /// <summary>
        /// Called when the plugin is enabled, initializing handlers and starting the system.
        /// </summary>
        public override void OnEnabled()
        {
            try
            {
                Singleton = this;
                InitializeComponents();
                RegisterEvents();
                Library_ExiledAPI.LogInfo("Plugin.OnEnabled", "DoorRestartSystem plugin enabled successfully.");
                base.OnEnabled();
            }
            catch (Exception ex)
            {
                Library_ExiledAPI.LogError("Plugin.OnEnabled", $"Failed to enable DoorRestartSystem: {ex.Message}");
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
                Singleton = null;
                Library_ExiledAPI.LogInfo("Plugin.OnDisabled", "DoorRestartSystem plugin disabled successfully.");
                base.OnDisabled();
            }
            catch (Exception ex)
            {
                Library_ExiledAPI.LogError("Plugin.OnDisabled", $"Failed to disable DoorRestartSystem: {ex.Message}");
                throw;
            }
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
        /// Unregisters event handlers and cleans up resources.
        /// </summary>
        private void UnregisterEvents()
        {
            if (_eventHandler != null)
            {
                Exiled.Events.Handlers.Server.RoundStarted -= _eventHandler.OnRoundStarted;
                Exiled.Events.Handlers.Server.RoundEnded -= _eventHandler.OnRoundEnded;
                Exiled.Events.Handlers.Server.WaitingForPlayers -= _eventHandler.OnWaitingForPlayers;
            }
            _eventHandler = null;
            _methods = null;
            Library_ExiledAPI.LogDebug("Plugin.UnregisterEvents", "Unregistered server event handlers and cleaned up resources.");
        }
    }
}