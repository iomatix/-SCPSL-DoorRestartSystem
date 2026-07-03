namespace DoorRestartSystem.Shared
{
    using Cassie;
    using LabApi.Features.Console;
    using LabApi.Features.Wrappers;
    using MapGeneration;
    using System;

    /// <summary>
    /// Centralized utility abstraction layer acting as a bridge between the native LabAPI framework 
    /// and the DoorRestartSystem operational context.
    /// </summary>
    public static class Library_LabAPI
    {
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

        [ThreadStatic]
        private static Random _localRandom;
        private static Random ThreadRandom => _localRandom ??= new(Guid.NewGuid().GetHashCode());

        /// <summary>
        /// Generates a pseudo-random integer utilizing a thread-safe static sub-instance compatible with .NET 4.8.
        /// </summary>
        public static int Loader_Random_Next(int min, int max) => ThreadRandom.Next(min, max);

        /// <summary>
        /// Generates a pseudo-random double floating-point token utilizing a thread-safe static sub-instance compatible with .NET 4.8.
        /// </summary>
        public static double Loader_Random_NextDouble() => ThreadRandom.NextDouble();
        #endregion

        #region RoomName Enum Extension Gauges (C# 9.0 Logical Pattern Matching Optimization)
        /// <summary>
        /// Validates whether the designated room layout belongs to a tactical zone checkpoint node.
        /// </summary>
        public static bool IsCheckpoint(this RoomName roomName) =>
            roomName is RoomName.LczCheckpointA
                      or RoomName.LczCheckpointB
                      or RoomName.HczCheckpointA
                      or RoomName.HczCheckpointB
                      or RoomName.HczCheckpointToEntranceZone;

        /// <summary>
        /// Evaluates if the running room structural blueprint is classified as an anomalous entity containment sector.
        /// </summary>
        public static bool IsScpRoom(this RoomName roomName) =>
            roomName is RoomName.Lcz173
                      or RoomName.Lcz330
                      or RoomName.Hcz049
                      or RoomName.Hcz079
                      or RoomName.Hcz096
                      or RoomName.Hcz106
                      or RoomName.Hcz939;

        /// <summary>
        /// Verifies if the targeted room contains high-value equipment or tactical ammunition reserves.
        /// </summary>
        public static bool IsArmory(this RoomName roomName) =>
            roomName is RoomName.LczArmory or RoomName.HczArmory;
        #endregion

        #region Unified Diagnostic Logging Infrastructure
        /// <summary>
        /// Logs a debug diagnostic string directly reading the boolean parameters from the core configuration assembly.
        /// </summary>
        public static void LogDebug(string moduleId, string message)
        {
            if (Config.Debug)
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
}