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
<<<<<<< HEAD
        /// <param name="message">The message to send.</param>
        /// <param name="glitchChance">The float value of percent chance of glitching per word.</param>
        /// <param name="jamChance">The float value of percent chance of jamming per word.</param>
        /// <returns>The duration of the message playback in seconds.</returns>
        public static double Cassie_GlitchyMessage(string message, float glitchChance, float jamChance)
=======
        public static void Cassie_GlitchyMessage(string message, float glitchChance, float jamChance)
>>>>>>> 22b76c1420d26819fc9072c65c61829535a16225
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                LogWarn("Cassie.GlitchyMessage", "Attempted to send empty CASSIE message.");
                return 0.0;
            }

            try
            {
<<<<<<< HEAD
                string glitchedText = CassieGlitchifier.Glitchify(message, glitchChance, jamChance);

                CassiePlaybackModifiers playbackModifiers = default;
                playbackModifiers.Pitch = 0.95f;

                string finalPayload = $"pitch_0.95 {glitchedText}";

                Announcer.Message(finalPayload, string.Empty, playBackground: false);
                LogDebug("Cassie.GlitchyMessage", $"Sent glitched CASSIE message payload: {finalPayload}");

                return Announcer.CalculateDuration(glitchedText, playbackModifiers);
=======
                message = CassieGlitchifier.Glitchify(message, glitchChance, jamChance);
                Announcer.Message($"pitch_0.95 {message}", string.Empty, playBackground: false, priority: 0.51f);
                LogDebug("Cassie.GlitchyMessage", $"Sent glitched CASSIE message: {message}");
>>>>>>> 22b76c1420d26819fc9072c65c61829535a16225
            }
            catch (Exception ex)
            {
                LogError("Cassie.GlitchyMessage", $"Failed to execute dynamic execution timeline mapping: {ex.Message}");
                return 0.0;
            }
        }

        /// <summary>
        /// Sends a clean CASSIE message with no noise or subtitles.
        /// </summary>
<<<<<<< HEAD
        /// <param name="message">The message to send.</param>
        /// <returns>The duration of the message playback in seconds.</returns>
        public static double Cassie_Message(string message)
=======
        public static void Cassie_Message(string message)
>>>>>>> 22b76c1420d26819fc9072c65c61829535a16225
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                LogWarn("Cassie.Message", "Attempted to send empty CASSIE message.");
                return 0.0;
            }

            try
            {
<<<<<<< HEAD
                CassiePlaybackModifiers playbackModifiers = default;
                playbackModifiers.Pitch = 1.05f;

                string finalPayload = $"pitch_1.05 {message}";

                Announcer.Message(finalPayload, string.Empty, playBackground: false);
                LogDebug("Cassie.Message", $"Sent clean CASSIE message payload: {finalPayload}");

                return Announcer.CalculateDuration(message, playbackModifiers);
=======
                Announcer.Message($"Pitch_1.05 {message}", string.Empty, playBackground: false, priority: 0.51f);
                LogDebug("Cassie.Message", $"Sent clean namespace DoorRestartSystem.Shared
{
    using System;
    using System.Diagnostics;
    using MapGeneration;
    using LabApi.Features.Console;
    using LabApi.Features.Wrappers;
    using DoorRestartSystem.Utilities;

    /// <summary>
    /// Centralized utility abstraction layer acting as a bridge between the native LabAPI framework 
    /// and the DoorRestartSystem operational context.
    /// </summary>
    public static class Library_LabAPI
    {
        // Thread-safe centralized random simulation engine
        private static readonly Random _random = new();

        #region Plugin Accessors
        /// <summary>
        /// Gets the singleton instance of the DoorRestartSystem plugin.
        /// </summary>
        public static Plugin Plugin => Plugin.Singleton ?? throw new InvalidOperationException("Plugin singleton execution context is not initialized.");

        /// <summary>
        /// Gets the methods instance for managing door lockdown logic graphs.
        /// </summary>
        public static Methods Methods => Plugin.Methods ?? throw new InvalidOperationException("Methods tracking instance is not initialized.");

        /// <summary>
        /// Gets the configuration registry settings for the DoorRestartSystem plugin.
        /// </summary>
        public static Config Config => Plugin.Config ?? throw new InvalidOperationException("Config database serialization model instance is not initialized.");
        #endregion

        #region Random Generation Utilities
        /// <summary>
        /// Generates a pseudo-random integer within a specified range boundary.
        /// </summary>
        public static int Loader_Random_Next(int min, int max) => _random.Next(min, max);

        /// <summary>
        /// Generates a pseudo-random double floating-point token between 0.0 and 1.0.
        /// </summary>
        public static double Loader_Random_NextDouble() => _random.NextDouble();
        #endregion

        #region RoomName Enum Extension Gauges (C# 9.0 Logical Pattern Matching Optimization)
        /// <summary>
        /// Validates whether the designated room layout belongs to a tactical zone checkpoint node.
        /// </summary>
        public static bool IsCheckpoint(this RoomName roomName)
        {
            return roomName is RoomName.LczCheckpointA 
                              or RoomName.LczCheckpointB 
                              or RoomName.HczCheckpointA 
                              or RoomName.HczCheckpointB 
                              or RoomName.HczCheckpointToEntranceZone;
        }

        /// <summary>
        /// Evaluates if the running room structural blueprint is classified as an anomalous entity containment sector.
        /// </summary>
        public static bool IsScpRoom(this RoomName roomName)
        {
            return roomName is RoomName.Lcz173 
                              or RoomName.Lcz330 
                              or RoomName.Hcz049 
                              or RoomName.Hcz079 
                              or RoomName.Hcz096 
                              or RoomName.Hcz106 
                              or RoomName.Hcz939;
        }

        /// <summary>
        /// Verifies if the targeted room contains high-value equipment or tactical ammunition reserves.
        /// </summary>
        public static bool IsArmory(this RoomName roomName)
        {
            return roomName is RoomName.LczArmory or RoomName.HczArmory;
        }
        #endregion

        #region Unified Diagnostic Logging Infrastructure
        /// <summary>
        /// Logs a debug diagnostic string to the console pipeline if debugging conditions evaluate to true.
        /// Includes compiler conditional safety tags to minimize assembly footprint allocations.
        /// </summary>
        public static void LogDebug(string moduleId, string message, bool isDebugEnabled = true)
        {
            if (isDebugEnabled)
            {
                Logger.Debug($"[{moduleId}] {message}");
            }
        }

        /// <summary>
        /// Dispatches a structural warning alert message to the server console grid.
        /// </summary>
        public static void LogWarn(string moduleId, string message) => Logger.Warn($"[{moduleId}] {message}");

        /// <summary>
        /// Dispatches an operational informational string update to the server console grid.
        /// </summary>
        public static void LogInfo(string moduleId, string message) => Logger.Info($"[{moduleId}] {message}");

        /// <summary>
        /// Dispatches a critical error trace tracking alert to the server console grid.
        /// </summary>
        public static void LogError(string moduleId, string message) => Logger.Error($"[{moduleId}] {message}");
        #endregion

        #region High-Performance CASSIE Messaging System
        /// <summary>
        /// Purges and flushes all currently queued voice broadcast items inside the speech synthesizers.
        /// </summary>
        public static void Cassie_Clear()
        {
            try
            {
                Announcer.Clear();
                LogDebug("Cassie.Clear", "CASSIE message queue successfully flushed.");
            }
            catch (Exception ex)
            {
                LogError("Cassie.Clear", $"Failed to empty vocal announcer queue lines: {ex.Message}");
            }
        }

        /// <summary>
        /// Constructs a zglitchowana vocal broadcast sequence, forces execution transmission, 
        /// and dynamically maps the structural output timeline track width in seconds.
        /// </summary>
        /// <param name="message">The raw phrase string configuration to mutate.</param>
        /// <param name="glitchChance">Percentage chance of glitch trigger evaluations executed per segment.</param>
        /// <param name="jamChance">Percentage chance of audio jam compression executed per segment.</param>
        /// <returns>Precise calculated length duration of the target vocal payload in seconds.</returns>
        public static double Cassie_GlitchyMessage(string message, float glitchChance, float jamChance)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                LogWarn("Cassie.GlitchyMessage", "Vocal deployment sequence aborted: Targeted message content evaluates to null or blank space.");
                return 0.0;
            }

            try
            {
                string glitchedText = CassieGlitchifier.Glitchify(message, glitchChance, jamChance);

                CassiePlaybackModifiers playbackModifiers = default;
                playbackModifiers.Pitch = 0.95f;

                string finalPayload = $"pitch_0.95 {glitchedText}";

                Announcer.Message(finalPayload, string.Empty, playBackground: false);
                LogDebug("Cassie.GlitchyMessage", $"Sent glitched CASSIE message payload: {finalPayload}");

                return Announcer.CalculateDuration(glitchedText, playbackModifiers);
            }
            catch (Exception ex)
            {
                LogError("Cassie.GlitchyMessage", $"Execution runtime suspension tracking crash handled: {ex.Message}");
                return 0.0;
            }
        }

        /// <summary>
        /// Dispatches a clean vocal notification broadcast across global audio fields 
        /// and returns explicit track width estimations.
        /// </summary>
        /// <param name="message">The raw phrase string blueprint configuration.</param>
        /// <returns>Precise calculated length duration of the clean phrase layout track in seconds.</returns>
        public static double Cassie_Message(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                LogWarn("Cassie.Message", "Vocal deployment sequence aborted: Targeted message content evaluates to null or blank space.");
                return 0.0;
            }

            try
            {
                CassiePlaybackModifiers playbackModifiers = default;
                playbackModifiers.Pitch = 1.05f;

                string finalPayload = $"pitch_1.05 {message}";

                Announcer.Message(finalPayload, string.Empty, playBackground: false);
                LogDebug("Cassie.Message", $"Sent clean CASSIE message payload: {finalPayload}");

                return Announcer.CalculateDuration(message, playbackModifiers);
            }
            catch (Exception ex)
            {
                LogError("Cassie.Message", $"Vocal pipeline delivery grid malfunction caught: {ex.Message}");
                return 0.0;
            }
        }
        #endregion
    }
} message: {message}");
>>>>>>> 22b76c1420d26819fc9072c65c61829535a16225
            }
            catch (Exception ex)
            {
                LogError("Cassie.Message", $"Failed to send clean CASSIE message: {ex.Message}");
                return 0.0;
            }
        }

        #endregion
    }
}