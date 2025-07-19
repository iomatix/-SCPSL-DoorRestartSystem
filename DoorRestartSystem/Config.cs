namespace DoorRestartSystem
{
    using System.ComponentModel;
    using Exiled.API.Interfaces;

    /// <summary>
    /// Configuration settings for the DoorRestartSystem plugin, controlling lockdown behavior and CASSIE announcements.
    /// </summary>
    public class Config : IConfig
    {
        #region General Settings

        /// <summary>
        /// Gets or sets a value indicating whether the DoorRestartSystem plugin is enabled.
        /// </summary>
        [Description("Enable or disable DoorRestartSystem.")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether debug logging is enabled.
        /// </summary>
        [Description("Enables debugging.")]
        public bool Debug { get; set; } = false;

        /// <summary>
        /// Gets or sets the percentage chance that a round includes DoorRestartSystem events.
        /// </summary>
        [Description("The chance that a Round even has DoorSystemRestarts")]
        public float Spawnchance
        {
            get => _spawnchance;
            set => _spawnchance = value < 0f ? 0f : value > 100f ? 100f : value;
        }
        private float _spawnchance = 55f;

        #endregion

        #region Door Settings

        /// <summary>
        /// Gets or sets a value indicating whether doors should close during a lockdown.
        /// </summary>
        [Description("Should doors close during lockdown?")]
        public bool CloseDoors { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether nuke surface door and HCZ elevator should be ignored.
        /// </summary>
        [Description("Should nuke surface door and hcz elevator be ignored?")]
        public bool SkipNukeDoors { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether unknown doors and elevators should be ignored.
        /// </summary>
        [Description("Should unknown doors and elevators be ignored?")]
        public bool SkipUnknownDoors { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether all elevators should be ignored.
        /// </summary>
        [Description("Should all elevators be ignored?")]
        public bool SkipElevators { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether all airlocks should be ignored.
        /// </summary>
        [Description("Should all airlocks be ignored?")]
        public bool SkipAirlocks { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether all SCP rooms should be ignored.
        /// </summary>
        [Description("Should all scp rooms be ignored?")]
        public bool SkipSCPRooms { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether all armory doors should be ignored.
        /// </summary>
        [Description("Should all armory doors be ignored?")]
        public bool SkipArmory { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether all checkpoint doors should be ignored.
        /// </summary>
        [Description("Should all checkpoints doors be ignored?")]
        public bool SkipCheckpoints { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether checkpoint gates should be ignored, independent of SkipCheckpoints.
        /// </summary>
        [Description("Should checkpoints gates be ignored? Independents from SkipCheckpoints")]
        public bool SkipCheckpointsGate { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether doors within a room should be locked randomly based on ChancePerDoor.
        /// </summary>
        [Description("Change this to true if want to disable doors randomly within the room.")]
        public bool UsePerDoorChange { get; set; } = false;

        /// <summary>
        /// Gets or sets the percentage chance of locking a door when UsePerDoorChange is true.
        /// </summary>
        [Description("Percentage chance of an outage per door if UsePerDoorChange is set to true.")]
        public int ChancePerDoor
        {
            get => _chancePerDoor;
            set => _chancePerDoor = value < 0 ? 0 : value > 100 ? 100 : value;
        }
        private int _chancePerDoor = 65;

        #endregion

        #region Timing Settings

        /// <summary>
        /// Gets or sets the initial delay (in seconds) before the first lockdown can occur.
        /// </summary>
        [Description("The initial delay (in seconds) before the first Door Restart can happen")]
        public int InitialDelay
        {
            get => _initialDelay;
            set => _initialDelay = value < 0 ? 0 : value;
        }
        private int _initialDelay = 60;

        /// <summary>
        /// Gets or sets the minimum duration (in seconds) of a lockdown.
        /// </summary>
        [Description("The Minimum Duration of the Lockdown")]
        public int DurationMin
        {
            get => _durationMin;
            set => _durationMin = value < 0 ? 0 : value;
        }
        private int _durationMin = 10;

        /// <summary>
        /// Gets or sets the maximum duration (in seconds) of a lockdown.
        /// </summary>
        [Description("The Maximum Duration of the Lockdown")]
        public int DurationMax
        {
            get => _durationMax;
            set => _durationMax = value < 0 ? 0 : value;
        }
        private int _durationMax = 35;

        /// <summary>
        /// Gets or sets the minimum delay (in seconds) before the next lockdown.
        /// </summary>
        [Description("The The Minimum Delay before the next the Lockdown")]
        public int DelayMin
        {
            get => _delayMin;
            set => _delayMin = value < 0 ? 0 : value;
        }
        private int _delayMin = 60;

        /// <summary>
        /// Gets or sets the maximum delay (in seconds) before the next lockdown.
        /// </summary>
        [Description("The The Maximum Delay before the next the Lockdown")]
        public int DelayMax
        {
            get => _delayMax;
            set => _delayMax = value < 0 ? 0 : value;
        }
        private int _delayMax = 200;

        /// <summary>
        /// Gets or sets a value indicating whether randomized delays are used between lockdown events.
        /// </summary>
        [Description("Enable or disable randomized delay between lockdown events. If set to false the InitialDelay would be used instead to keep regular events.")]
        public bool RandomEvents { get; set; } = true;

        #endregion

        #region Lighting Settings

        /// <summary>
        /// Gets or sets a value indicating whether lights should flicker during a lockdown.
        /// </summary>
        [Description("Enable lighting flicker")]
        public bool Flicker { get; set; } = true;

        /// <summary>
        /// Gets or sets the frequency of light flickering during a lockdown. Higher values mean faster flickering.
        /// </summary>
        [Description("Flickering frequency. Higher the value faster the flickering.")]
        public float FlickerFrequency
        {
            get => _flickerFrequency;
            set => _flickerFrequency = value < 0f ? 0f : value;
        }
        private float _flickerFrequency = 2.5f;

        /// <summary>
        /// Gets or sets the red channel of the room lights' color during a lockdown (0.0 to 1.0).
        /// </summary>
        [Description("Red channel of the lights color in the room during lockdown")]
        public float LightsColorR
        {
            get => _lightsColorR;
            set => _lightsColorR = value < 0f ? 0f : value > 1f ? 1f : value;
        }
        private float _lightsColorR = 0.85f;

        /// <summary>
        /// Gets or sets the green channel of the room lights' color during a lockdown (0.0 to 1.0).
        /// </summary>
        [Description("Green channel of the lights color in the room during lockdown")]
        public float LightsColorG
        {
            get => _lightsColorG;
            set => _lightsColorG = value < 0f ? 0f : value > 1f ? 1f : value;
        }
        private float _lightsColorG = 0.07f;

        /// <summary>
        /// Gets or sets the blue channel of the room lights' color during a lockdown (0.0 to 1.0).
        /// </summary>
        [Description("Blue channel of the lights color in the room during lockdown")]
        public float LightsColorB
        {
            get => _lightsColorB;
            set => _lightsColorB = value < 0f ? 0f : value > 1f ? 1f : value;
        }
        private float _lightsColorB = 0.23f;

        #endregion

        #region CASSIE Settings

        /// <summary>
        /// Gets or sets a value indicating whether to clear the CASSIE message queue before important messages to prevent spam.
        /// </summary>
        [Description("Should cassie clear the messeage cue before important message to prevent spam?")]
        public bool CassieMessageClearBeforeImportant { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the countdown announcement is enabled.
        /// </summary>
        [Description("Enable CassieMessageCountdown announcement")]
        public bool IsCountdownEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the delay (in seconds) between the countdown and start messages when IsCountdownEnabled is true.
        /// </summary>
        [Description("The delay between the CassieMessageCountdown and the CassieMessageStart if IsCountdownEnabled is enabled.")]
        public float TimeBetweenSentenceAndStart
        {
            get => _timeBetweenSentenceAndStart;
            set => _timeBetweenSentenceAndStart = value < 0f ? 0f : value;
        }
        private float _timeBetweenSentenceAndStart = 11f;

        /// <summary>
        /// Gets or sets the glitch chance per word in CASSIE sentences.
        /// </summary>
        [Description("Glitch chance during message per word in CASSIE sentence.")]
        public float GlitchChance
        {
            get => _glitchChance;
            private set => _glitchChance = value < 0f ? 0f : value > 100f ? 100f : value;
        }
        private float _glitchChance = 10f;

        /// <summary>
        /// Gets or sets the jam chance per word in CASSIE sentences.
        /// </summary>
        [Description("Jam chance during message per word in CASSIE sentence.")]
        public float JamChance
        {
            get => _jamChance;
            private set => _jamChance = value < 0f ? 0f : value > 100f ? 100f : value;
        }
        private float _jamChance = 5f;

        /// <summary>
        /// Gets or sets the CASSIE message played when no lockdown occurs.
        /// </summary>
        [Description("Message said by Cassie if no lockdown occurs")]
        public string CassieMessageWrong { get; set; } = ". I have avoided the system failure . .g5 Sorry for a .g3 . false alert .";

        /// <summary>
        /// Gets or sets the CASSIE countdown message played before a lockdown.
        /// </summary>
        [Description("Message said by Cassie just before a lockdown starts - Countdown - 3 . 2 . 1 announcement")]
        public string CassieMessageCountdown { get; set; } = "pitch_0.2 .g4 . .g4 pitch_1 door control system pitch_0.25 .g1 pitch_0.9 malfunction pitch_1 . initializing repair";

        /// <summary>
        /// Gets or sets the CASSIE message played at the start of a lockdown.
        /// </summary>
        [Description("Message said by Cassie on the lockdown start, delayed by time set on delayTimeBetweenSentenceAndStart if the countdown is enabled.")]
        public string CassieMessageStart { get; set; } = "door control system malfunction has been detected at .";

        /// <summary>
        /// Gets or sets the CASSIE message played for a facility-wide lockdown.
        /// </summary>
        [Description("Message said by Cassie after CassiePostMessage if lockdown gonna occur at whole site.")]
        public string CassieMessageFacility { get; set; } = "The Facility .";

        /// <summary>
        /// Gets or sets the CASSIE message played for an Entrance Zone lockdown.
        /// </summary>
        [Description("Message said by Cassie after CassiePostMessage if outage gonna occur at the Entrance Zone.")]
        public string CassieMessageEntrance { get; set; } = "The Entrance Zone .";

        /// <summary>
        /// Gets or sets the CASSIE message played for a Light Containment Zone lockdown.
        /// </summary>
        [Description("Message said by Cassie after CassiePostMessage if outage gonna occur at the Light Containment Zone.")]
        public string CassieMessageLight { get; set; } = "The Light Containment Zone .";

        /// <summary>
        /// Gets or sets the CASSIE message played for a Heavy Containment Zone lockdown.
        /// </summary>
        [Description("Message said by Cassie after CassiePostMessage if outage gonna occur at the Heavy Containment Zone.")]
        public string CassieMessageHeavy { get; set; } = "The Heavy Containment Zone.";

        /// <summary>
        /// Gets or sets the CASSIE message played for a Surface Zone lockdown.
        /// </summary>
        [Description("Message said by Cassie after CassiePostMessage if outage gonna occur at the entrance zone.")]
        public string CassieMessageSurface { get; set; } = "The Surface .";

        /// <summary>
        /// Gets or sets the CASSIE message played for an unspecified zone lockdown.
        /// </summary>
        [Description("Message said by Cassie after CassiePostMessage if outage gonna occur at random rooms in facility when zone is unknown or unspecified.")]
        public string CassieMessageOther { get; set; } = ". pitch_0.35 .g6 pitch_0.95 the malfunction is Unspecified .";

        /// <summary>
        /// Gets or sets the CASSIE sound played during a lockdown.
        /// </summary>
        [Description("The sound CASSIE will make during a lockdown.")]
        public string CassieKeter { get; set; } = "pitch_0.15 .g7";

        /// <summary>
        /// Gets or sets the CASSIE message played when a lockdown ends.
        /// </summary>
        [Description("The message CASSIE will say when a lockdown ends.")]
        public string CassieMessageEnd { get; set; } = "facility door control system is now operational";

        #endregion

        #region Probability Settings

        /// <summary>
        /// Gets or sets a value indicating whether a facility-wide lockdown occurs if no zones are selected.
        /// </summary>
        [Description("A lockdown in the whole facility will occur if none of the zones are selected randomly and EnableFacilityLockdown is set to true.")]
        public bool EnableFacilityLockdown { get; private set; } = true;

        /// <summary>
        /// Gets or sets the percentage chance of a lockdown in the Heavy Containment Zone.
        /// </summary>
        [Description("Percentage chance of an outage at the Heavy Containment Zone during the lockdown.")]
        public int ChanceHeavy
        {
            get => _chanceHeavy;
            set => _chanceHeavy = value < 0 ? 0 : value > 100 ? 100 : value;
        }
        private int _chanceHeavy = 99;

        /// <summary>
        /// Gets or sets the percentage chance of a lockdown in the Light Containment Zone.
        /// </summary>
        [Description("Percentage chance of an outage at the Light Containment Zone during the lockdown.")]
        public int ChanceLight
        {
            get => _chanceLight;
            set => _chanceLight = value < 0 ? 0 : value > 100 ? 100 : value;
        }
        private int _chanceLight = 45;

        /// <summary>
        /// Gets or sets the percentage chance of a lockdown in the Entrance Zone.
        /// </summary>
        [Description("Percentage chance of an outage at the Entrance Zone during the lockdown.")]
        public int ChanceEntrance
        {
            get => _chanceEntrance;
            set => _chanceEntrance = value < 0 ? 0 : value > 100 ? 100 : value;
        }
        private int _chanceEntrance = 65;

        /// <summary>
        /// Gets or sets the percentage chance of a lockdown in the Surface Zone.
        /// </summary>
        [Description("Percentage chance of an outage at the Surface Zone during the lockdown.")]
        public int ChanceSurface
        {
            get => _chanceSurface;
            set => _chanceSurface = value < 0 ? 0 : value > 100 ? 100 : value;
        }
        private int _chanceSurface = 25;

        /// <summary>
        /// Gets or sets the percentage chance of a lockdown in an unspecified zone.
        /// </summary>
        [Description("Percentage chance of an outage at an unknown and unspecified type of zone during the lockdown.")]
        public int ChanceOther
        {
            get => _chanceOther;
            set => _chanceOther = value < 0 ? 0 : value > 100 ? 100 : value;
        }
        private int _chanceOther = 0;

        /// <summary>
        /// Gets or sets a value indicating whether to use per-room probability settings instead of per-zone settings.
        /// </summary>
        [Description("Change this to true if want to use per room probability settings instead of per zone settings. The script will check all rooms in the specified zone with its probability.")]
        public bool UsePerRoomChances { get; set; } = false;

        #endregion
    }
}