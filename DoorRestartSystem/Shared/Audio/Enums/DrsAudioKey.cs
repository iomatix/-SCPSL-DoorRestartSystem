namespace DoorRestartSystem.Shared.Audio.Enums
{
    /// <summary>
    /// Architectural registration keys for DoorRestartSystem structural audio elements.
    /// Maps static embedded audio manifest resources to automated runtime lockdown cycles.
    /// </summary>
    public enum DrsAudioKey
    {
        /// <summary>
        /// Broadcasted globally when a lockdown triggers to alert all personnel across the zone
        /// </summary>
        LockdownSirenLoop,

        /// <summary>
        /// Spatialized heavy mechanical impact played when a room node isolates its doors
        /// </summary>
        MechanicalLockSlam,

        /// <summary>
        /// High-frequency ambient electrical distortion played while lights are flickering
        /// </summary>
        ElectricalBuzzLoop,

        /// <summary>
        /// Zone-wide structural clearance tone played during the system restoration phase
        /// </summary>
        LockdownReleaseGlobal
    }
}