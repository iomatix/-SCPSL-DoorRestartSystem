namespace DoorRestartSystem.Shared.Runtime
{
    using Interactables.Interobjects.DoorUtils;
    using LabApi.Features.Wrappers;
    using System;
    using System.Collections.Generic;
    using Logger = LabApi.Extensions.Misc.iLogger;

    /// <summary>
    /// Thread-safe and allocation-free evaluator compiling string-based door lock configurations
    /// into native bitmasks to protect priority containment and administrative door locks.
    /// </summary>
    public sealed class DoorLockFilter
    {
        /// <summary>
        /// Gets the compiled native bitmask of all ignored lock reasons.
        /// </summary>
        public ushort IgnoredMask { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoorLockFilter"/> class by compiling string definitions.
        /// </summary>
        /// <param name="ignoredReasons">The string representation of DoorLockReason enum flags from configuration.</param>
        public DoorLockFilter(IEnumerable<string> ignoredReasons)
        {
            ushort mask = 0;

            if (ignoredReasons != null)
            {
                foreach (string entry in ignoredReasons)
                {
                    if (string.IsNullOrWhiteSpace(entry))
                        continue;

                    if (Enum.TryParse<DoorLockReason>(entry.Trim(), ignoreCase: true, out DoorLockReason parsedReason))
                    {
                        mask |= (ushort)parsedReason;
                    }
                    else
                    {
                        Logger.Warn(nameof(DoorLockFilter), $"Failed to parse DoorLockReason entry '{entry}'. Skipping.");
                    }
                }
            }

            IgnoredMask = mask;
            Logger.Info(nameof(DoorLockFilter), $"Bitmask compiled: 0x{IgnoredMask:X4} (Flags: {(DoorLockReason)IgnoredMask})");
        }

        /// <summary>
        /// Evaluates whether the targeted door wrapper currently holds any priority lock defined in the mask.
        /// </summary>
        /// <param name="door">The door wrapper to test.</param>
        /// <returns><c>true</c> if the door holds an ignored lock state and must be bypassed; otherwise, <c>false</c>.</returns>
        public bool IsIgnored(Door door)
        {
            if (door == null || door.IsDestroyed || IgnoredMask == 0)
                return false;

            return (door.Base.ActiveLocks & IgnoredMask) != 0;
        }
    }
}