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

        [Description("Enables debugging logs.")]
        public bool Debug { get; set; } = false;

        [Description("The percentage chance that a round will feature active Door System Restarts.")]
        public float Spawnchance { get; set; } = 55f;
        #endregion

        #region Door Lockdown Settings
        [Description("Should doors close during lockdown?")]
        public bool CloseDoors { get; set; } = true;

        [Description("Should the nuke surface door and HCZ elevator doors be ignored during lockdowns?")]
        public bool SkipNukeDoors { get; set; } = true;

        [Description("Should unknown doors and unmapped elevators be ignored?")]
        public bool SkipUnknownDoors { get; set; } = true;

        [Description("Should all elevator doors be ignored?")]
        public bool SkipElevators { get; set; } = false;

        [Description("Should all airlocks be ignored?")]
        public bool SkipAirlocks { get; set; } = false;

        [Description("Should all anomalous entity containment containment cells be ignored?")]
        public bool SkipSCPRooms { get; set; } = false;

        [Description("Should all high-value tactical armory doors be ignored?")]
        public bool SkipArmory { get; set; } = true;

        [Description("Should all zone checkpoint doors be ignored?")]
        public bool SkipCheckpoints { get; set; } = true;

        [Description("Should checkpoints gates be ignored? Independent from SkipCheckpoints configuration.")]
        public bool SkipCheckpointsGate { get; set; } = false;

        [Description("Set to true to toggle randomized component failures per door within affected rooms.")]
        public bool UsePerDoorChance { get; set; } = false;

        [Description("Percentage chance of an outage per door if UsePerDoorChance evaluates to true.")]
        public int ChancePerDoor { get; set; } = 65;
        #endregion

        #region Post-Lockdown Open Settings
        [Description("Should doors be explicitly OPENED (not just unlocked) after the lockdown sequence terminates?")]
        public bool OpenDoorsAfterLockdown { get; set; } = true;

        [Description("If set to true, ONLY checkpoint doors/gates will be forced open post-lockdown. Ignored if OpenDoorsAfterLockdown is false.")]
        public bool OpenOnlyCheckpoints { get; set; } = true;

        [Description("Percentage chance (0-100) that the post-lockdown door opening behavior will trigger successfully.")]
        public int OpenDoorsChance { get; set; } = 45;
        #endregion

        #region Timing Matrix Settings
        [Description("The initial delay (in seconds) before the first Door Restart loop can execute.")]
        public int InitialDelay { get; set; } = 60;

        [Description("The minimum duration threshold of a facility lockdown event (in seconds).")]
        public int DurationMin { get; set; } = 10;

        [Description("The maximum duration threshold of a facility lockdown event (in seconds).")]
        public int DurationMax { get; set; } = 35;

        [Description("The minimum delay spacing before the next randomized lockdown loop can cycle.")]
        public int DelayMin { get; set; } = 60;

        [Description("The maximum delay spacing before the next randomized lockdown loop can cycle.")]
        public int DelayMax { get; set; } = 200;

        [Description("Enable randomized delay intervals between events. If false, InitialDelay acts as a regular static ticker.")]
        public bool RandomEvents { get; set; } = true;
        #endregion

        #region Visual Lighting Settings
        [Description("Enable environmental light flickering matrices during room lockdowns.")]
        public bool Flicker { get; set; } = true;

        [Description("Flickering frequency modifier. Higher values cause faster light strobe cycles.")]
        public float FlickerFrequency { get; set; } = 2.5f;

        [Description("Red channel emission of the room lighting spectrum during lockdown states (0.0 - 1.0).")]
        public float LightsColorR { get; set; } = 0.85f;

        [Description("Green channel emission of the room lighting spectrum during lockdown states (0.0 - 1.0).")]
        public float LightsColorG { get; set; } = 0.07f;

        [Description("Blue channel emission of the room lighting spectrum during lockdown states (0.0 - 1.0).")]
        public float LightsColorB { get; set; } = 0.23f;
        #endregion

        #region CASSIE Vocal Synthesis Settings
        [Description("Should CASSIE flush the message queue buffer before playing a critical alert to prevent layout spam?")]
        public bool CassieMessageClearBeforeImportant { get; set; } = true;

        [Description("Enable the CassieMessageCountdown pre-lockdown vocal warning.")]
        public bool IsCountdownEnabled { get; set; } = false;

        [Description("Glitch injection probability percentage per word in zglitchowane sentences.")]
        public float GlitchChance { get; set; } = 10f;

        [Description("Audio compression jam probability percentage per word in zglitchowane sentences.")]
        public float JamChance { get; set; } = 5f;

        [Description("Vocal broadcast dispatched if the facility infrastructure avoids a projected crash.")]
        public string CassieMessageWrong { get; set; } = ". I have avoided the system failure . .g5 Sorry for a .g3 . false alert .";

        [Description("Vocal broadcast warning structural zones prior to active locks engaging.")]
        public string CassieMessageCountdown { get; set; } = "pitch_0.2 .g4 . .g4 pitch_1 door control system pitch_0.25 .g1 pitch_0.9 malfunction pitch_1 . initializing repair";

        [Description("Vocal payload prefix sent at the exact second a lockdown event enters the active execution graph.")]
        public string CassieMessageStart { get; set; } = "door control system malfunction has been detected at .";

        [Description("Vocal payload extension added if a lockdown encompasses all coordinates.")]
        public string CassieMessageFacility { get; set; } = "The Facility .";

        [Description("Vocal payload extension added if an outage isolates the Entrance Zone.")]
        public string CassieMessageEntrance { get; set; } = "The Entrance Zone .";

        [Description("Vocal payload extension added if an outage isolates the Light Containment Zone.")]
        public string CassieMessageLight { get; set; } = "The Light Containment Zone .";

        [Description("Vocal payload extension added if an outage isolates the Heavy Containment Zone.")]
        public string CassieMessageHeavy { get; set; } = "The Heavy Containment Zone.";

        [Description("Vocal payload extension added if an outage isolates the Surface sector.")]
        public string CassieMessageSurface { get; set; } = "The Surface .";

        [Description("Fallback broadcast injected if an outage strikes untracked room targets.")]
        public string CassieMessageOther { get; set; } = ". pitch_0.35 .g6 pitch_0.95 the malfunction is Unspecified .";

        [Description("Positional background static modulation sound asset deployed locally during lockdown flickers.")]
        public string CassieKeter { get; set; } = "pitch_0.15 .g7";

        [Description("The final cleanup phrase broadcasted globally when facility grid locks are fully vented.")]
        public string CassieMessageEnd { get; set; } = "facility door control system is now operational";
        #endregion

        #region Spatial Probability Settings
        [Description("Triggers a total facility containment drop if zero individual zones successfully clear their rolling chance gates.")]
        public bool EnableFacilityLockdown { get; set; } = true;

        [Description("Percentage rolling chance of a lockdown selecting the Heavy Containment Zone.")]
        public int ChanceHeavy { get; set; } = 99;

        [Description("Percentage rolling chance of a lockdown selecting the Light Containment Zone.")]
        public int ChanceLight { get; set; } = 45;

        [Description("Percentage rolling chance of a lockdown selecting the Entrance Zone.")]
        public int ChanceEntrance { get; set; } = 65;

        [Description("Percentage rolling chance of a lockdown selecting the Surface Zone.")]
        public int ChanceSurface { get; set; } = 25;

        [Description("Percentage rolling chance of a lockdown selecting an unmapped structural sector.")]
        public int ChanceOther { get; set; } = 0;

        [Description("Toggle true to execute rolling probability checks per room object instead of grouping via entire zones.")]
        public bool UsePerRoomChances { get; set; } = false;
        #endregion

        /// <summary>
        /// Validates configuration parameters, normalizes system thresholds, and mathematically clamps ranges.
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

            // Relational Threshold Guard: Duration Boundaries (High-Performance Tuple Swap Pattern)
            if (DurationMin > DurationMax)
            {
                Logger.Warn(nameof(Config), $"Relational Error: DurationMin ({DurationMin}s) was greater than DurationMax ({DurationMax}s). Executing tuple-swap correction...");
                (DurationMin, DurationMax) = (DurationMax, DurationMin); // Modern C# Tuple Swap Pattern
            }

            // Relational Threshold Guard: Delay Boundaries (High-Performance Tuple Swap Pattern)
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