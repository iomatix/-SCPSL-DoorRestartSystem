namespace DoorRestartSystem
{
    using System.ComponentModel;
    using UnityEngine;

    using Logger = LabApi.Extensions.Misc.iLogger;

    /// <summary>
    /// Configuration settings for the DoorRestartSystem plugin, controlling lockdown behavior and CASSIE announcements.
    /// </summary>
    public class Config : LabApi.Loader.Features.Configuration.LabApiConfig
    {
        #region General Settings

        [Description("Enable or disable DoorRestartSystem.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Enables debugging.")]
        public bool Debug { get; set; } = false;

        [Description("The chance that a Round even has DoorSystemRestarts")]
        public float Spawnchance { get; set; } = 55f;

        #endregion

        #region Door Settings

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
        public bool SkipCheckpointsGate { get; set; } = false;

        [Description("Change this to true if want to disable doors randomly within the room.")]
        public bool UsePerDoorChance { get; set; } = false;

        [Description("Percentage chance of an outage per door if UsePerDoorChance is set to true.")]
        public int ChancePerDoor { get; set; } = 65;

        #endregion

        #region Post-Lockdown Open Settings

        [Description("Should doors be explicitly OPENED (not just unlocked) after the lockdown ends?")]
        public bool OpenDoorsAfterLockdown { get; set; } = true;

        [Description("If set to true, ONLY checkpoint doors/gates will be forced open after lockdown. Ignored if OpenDoorsAfterLockdown is false.")]
        public bool OpenOnlyCheckpoints { get; set; } = true;

        [Description("Percentage chance (0-100) that the post-lockdown door opening behavior will trigger successfully.")]
        public int OpenDoorsChance { get; set; } = 45;

        #endregion

        #region Timing Settings

        [Description("The initial delay (in seconds) before the first Door Restart can happen")]
        public int InitialDelay { get; set; } = 60;

        [Description("The Minimum Duration of the Lockdown")]
        public int DurationMin { get; set; } = 10;

        [Description("The Maximum Duration of the Lockdown")]
        public int DurationMax { get; set; } = 35;

        [Description("The The Minimum Delay before the next the Lockdown")]
        public int DelayMin { get; set; } = 60;

        [Description("The The Maximum Delay before the next the Lockdown")]
        public int DelayMax { get; set; } = 200;

        [Description("Enable or disable randomized delay between lockdown events. If set to false the InitialDelay would be used instead to keep regular events.")]
        public bool RandomEvents { get; set; } = true;

        #endregion

        #region Lighting Settings

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

        #endregion

        #region CASSIE Settings

        [Description("Should cassie clear the messeage cue before important message to prevent spam?")]
        public bool CassieMessageClearBeforeImportant { get; set; } = true;

        [Description("Enable CassieMessageCountdown announcement")]
        public bool IsCountdownEnabled { get; set; } = false;

        [Description("The delay between the CassieMessageCountdown and the CassieMessageStart if IsCountdownEnabled is enabled.")]
        public float TimeBetweenSentenceAndStart { get; set; } = 11f;

        [Description("Glitch chance during message per word in CASSIE sentence.")]
        public float GlitchChance { get; set; } = 10f;

        [Description("Jam chance during message per word in CASSIE sentence.")]
        public float JamChance { get; set; } = 5f;

        [Description("Message said by Cassie if no lockdown occurs")]
        public string CassieMessageWrong { get; set; } = ". I have avoided the system failure . .g5 Sorry for a .g3 . false alert .";

        [Description("Message said by Cassie just before a lockdown starts - Countdown - 3 . 2 . 1 announcement")]
        public string CassieMessageCountdown { get; set; } = "pitch_0.2 .g4 . .g4 pitch_1 door control system pitch_0.25 .g1 pitch_0.9 malfunction pitch_1 . initializing repair";

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

        [Description("The message CASSIE will say when a lockdown ends.")]
        public string CassieMessageEnd { get; set; } = "facility door control system is now operational";

        #endregion

        #region Probability Settings

        [Description("A lockdown in the whole facility will occur if none of the zones are selected randomly and EnableFacilityLockdown is set to true.")]
        public bool EnableFacilityLockdown { get; set; } = true;

        [Description("Percentage chance of an outage at the Heavy Containment Zone during the lockdown.")]
        public int ChanceHeavy { get; set; } = 99;

        [Description("Percentage chance of an outage at the Light Containment Zone during the lockdown.")]
        public int ChanceLight { get; set; } = 45;

        [Description("Percentage chance of an outage at the Entrance Zone during the lockdown.")]
        public int ChanceEntrance { get; set; } = 65;

        [Description("Percentage chance of an outage at the Surface Zone during the lockdown.")]
        public int ChanceSurface { get; set; } = 25;

        [Description("Percentage chance of an outage at an unknown and unspecified type of zone during the lockdown.")]
        public int ChanceOther { get; set; } = 0;

        [Description("Change this to true if want to use per room probability settings instead of per zone settings. The script will check all rooms in the specified zone with its probability.")]
        public bool UsePerRoomChances { get; set; } = false;

        #endregion

        /// <summary>
        /// Validates configuration parameters, normalizes system thresholds, and corrects anomalous input configurations.
        /// </summary>
        public void Validate()
        {
            // Linear Probability and Outage Clamping Matrix
            Spawnchance = Mathf.Clamp(Spawnchance, 0f, 100f);
            ChancePerDoor = Mathf.Clamp(ChancePerDoor, 0, 100);
            ChanceHeavy = Mathf.Clamp(ChanceHeavy, 0, 100);
            ChanceLight = Mathf.Clamp(ChanceLight, 0, 100);
            ChanceEntrance = Mathf.Clamp(ChanceEntrance, 0, 100);
            ChanceSurface = Mathf.Clamp(ChanceSurface, 0, 100);
            ChanceOther = Mathf.Clamp(ChanceOther, 0, 100);
            GlitchChance = Mathf.Clamp(GlitchChance, 0f, 100f);
            JamChance = Mathf.Clamp(JamChance, 0f, 100f);
            OpenDoorsChance = Mathf.Clamp(OpenDoorsChance, 0, 100);

            // Establish Absolute Non-Negative Baselines via Linear Maximization
            InitialDelay = Mathf.Max(0, InitialDelay);
            DurationMin = Mathf.Max(0, DurationMin);
            DurationMax = Mathf.Max(0, DurationMax);
            DelayMin = Mathf.Max(0, DelayMin);
            DelayMax = Mathf.Max(0, DelayMax);
            TimeBetweenSentenceAndStart = Mathf.Max(0f, TimeBetweenSentenceAndStart);

            // Relational Threshold Guard: Duration Boundaries
            if (DurationMin > DurationMax)
            {
                Logger.Warn(nameof(Config), $"Relational Error: DurationMin ({DurationMin}s) was greater than DurationMax ({DurationMax}s). Executing tuple-swap correction...");
                (DurationMin, DurationMax) = (DurationMax, DurationMin); // Modern C# Tuple Swap Pattern
            }

            // Relational Threshold Guard: Delay Boundaries
            if (DelayMin > DelayMax)
            {
                Logger.Warn(nameof(Config), $"Relational Error: DelayMin ({DelayMin}s) was greater than DelayMax ({DelayMax}s). Executing tuple-swap correction...");
                (DelayMin, DelayMax) = (DelayMax, DelayMin); // Modern C# Tuple Swap Pattern
            }

            // Hardware Environmental Guards: Light Inversion Prevention
            if (FlickerFrequency <= 0f)
            {
                Logger.Warn(nameof(Config), $"Hardware Error: FlickerFrequency ({FlickerFrequency}) must be strictly positive. Reverting to factory baseline (2.5f).");
                FlickerFrequency = 2.5f;
            }

            // Native Render Engine Color Spectrum Channel Clamping
            LightsColorR = Mathf.Clamp(LightsColorR, 0f, 1f);
            LightsColorG = Mathf.Clamp(LightsColorG, 0f, 1f);
            LightsColorB = Mathf.Clamp(LightsColorB, 0f, 1f);
        }
    }
}