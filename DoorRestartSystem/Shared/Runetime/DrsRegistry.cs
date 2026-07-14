namespace DoorRestartSystem.Shared.Runtime
{
    using LabApi.Extensions;

    /// <summary>
    /// High-performance, zero-allocation registry for thread execution tags.
    /// Relies on MEC's native tag-tracking infrastructure to prevent memory leaks.
    /// </summary>
    public static class DrsRegistry
    {
        public const string TimerTag = "DRS_LockdownTimer";
        public const string ExecutionTag = "DRS_LockdownExec";
        public const string FinalizationTag = "DRS_LockdownFinalize";
        public const string FlickerTag = "DRS_LockdownFlicker";
        public const string CassieCooldownTag = "DRS_CassieCooldown";

        // FIX: Pre-allocated read-only arrays to eliminate heap allocations during cleanup cascades.
        private static readonly string[] LockdownPipelineTags = { ExecutionTag, FinalizationTag, FlickerTag };
        private static readonly string[] AuxiliaryTags = { TimerTag, CassieCooldownTag };

        /// <summary>
        /// Instantly terminates active lockdown execution loops and structural animation pipelines.
        /// </summary>
        public static void KillLockdownPipelines()
        {
            // FIX: Uses the pre-allocated zero-allocation array.
            LockdownPipelineTags.Kill();
        }

        /// <summary>
        /// Forces a comprehensive purge of all background processes and cleans structural memory blocks.
        /// </summary>
        public static void FlushAll()
        {
            KillLockdownPipelines();

            // FIX: Uses the pre-allocated zero-allocation array.
            AuxiliaryTags.Kill();
        }
    }
}