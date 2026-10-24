namespace DoorRestartSystem.Shared.Runtime
{
    using LabApi.Extensions;
    using LabApi.Features.Wrappers;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Read-only snapshot storing pre-filtered structural room assets to eliminate hot-path LINQ allocations.
    /// Preserves illumination snapshots to ensure decoupled multi-system compatibility.
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
        /// Gets or sets the immutable snapshot of the room's illumination and power state sampled prior to lockdown engagement.
        /// Used to restore preexisting atmospheric lighting profiles (e.g., Warhead alerts) while respecting active grid blackouts.
        /// </summary>
        public RoomLightSnapshot LightSnapshot { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoomLockdownContext"/> class.
        /// </summary>
        /// <param name="room">The target room instance undergoing lockdown pre-processing.</param>
        /// <param name="skipUnknownDoors">Enforces whether unregistered, dummy, or non-interactable door assets are bypassed.</param>
        /// <param name="skipCheckpointsGate">Enforces whether checkpoint gates are bypassed.</param>
        /// <param name="skipElevators">Enforces whether connected elevator systems are completely ignored.</param>
        public RoomLockdownContext(Room room, bool skipUnknownDoors, bool skipCheckpointsGate, bool skipElevators)
        {
            Room = room ?? throw new ArgumentNullException(nameof(room));
            RoomInstanceId = room.GameObject.GetInstanceID();

            var doorsList = new List<Door>();
            foreach (var door in room.Doors)
            {
                if (door == null || door.IsDestroyed || door.IsElevatorDoor())
                    continue;

                if (skipUnknownDoors && (door is DummyDoor || door is NonInteractableDoor || door.GetType() == typeof(Door)))
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