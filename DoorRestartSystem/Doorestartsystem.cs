using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Extensions;
using Interactables.Interobjects.DoorUtils;
using MEC;

namespace DoorRestartSystem
{

    public class DoorRestartsystem : Plugin<Config>
    {
        public override string Author { get; } = "GameKuchen";
        public override string Name { get; } = "DoorRestartSystem";
        public override string Prefix { get; } = "DRS";
        public override Version Version { get; } = new Version(2, 5, 0);
        public Random Gen = new Random();
        public static DoorRestartsystem Singleton;
        private Handlers.Server server;
        private Handlers.Player player;
        public NineTailedFoxAnnouncer Respawn;
        public static bool TimerOn = true;
        System.Random random = new System.Random();
        public override PluginPriority Priority { get; } = PluginPriority.Medium;

        public override void OnEnabled()
        {
            base.OnEnabled();
            Singleton = this;
            registerEvents();
        }

        public override void OnDisabled()
        {
            base.OnDisabled();
            foreach (CoroutineHandle handle in server.Coroutines)
                Timing.KillCoroutines(handle);
            unRegisterEvents();
        }

        public static void softlockDoors()
        {
            foreach (DoorVariant door in Map.Doors)
            {
                door.ServerChangeLock(DoorLockReason.Warhead, true);
            }
        }

        private void registerEvents()
        {
            Log.Info("RegisterEvents");
            server = new Handlers.Server(this);
            player = new Handlers.Player(this);
            Exiled.Events.Handlers.Server.RoundStarted += server.OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded += server.OnRoundEnd;
            Exiled.Events.Handlers.Server.WaitingForPlayers += server.OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.TriggeringTesla += player.OnTriggerTesla;
        }

        private void unRegisterEvents()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= server.OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded -= server.OnRoundEnd;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= server.OnWaitingForPlayers;
            Exiled.Events.Handlers.Player.TriggeringTesla -= player.OnTriggerTesla;

            server = null;
            player = null;
        }

        public IEnumerator<float> RunBlackoutTimer()
        {
            Log.Info("Runblackouttimer");
            yield return Timing.WaitForSeconds(Config.InitialDelay);
            yield return Timing.WaitForSeconds((float)random.NextDouble() * (Config.DelayMax - Config.DelayMin) + Config.DelayMin);

            for (; ; )
            {
                yield return Timing.WaitUntilTrue(() => Warhead.IsDetonated || Warhead.IsInProgress);
                Cassie.Message(Config.DoorSentence, false, true);

                TimerOn = true;
                yield return Timing.WaitForSeconds(Config.TimeBetweenSentenceAndStart);
                if (Config.Countdown)
                {
                    Cassie.Message("3 . 2 . 1", false, true);
                    yield return Timing.WaitForSeconds(3f);
                }

                float BlackoutDur = (float)(random.NextDouble() * (Config.DurationMax - Config.DurationMin) + Config.DurationMin);


                foreach (DoorVariant door in Map.Doors)
                {
                   if(door.Type() != DoorType.NukeSurface)
                    {
                        Log.Info("locking the doors");
                        door.NetworkTargetState = false;
                        door.ServerChangeLock(DoorLockReason.SpecialDoorFeature, true);
                    }
                }
                yield return Timing.WaitForSeconds(BlackoutDur);
                foreach (DoorVariant door in Map.Doors)
                {
                    door.ServerChangeLock(DoorLockReason.AdminCommand, false);
                }
                Cassie.Message(Config.DoorAfterSentence, false, true);
                yield return Timing.WaitForSeconds((float)random.NextDouble() * (Config.DelayMax - Config.DelayMin) + Config.DelayMin);
                TimerOn = false;
            }
        }
    }
}
