namespace DoorRestartSystem.Shared.Runtime
{
    using LabApi.Extensions;
    using MEC;
    using System.Collections.Generic;

    /// <summary>
    /// Centralized registry for thread execution tags and active Coroutine handles.
    /// Prevents memory leaks and ensures absolute string consistency across assemblies.
    /// </summary>
    public static class DrsRegistry
    {
        public const string TimerTag = "DRS_LockdownTimer";
        public const string ExecutionTag = "DRS_LockdownExec";
        public const string FinalizationTag = "DRS_LockdownFinalize";
        public const string FlickerTag = "DRS_LockdownFlicker";
        public const string CassieCooldownTag = "DRS_CassieCooldown";

        private static readonly List<CoroutineHandle> TrackedHandles = new List<CoroutineHandle>();

        /// <summary>
        /// Registers an active coroutine handle to safeguard against unmanaged execution tracking.
        /// </summary>
        /// <param name="handle">The active coroutine handle instance to track.</param>
        public static void RegisterHandle(CoroutineHandle handle)
        {
            if (handle.IsRunning)
                TrackedHandles.Add(handle);
        }

        /// <summary>
        /// Instantly terminates active lockdown execution loops and structural animation pipelines.
        /// </summary>
        public static void KillLockdownPipelines()
        {
            new[] { ExecutionTag, FinalizationTag, FlickerTag }.KillCoroutines();
        }

        /// <summary>
        /// Forces a comprehensive purge of all background processes and cleans structural memory blocks.
        /// </summary>
        public static void FlushAll()
        {
            KillLockdownPipelines();
            new[] { TimerTag, CassieCooldownTag }.KillCoroutines();
            TrackedHandles.KillAndClear();
        }
    }
}