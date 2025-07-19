namespace DoorRestartSystem.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using DoorRestartSystem;
    using DoorRestartSystem.Utilities;
    using Exiled.API.Features;
    using Exiled.Loader;

    /// <summary>
    /// Utility class for interacting with the Exiled API within the DoorRestartSystem context.
    /// Provides access to plugin components, random number generation, CASSIE messaging, and logging tools.
    /// </summary>
    public static class Library_ExiledAPI
    {
        #region Plugin Accessors

        /// <summary>
        /// Gets the singleton instance of the DoorRestartSystem plugin.
        /// </summary>
        public static Plugin Plugin => Plugin.Singleton ?? throw new InvalidOperationException("Plugin singleton is not initialized.");

        /// <summary>
        /// Gets the methods instance for managing door lockdown logic.
        /// </summary>
        public static Methods Methods => Plugin.Methods ?? throw new InvalidOperationException("Methods instance is not initialized.");

        /// <summary>
        /// Gets the configuration settings for the DoorRestartSystem plugin.
        /// </summary>
        public static DoorRestartSystem.Config Config => Plugin.Config ?? throw new InvalidOperationException("Config instance is not initialized.");

        #endregion

        #region Game Object Accessors

        /// <summary>
        /// Gets the list of currently connected players.
        /// </summary>
        public static IReadOnlyCollection<Player> Players => Player.List;

        /// <summary>
        /// Gets the list of all rooms in the facility.
        /// </summary>
        public static IReadOnlyCollection<Room> Rooms => Room.List;

        /// <summary>
        /// Gets the list of all Tesla gates in the facility.
        /// </summary>
        public static IReadOnlyCollection<Exiled.API.Features.TeslaGate> TeslaGates => Exiled.API.Features.TeslaGate.List;

        /// <summary>
        /// Gets the room at the specified position.
        /// </summary>
        /// <param name="position">The world position to check.</param>
        /// <returns>The room at the specified position, or null if none exists.</returns>
        public static Room GetRoomAtPosition(Vector3 position)
        {
            return Room.Get(position) ?? throw new ArgumentException($"No room found at position {position}.");
        }

        #endregion

        #region Random Number Generation

        /// <summary>
        /// Returns a random integer in the range [0, range).
        /// </summary>
        /// <param name="range">The upper bound of the random range (exclusive).</param>
        /// <returns>A random integer.</returns>
        public static int Loader_Random_Next(int range = 100)
        {
            if (range < 0)
                throw new ArgumentOutOfRangeException(nameof(range), "Range must be non-negative.");
            return Loader.Random.Next(range);
        }

        /// <summary>
        /// Returns a random integer in the range [minValue, maxValue).
        /// </summary>
        /// <param name="minValue">The lower bound of the random range (inclusive).</param>
        /// <param name="maxValue">The upper bound of the random range (exclusive).</param>
        /// <returns>A random integer.</returns>
        public static int Loader_Random_Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentException("minValue must not be greater than maxValue.");
            return Loader.Random.Next(minValue, maxValue);
        }

        /// <summary>
        /// Returns a random double between 0.0 and 1.0.
        /// </summary>
        /// <returns>A random double.</returns>
        public static double Loader_Random_NextDouble()
        {
            return Loader.Random.NextDouble();
        }

        #endregion

        #region CASSIE Messaging

        /// <summary>
        /// Clears all currently queued CASSIE messages.
        /// </summary>
        public static void Cassie_Clear()
        {
            try
            {
                Cassie.Clear();
                LogDebug("Cassie.Clear", "CASSIE message queue cleared.");
            }
            catch (Exception ex)
            {
                LogError("Cassie.Clear", $"Failed to clear CASSIE queue: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a glitched CASSIE message with specified glitch and jam chances.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="glitchChance">The chance of glitching per word (0.0 to 1.0).</param>
        /// <param name="jamChance">The chance of jamming per word (0.0 to 1.0).</param>
        public static void Cassie_GlitchyMessage(string message, float glitchChance, float jamChance)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                LogWarn("Cassie.GlitchyMessage", "Attempted to send empty CASSIE message.");
                return;
            }

            try
            {
                Cassie.GlitchyMessage(message, glitchChance, jamChance);
                LogDebug("Cassie.GlitchyMessage", $"Sent glitched CASSIE message: {message}");
            }
            catch (Exception ex)
            {
                LogError("Cassie.GlitchyMessage", $"Failed to send glitched CASSIE message: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a clean CASSIE message with no noise or subtitles.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public static void Cassie_Message(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                LogWarn("Cassie.Message", "Attempted to send empty CASSIE message.");
                return;
            }

            try
            {
                Cassie.Message(message, isNoisy: false, isSubtitles: false, isHeld: false);
                LogDebug("Cassie.Message", $"Sent clean CASSIE message: {message}");
            }
            catch (Exception ex)
            {
                LogError("Cassie.Message", $"Failed to send clean CASSIE message: {ex.Message}");
            }
        }

        #endregion

        #region Room Utilities

        // Note: The following generator-related methods are commented out as they may be specific to SCP-575.
        // Uncomment or remove based on whether DoorRestartSystem uses generators.

        /*
        /// <summary>
        /// Determines if the specified room contains no engaged generators.
        /// </summary>
        /// <param name="room">The room to check.</param>
        /// <returns>True if no generators in the room are engaged; otherwise, false.</returns>
        public static bool IsRoomFreeOfEngagedGenerators(Room room)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room), "Room cannot be null.");
            return !Generator.List.Any(gen => gen.Room == room && gen.IsEngaged);
        }

        /// <summary>
        /// Determines if the specified room and its neighboring rooms contain no engaged generators.
        /// </summary>
        /// <param name="room">The room to check.</param>
        /// <returns>True if no generators in the room or its neighbors are engaged; otherwise, false.</returns>
        public static bool IsRoomAndNeighborsFreeOfEngagedGenerators(Room room)
        {
            if (room == null)
                throw new ArgumentNullException(nameof(room), "Room cannot be null.");
            return !Generator.List.Any(gen =>
                gen.IsEngaged &&
                (gen.Room == room || room.NearestRooms.Contains(gen.Room)));
        }
        */

        /// <summary>
        /// Determines if a player is in a room with lights turned off.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>True if the player is in a dark room; otherwise, false.</returns>
        public static bool IsInDarkRoom(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player), "Player cannot be null.");
            return player.CurrentRoom?.AreLightsOff ?? false;
        }

        #endregion

        #region Logging

        /// <summary>
        /// Logs a debug message if debugging is enabled in the config.
        /// </summary>
        /// <param name="moduleId">The module identifier.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="isDebugEnabled">Whether debug logging is enabled.</param>
        public static void LogDebug(string moduleId, string message, bool isDebugEnabled = true)
        {
            if (isDebugEnabled)
                Log.Debug($"[{moduleId}] {message}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="moduleId">The module identifier.</param>
        /// <param name="message">The message to log.</param>
        public static void LogWarn(string moduleId, string message)
        {
            Log.Warn($"[{moduleId}] {message}");
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="moduleId">The module identifier.</param>
        /// <param name="message">The message to log.</param>
        public static void LogInfo(string moduleId, string message)
        {
            Log.Info($"[{moduleId}] {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="moduleId">The module identifier.</param>
        /// <param name="message">The message to log.</param>
        public static void LogError(string moduleId, string message)
        {
            Log.Error($"[{moduleId}] {message}");
        }

        #endregion

        #region LabAPI Adapters

        /// <summary>
        /// Converts a LabAPI player to an Exiled player.
        /// </summary>
        /// <param name="labApiPlayer">The LabAPI player to convert.</param>
        /// <returns>The corresponding Exiled player, or null if conversion fails.</returns>
        public static Player? ToExiledPlayer(LabApi.Features.Wrappers.Player? labApiPlayer)
        {
            if (labApiPlayer?.ReferenceHub == null)
            {
                LogWarn("ToExiledPlayer", "LabAPI player or ReferenceHub is null.");
                return null;
            }
            return Player.Get(labApiPlayer.ReferenceHub);
        }

        /// <summary>
        /// Converts a LabAPI ragdoll to an Exiled ragdoll.
        /// </summary>
        /// <param name="labApiRagdoll">The LabAPI ragdoll to convert.</param>
        /// <returns>The corresponding Exiled ragdoll, or null if conversion fails.</returns>
        public static Ragdoll? ToExiledRagdoll(LabApi.Features.Wrappers.Ragdoll? labApiRagdoll)
        {
            if (labApiRagdoll?.Base == null)
            {
                LogWarn("ToExiledRagdoll", "LabAPI ragdoll or Base is null.");
                return null;
            }
            return Ragdoll.Get(labApiRagdoll.Base);
        }

        /// <summary>
        /// Converts a LabAPI room to an Exiled room by matching world position.
        /// </summary>
        /// <param name="labApiRoom">The LabAPI room to convert.</param>
        /// <returns>The corresponding Exiled room, or null if no match is found.</returns>
        public static Room? ToExiledRoom(LabApi.Features.Wrappers.Room? labApiRoom)
        {
            if (labApiRoom == null)
            {
                LogWarn("ToExiledRoom", "LabAPI room is null.");
                return null;
            }

            Room? room = Room.List.FirstOrDefault(r => Helpers.Distance(r.Position, labApiRoom.Position) < 0.5f);
            if (room == null)
            {
                LogWarn("ToExiledRoom", $"No Exiled room found near position {labApiRoom.Position}.");
            }
            return room;
        }

        #endregion
    }
}