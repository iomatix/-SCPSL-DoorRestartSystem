namespace DoorRestartSystem.Shared
{
    using Cassie;
    using DoorRestartSystem;
    using DoorRestartSystem.Utilities;
    using LabApi.Features.Console;
    using LabApi.Features.Wrappers;
    using MapGeneration;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Utility class for interacting with the native LabAPI within the DoorRestartSystem context.
    /// Provides access to plugin components, random number generation, CASSIE messaging, and logging tools.
    /// </summary>
    public static class Library_LabAPI
    {
        // Thread-safe centralized random engine
        private static readonly Random _random = new();

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

        #region Random Generation Utilities

        /// <summary>
        /// Generates a random integer within a specified range.
        /// </summary>
        public static int Loader_Random_Next(int min, int max) => _random.Next(min, max);

        /// <summary>
        /// Generates a random double floating-point number between 0.0 and 100.0.
        /// </summary>
        public static double Loader_Random_NextDouble() => _random.NextDouble();

        #endregion

        #region RoomName Enum Extension Gauges

        /// <summary>
        /// Validates whether the designated room layout belongs to a tactical zone checkpoint checkpoint node.
        /// </summary>
        public static bool IsCheckpoint(this RoomName roomName)
        {
            return roomName == RoomName.LczCheckpointA ||
                   roomName == RoomName.LczCheckpointB ||
                   roomName == RoomName.HczCheckpointA ||
                   roomName == RoomName.HczCheckpointB ||
                   roomName == RoomName.HczCheckpointToEntranceZone;
        }

        /// <summary>
        /// Evaluates if the running room structural blueprint is classified as an anomalous entity containment containment sector.
        /// </summary>
        public static bool IsScpRoom(this RoomName roomName)
        {
            return roomName == RoomName.Lcz173 ||
                   roomName == RoomName.Lcz330 ||
                   roomName == RoomName.Hcz049 ||
                   roomName == RoomName.Hcz079 ||
                   roomName == RoomName.Hcz096 ||
                   roomName == RoomName.Hcz106 ||
                   roomName == RoomName.Hcz939;
        }

        /// <summary>
        /// Verifies if the targeted room contains high-value equipment or tactical ammunition reserves.
        /// </summary>
        public static bool IsArmory(this RoomName roomName)
        {
            return roomName == RoomName.LczArmory ||
                   roomName == RoomName.HczArmory;
        }

        #endregion

        #region Logging

        /// <summary>
        /// Logs a debug message to the console line strictly during DEBUG compilation builds.
        /// Fully stripped out by the compiler in RELEASE mode to guarantee zero runtime allocation overhead.
        /// </summary>
        [Conditional("DEBUG")]
        public static void LogDebug(string moduleId, string message)
        {
           Logger.Debug($"[{moduleId}] {message}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void LogWarn(string moduleId, string message)
        {
            Logger.Warn($"[{moduleId}] {message}");
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public static void LogInfo(string moduleId, string message)
        {
            Logger.Info($"[{moduleId}] {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(string moduleId, string message)
        {
            Logger.Error($"[{moduleId}] {message}");
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
                Announcer.Clear();
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
        public static void Cassie_GlitchyMessage(string message, float glitchChance, float jamChance)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                LogWarn("Cassie.GlitchyMessage", "Attempted to send empty CASSIE message.");
                return;
            }

            try
            {
                message = CassieGlitchifier.Glitchify(message, glitchChance, jamChance);
                Announcer.Message($"pitch_0.95 {message}", string.Empty, playBackground: false, priority: 0.51f);
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
        public static void Cassie_Message(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                LogWarn("Cassie.Message", "Attempted to send empty CASSIE message.");
                return;
            }

            try
            {
                Announcer.Message($"Pitch_1.05 {message}", string.Empty, playBackground: false, priority: 0.51f);
                LogDebug("Cassie.Message", $"Sent clean CASSIE message: {message}");
            }
            catch (Exception ex)
            {
                LogError("Cassie.Message", $"Failed to send clean CASSIE message: {ex.Message}");
            }
        }

        #endregion
    }
}