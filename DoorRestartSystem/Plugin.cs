namespace DoorRestartSystem
{
    using System;
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Server = DoorRestartSystem.Handlers.Server;


    public class Plugin : Plugin<Config>
    {
        public override string Author => "iomatix & GameKuchen";
        public override string Name => "DoorRestartSystem";
        public override string Prefix => "DRS";
        public override Version Version => new Version(6, 5, 0);
        public override Version RequiredExiledVersion => new Version(9, 5, 0);
        internal Methods Methods { get; private set; }
        internal Server Server { get; private set; }
        public override PluginPriority Priority => PluginPriority.Medium;


        public override void OnEnabled()
        {
            RegisterEvents();
            Methods.Init();
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Methods.Clean();
            UnRegisterEvents();
            base.OnDisabled();
        }


        private void RegisterEvents()
        {
            Server = new Server(this);
            Methods = new Methods(this);
            Exiled.Events.Handlers.Server.RoundStarted += Server.OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded += Server.OnRoundEnded;
            Exiled.Events.Handlers.Server.WaitingForPlayers += Server.OnWaitingForPlayers;
        }

        private void UnRegisterEvents()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= Server.OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded -= Server.OnRoundEnded;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= Server.OnWaitingForPlayers;

            Server = null;
            Methods = null;
        }

    }
}