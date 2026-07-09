namespace DoorRestartSystem.Shared.Runtime
{
    using LabApi.Extensions;
    using LabApi.Features.Wrappers;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Read-only snapshot storing pre-filtered structural room assets to eliminate hot-path LINQ allocations.
    /// </summary>
    public sealed class RoomLockdownContext
    {
        /// <summary>
        /// Gets the target room wrapper instance.
        /// </summary>
        public Room Room { get; }

        /// <summary>
        /// Gets the unique instance identifier of the room GameObject.
        /// </summary>
        public int RoomInstanceId { get; }

        /// <summary>
        /// Gets the cached array of standard non-elevator doors within the room boundary.
        /// </summary>
        public Door[] NormalDoors { get; }

        /// <summary>
        /// Gets the cached array of elevators linked directly to this room matrix.
        /// </summary>
        public Elevator[] ConnectedElevators { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomLockdownContext"/> class.
        /// </summary>
        /// <param name="room">The target room instance undergoing lockdown pre-processing.</param>
        /// <param name="skipCheckpointsGate">Enforces whether checkpoint gates are bypassed.</param>
        /// <param name="skipElevators">Enforces whether connected elevator systems are completely ignored.</param>
        public RoomLockdownContext(Room room, bool skipCheckpointsGate, bool skipElevators)
        {
            Room = room ?? throw new ArgumentNullException(nameof(room));
            RoomInstanceId = room.GameObject.GetInstanceID();

            // Allocation Optimization: Single pass explicit conversion loop without runtime LINQ overhead
            var doorsList = new List<Door>();
            foreach (var door in room.Doors)
            {
                if (door == null || door.IsElevatorDoor())
                    continue;

                if (skipCheckpointsGate && room.Name.IsCheckpoint() && door.IsGate())
                    continue;

                doorsList.Add(door);
            }
            NormalDoors = doorsList.ToArray();

            ConnectedElevators = !skipElevators
                ? room.GetElevatorsConnectedToRoom().ToArray()
                : Array.Empty<Elevator>();
        }
    }
}