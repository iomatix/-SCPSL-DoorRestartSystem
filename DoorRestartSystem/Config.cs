using LabApi.Extensions;
using LabApi.Loader.Features.Configuration;
using System.ComponentModel;
using Logger = LabApi.Extensions.Misc.iLogger;

namespace DoorRestartSystem
{
    /// <summary>
    /// Master configuration settings for the DoorRestartSystem plugin, controlling automated timing matrix tracks,
    /// zone-specific lockdown probabilities, color spectrum light rendering, and decoupled CASSIE announcements.
    /// </summary>
    public class Config : LabApiConfig
    {
        #region Factory Baseline Constants
        private const string DefaultMessageWrong = ". I have avoided the system failure . .g5 Sorry for a .g3 . false alert .";
        private const string DefaultMessageCountdown = "$pitch_0.2 .g4 . .g4 $pitch_1 door control system $pitch_0.25 .g1 $pitch_0.9 malfunction $pitch_1 . initializing repair";
        private const string DefaultMessageStart = "door control system malfunction has been detected at .";
        private const string DefaultMessageFacility = "The Facility .";
        private const string DefaultMessageEntrance = "The Entrance Zone .";
        private const string DefaultMessageLight = "The Light Containment Zone .";
        private const string DefaultMessageHeavy = "The Heavy Containment Zone.";
        private const string DefaultMessageSurface = "The Surface .";
        private const string DefaultMessageOther = ". $pitch_0.35 .g6 $pitch_0.95 the malfunction is Unspecified .";
        private const string DefaultMessageEnd = "facility door control system is now operational";
        #endregion

        #region General Settings
        [Description("Enable or disable the DoorRestartSystem plugin infrastructure entirely.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Enable enhanced debug logging statements within the server console.")]
        public bool Debug { get; set; } = false;

        [Description("The percentage probability chance (0% - 100%) that a round state features active automated system restarts.")]
        public float Spawnchance { get; set; } = 55f;
        #endregion

        #region Door Lockdown Settings
        [Description("Should doors forcibly close when a targeted room triggers a lockdown sequence?")]
        public bool CloseDoors { get; set; } = true;

        [Description("Should the Surface Alpha Warhead blast door and Heavy Containment Zone elevator bulkheads be ignored during lockdowns?")]
        public bool SkipNukeDoors { get; set; } = true;

        [Description("Should unregistered doors and unmapped elevator shafts be bypassed dynamically?")]
        public bool SkipUnknownDoors { get; set; } = true;

        [Description("Should all native elevator cabin doors be completely ignored during automated lockdowns?")]
        public bool SkipElevators { get; set; } = false;

        [Description("Should all Light Containment Zone transitional airlock corridors be completely ignored?")]
        public bool SkipAirlocks { get; set; } = false;

        [Description("Should all secure anomalous entity containment cells be completely ignored?")]
        public bool SkipSCPRooms { get; set; } = false;

        [Description("Should all high-security tactical weapons and munitions armory depots be completely ignored?")]
        public bool SkipArmory { get; set; } = true;

        [Description("Should all zone checkpoint transition airlocks be completely ignored?")]
        public bool SkipCheckpoints { get; set; } = true;

        [Description("Should checkpoint gates be completely ignored? Independent from standard SkipCheckpoints properties.")]
        public bool SkipCheckpointsGate { get; set; } = false;

        [Description("Toggle true to execute randomized components failures per door item within affected room boundaries.")]
        public bool UsePerDoorChance { get; set; } = false;

        [Description("Percentage rolling chance (0% - 100%) of an outage per door asset if UsePerDoorChance is toggled true.")]
        public int ChancePerDoor { get; set; } = 65;
        #endregion

        #region Post-Lockdown Open Settings
        [Description("Should affected door assets be explicitly forced OPEN (not just unlocked) after the lockdown sequence terminates?")]
        public bool OpenDoorsAfterLockdown { get; set; } = true;

        [Description("If set to true, ONLY checkpoint gates will be forced open post-lockdown. Ignored if OpenDoorsAfterLockdown is false.")]
        public bool OpenOnlyCheckpoints { get; set; } = true;

        [Description("Percentage probability chance (0% - 100%) that the post-lockdown door opening behavior triggers successfully.")]
        public float OpenDoorsChance { get; set; } = 45f;
        #endregion

        #region Timing Matrix Settings
        [Description("The initial delay in seconds executed on round start before the first restart threat calculation loop can execute.")]
        public int InitialDelay { get; set; } = 60;

        [Description("The minimum total operational duration window in seconds for an individual facility lockdown event.")]
        public int DurationMin { get; set; } = 10;

        [Description("The maximum total operational duration window in seconds for an individual facility lockdown event.")]
        public int DurationMax { get; set; } = 35;

        [Description("The minimum legal delay spacing window in seconds enforced between successive restart event cycles.")]
        public int DelayMin { get; set; } = 60;

        [Description("The maximum legal delay spacing window in seconds enforced between successive restart event cycles.")]
        public int DelayMax { get; set; } = 200;

        [Description("Enable randomized delay intervals between events. If false, InitialDelay acts as a regular static ticker loop.")]
        public bool RandomEvents { get; set; } = true;
        #endregion

        #region Visual Lighting Settings
        [Description("Enable localized environmental light flickering matrices across room sectors during an active lockdown state.")]
        public bool Flicker { get; set; } = true;

        [Description("Flickering frequency modifier. Higher value parameters accelerate light strobe cycles.")]
        public float FlickerFrequency { get; set; } = 2.5f;

        [Description("Normalized Red spectrum channel emission value (0.0 - 1.0) applied to light controllers during active room lockdowns.")]
        public float LightsColorR { get; set; } = 0.85f;

        [Description("Normalized Green spectrum channel emission value (0.0 - 1.0) applied to light controllers during active room lockdowns.")]
        public float LightsColorG { get; set; } = 0.07f;

        [Description("Normalized Blue spectrum channel emission value (0.0 - 1.0) applied to light controllers during active room lockdowns.")]
        public float LightsColorB { get; set; } = 0.23f;
        #endregion

        #region CASSIE Vocal Synthesis Settings
        [Description("Should CASSIE force-flush the message queue buffer before playing a critical alert to prevent broadcast overlapping?")]
        public bool CassieMessageClearBeforeImportant { get; set; } = true;

        [Description("Enable or disable the pre-lockdown vocal count warning sequence announcement.")]
        public bool IsCountdownEnabled { get; set; } = false;

        [Description("The probability percentage chance (0% - 100%) of an individual word sustaining structural glitch vocal modulation.")]
        public float GlitchChance { get; set; } = 0.112f;

        [Description("The probability percentage chance (0% - 100%) of an individual word sustaining terminal audio compression jamming.")]
        public float JamChance { get; set; } = 0.063f;

        [Description("Vocal broadcast warning structural zones prior to active locks engaging.")]
        public string CassieMessageCountdown { get; set; } = DefaultMessageCountdown;

        [Description("Vocal payload prefix sent at the exact second a lockdown event enters the active execution graph.")]
        public string CassieMessageStart { get; set; } = DefaultMessageStart;

        [Description("Vocal broadcast dispatched if the facility infrastructure avoids a projected power system crash.")]
        public string CassieMessageWrong { get; set; } = DefaultMessageWrong;

        [Description("Vocal payload extension appended if an outage encompasses all coordinates.")]
        public string CassieMessageFacility { get; set; } = DefaultMessageFacility;

        [Description("Vocal payload extension appended if an outage isolates the Entrance Zone.")]
        public string CassieMessageEntrance { get; set; } = DefaultMessageEntrance;

        [Description("Vocal payload extension appended if an outage isolates the Light Containment Zone.")]
        public string CassieMessageLight { get; set; } = DefaultMessageLight;

        [Description("Vocal payload extension appended if an outage isolates the Heavy Containment Zone.")]
        public string CassieMessageHeavy { get; set; } = DefaultMessageHeavy;

        [Description("Vocal payload extension appended if an outage isolates Surface sector structural quadrants.")]
        public string CassieMessageSurface { get; set; } = DefaultMessageSurface;

        [Description("Fallback broadcast token injected if an outage strikes untracked room targets.")]
        public string CassieMessageOther { get; set; } = DefaultMessageOther;

        [Description("The final cleanup notification phrase broadcasted globally when facility door controls are restored to standard baseline parameters.")]
        public string CassieMessageEnd { get; set; } = DefaultMessageEnd;
        #endregion

        #region Spatial Probability Settings
        [Description("Triggers a total facility containment drop if zero individual zones successfully clear their rolling chance gates.")]
        public bool EnableFacilityLockdown { get; set; } = true;

        [Description("Percentage rolling chance (0% - 100%) of a lockdown selecting the Heavy Containment Zone.")]
        public float ChanceHeavy { get; set; } = 99f;

        [Description("Percentage rolling chance (0% - 100%) of a lockdown selecting the Light Containment Zone.")]
        public float ChanceLight { get; set; } = 45f;

        [Description("Percentage rolling chance (0% - 100%) of a lockdown selecting the Entrance Zone.")]
        public float ChanceEntrance { get; set; } = 65f;

        [Description("Percentage rolling chance (0% - 100%) of a lockdown selecting the Surface Zone.")]
        public float ChanceSurface { get; set; } = 25f;

        [Description("Percentage rolling chance (0% - 100%) of a lockdown selecting an unmapped structural sector.")]
        public float ChanceOther { get; set; } = 0.0f;

        [Description("Toggle true to execute rolling probability checks per room object instead of grouping via entire zone sectors.")]
        public bool UsePerRoomChances { get; set; } = false;
        #endregion

        #region Validation Engine
        /// <summary>
        /// Validates system probabilities, enforces temporal scale constraints via fluent extensions, 
        /// and scrubs spacing corruptions to insulate synthesizers against sub-frame execution failures.
        /// </summary>
        public void Validate()
        {
            // Fluent API Upgrade: Clamp all chance parameters cleanly using fluent single-precision math limits
            Spawnchance = Spawnchance.Clamp(0f, 100f);
            ChancePerDoor = ChancePerDoor.Clamp(0, 100);
            ChanceHeavy = ChanceHeavy.Clamp(0, 100);
            ChanceLight = ChanceLight.Clamp(0, 100);
            ChanceEntrance = ChanceEntrance.Clamp(0, 100);
            ChanceSurface = ChanceSurface.Clamp(0, 100);
            ChanceOther = ChanceOther.Clamp(0, 100);
            GlitchChance = GlitchChance.Clamp(0f, 100f);
            JamChance = JamChance.Clamp(0f, 100f);
            OpenDoorsChance = OpenDoorsChance.Clamp(0, 100);

            // Fluent API Upgrade: Establish absolute non-negative baseline milestones via inline integer limiters
            InitialDelay = InitialDelay.LimitMin(0);
            DurationMin = DurationMin.LimitMin(0);
            DurationMax = DurationMax.LimitMin(0);
            DelayMin = DelayMin.LimitMin(0);
            DelayMax = DelayMax.LimitMin(0);

            // Relational Threshold Guard: Duration Boundaries (High-Performance Tuple Swap Pattern)
            if (DurationMin > DurationMax)
            {
                Logger.Warn(nameof(Config), $"Relational Warning: DurationMin ({DurationMin}s) exceeded Max ({DurationMax}s). Executing atomic tuple-swap correction...");
                (DurationMin, DurationMax) = (DurationMax, DurationMin);
            }

            // Relational Threshold Guard: Delay Boundaries (High-Performance Tuple Swap Pattern)
            if (DelayMin > DelayMax)
            {
                Logger.Warn(nameof(Config), $"Relational Warning: DelayMin ({DelayMin}s) exceeded Max ({DelayMax}s). Executing atomic tuple-swap correction...");
                (DelayMin, DelayMax) = (DelayMax, DelayMin);
            }

            // Hardware Environmental Guards: Light Inversion Prevention
            if (FlickerFrequency <= 0f)
            {
                Logger.Warn(nameof(Config), $"Hardware Execution Error: FlickerFrequency ({FlickerFrequency}) collapsed below zero. Reverting back to factory default baseline (2.5f).");
                FlickerFrequency = 2.5f;
            }

            // Fluent API Upgrade: Clamp color spectrum rendering arrays inline inside safe byte parameters (0.0 - 1.0)
            LightsColorR = LightsColorR.Clamp(0f, 1f);
            LightsColorG = LightsColorG.Clamp(0f, 1f);
            LightsColorB = LightsColorB.Clamp(0f, 1f);

            // DRY-Compliant Clean String Sanitization Matrix Mapping Constants
            CassieMessageCountdown = CassieMessageCountdown.SanitizeCassieString();
            CassieMessageStart = CassieMessageStart.SanitizeCassieString();
            CassieMessageWrong = CassieMessageWrong.SanitizeCassieString();
            CassieMessageFacility = CassieMessageFacility.SanitizeCassieString();
            CassieMessageEntrance = CassieMessageEntrance.SanitizeCassieString();
            CassieMessageLight = CassieMessageLight.SanitizeCassieString();
            CassieMessageHeavy = CassieMessageHeavy.SanitizeCassieString();
            CassieMessageSurface = CassieMessageSurface.SanitizeCassieString();
            CassieMessageOther = CassieMessageOther.SanitizeCassieString();
            CassieMessageEnd = CassieMessageEnd.SanitizeCassieString();

        }
        #endregion
    }
}