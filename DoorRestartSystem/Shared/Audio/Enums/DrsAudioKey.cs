namespace DoorRestartSystem.Shared.Audio.Enums
{
    /// <summary>
    /// Architectural registration keys for DoorRestartSystem structural audio elements.
    /// Maps static embedded audio manifest resources to automated runtime lockdown cycles.
    /// </summary>
    public enum DrsAudioKey
    {
        // Broadcasted globally when a lockdown triggers to alert all personnel across the zone
        LockdownSirenGlobal,

        // Spatialized heavy mechanical impact played when a room node isolates its doors
        MechanicalLockSlam,

        // High-frequency ambient electrical distortion played while lights are flickering
        ElectricalBuzzLoop,

        // Zone-wide structural clearance tone played during the system restoration phase
        LockdownReleaseGlobal
    }
}