namespace DoorRestartSystem
{
    using System.ComponentModel;
    using Exiled.API.Interfaces;


    public class Config : IConfig
    {
        [Description("Enable or disable DoorRestartSystem.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Should doors close during lockdown?")]
        public bool CloseDoors { get; set; } = true;

        [Description("Should nuke surface door and hcz elevator be ignored?")]
        public bool SkipNukeDoors { get; set; } = true;

        [Description("Should unknown doors and elevators be ignored?")]
        public bool SkipUnknownDoors { get; set; } = true;

        [Description("Should all elevators be ignored?")]
        public bool SkipElevators { get; set; } = false;

        [Description("Should all airlocks be ignored?")]
        public bool SkipAirlocks { get; set; } = false;

        [Description("Should all scp rooms be ignored?")]
        public bool SkipSCPRooms { get; set; } = false;

        [Description("Should all armory doors be ignored?")]
        public bool SkipArmory { get; set; } = true;

        [Description("Should all checkpoints doors be ignored?")]
        public bool SkipCheckpoints { get; set; } = true;

        [Description("Should checkpoints gates be ignored? Independents from SkipCheckpoints")]
        public bool skipCheckpointsGate { get; set; } = false;

        [Description("The InitialDelay before the first Door Restart can happen")]
        public int InitialDelay { get; set; } = 60;

        [Description("The Minimum Duration of the Lockdown")]
        public int DurationMin { get; set; } = 10;

        [Description("The Maximum Duration of the Lockdown")]
        public int DurationMax { get; set; } = 35;

        [Description("The The Minimum Delay before the next the Lockdown")]
        public int DelayMin { get; set; } = 60;

        [Description("The The Maximum Delay before the next the Lockdown")]
        public int DelayMax { get; set; } = 200;

        [Description("The chance that a Round even has DoorSystemRestarts")]
        public float Spawnchance { get; set; } = 55;

        //Custom Flicker Lights
        [Description("Enable lighting flicker")]
        public bool Flicker { get; set; } = true;

        [Description("Flickering frequency. Higher the value faster the flickering.")]
        public float FlickerFrequency { get; set; } = 2.5f;

        [Description("Red channel of the lights color in the room during lockdown")]
        public float LightsColorR { get; set; } = 0.85f;

        [Description("Green channel of the lights color in the room during lockdown")]
        public float LightsColorG { get; set; } = 0.07f;

        [Description("Blue channel of the lights color in the room during lockdown")]
        public float LightsColorB { get; set; } = 0.23f;
        // MESSAGES
        [Description("Glitch chance during message per word in CASSIE sentence.")]
        public float GlitchChance { get; private set; } = 10f;

        [Description("Jam chance during message per word in CASSIE sentence.")]
        public float JamChance { get; private set; } = 5f;

        [Description("Message said by Cassie if no lockdown occurs")]
        public string CassieMessageWrong { get; set; } = ". I have avoided the system failure . .g5 Sorry for a .g3 . false alert .";

        [Description("Enable CassieMessageCountdown announcement")]
        public bool IsCountdownEnabled { get; set; } = false;

        [Description("Message said by Cassie just before a lockdown starts - Countdown - 3 . 2 . 1 announcement")]
        public string CassieMessageCountdown { get; set; } = "pitch_0.2 .g4 . .g4 pitch_1 door control system pitch_0.25 .g1 pitch_0.9 malfunction pitch_1 . initializing repair";

        [Description("The delay between the CassieMessageCountdown and the CassieMessageStart if IsCountdownEnabled is enabled.")]
        public float TimeBetweenSentenceAndStart { get; set; } = 11f;

        [Description("Message said by Cassie on the lockdown start, delayed by time set on delayTimeBetweenSentenceAndStart if the countdown is enabled.")]
        public string CassieMessageStart { get; set; } = "door control system malfunction has been detected at .";

        [Description("Message said by Cassie after CassiePostMessage if lockdown gonna occur at whole site.")]
        public string CassieMessageFacility { get; set; } = "The Facility .";

        [Description("Message said by Cassie after CassiePostMessage if outage gonna occur at the Entrance Zone.")]
        public string CassieMessageEntrance { get; set; } = "The Entrance Zone .";

        [Description("Message said by Cassie after CassiePostMessage if outage gonna occur at the Light Containment Zone.")]
        public string CassieMessageLight { get; set; } = "The Light Containment Zone .";

        [Description("Message said by Cassie after CassiePostMessage if outage gonna occur at the Heavy Containment Zone.")]
        public string CassieMessageHeavy { get; set; } = "The Heavy Containment Zone.";

        [Description("Message said by Cassie after CassiePostMessage if outage gonna occur at the entrance zone.")]
        public string CassieMessageSurface { get; set; } = "The Surface .";

        [Description("Message said by Cassie after CassiePostMessage if outage gonna occur at random rooms in facility when zone is unknown or unspecified.")]
        public string CassieMessageOther { get; set; } = ". pitch_0.35 .g6 pitch_0.95 the malfunction is Unspecified .";

        [Description("The sound CASSIE will make during a lockdown.")]
        public string CassieKeter { get; set; } = "pitch_0.15 .g7";

        [Description("The message CASSIE will say when a lockdown ends.")]
        public string CassieMessageEnd { get; set; } = "facility door control system is now operational";

        [Description("Should cassie clear the messeage cue before important message to prevent spam?")]
        public bool CassieMessageClearBeforeImportant { get; set; } = true;

        // Probability 
        [Description("A lockdown in the whole facility will occur if none of the zones are selected randomly and EnableFacilityLockdown is set to true.")]
        public bool EnableFacilityLockdown { get; private set; } = true;

        [Description("Percentage chance of an outage at the Heavy Containment Zone during the lockdown.")]
        public int ChanceHeavy { get; set; } = 99;

        [Description("Percentage chance of an outage at the Heavy Containment Zone during the lockdown.")]
        public int ChanceLight { get; set; } = 45;

        [Description("Percentage chance of an outage at the Entrance Zone during the lockdown.")]
        public int ChanceEntrance { get; set; } = 65;

        [Description("Percentage chance of an outage at the Surface Zone during the lockdown.")]
        public int ChanceSurface { get; set; } = 25;

        [Description("Percentage chance of an outage at an unknown and unspecified type of zone during the lockdown.")]
        public int ChanceOther { get; set; } = 0;

        [Description("Change this to true if want to use per room probability settings instead of per zone settings. The script will check all rooms in the specified zone with its probability.")]
        public bool UsePerRoomChances { get; set; } = false;

        [Description("Enables debugging.")]
        public bool Debug { get; set; } = false;
    }
}
