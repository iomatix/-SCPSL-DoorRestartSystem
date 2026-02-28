namespace DoorRestartSystem.Shared
{
    using Cassie;
    using DoorRestartSystem;
    using DoorRestartSystem.Utilities;
    using LabApi.Features.Console;
    using LabApi.Features.Wrappers;
    using System;

    /// <summary>
    /// Utility class for interacting with the native LabAPI within the DoorRestartSystem context.
    /// Provides access to plugin components, random number generation, CASSIE messaging, and logging tools.
    /// </summary>
    public static class Library_LabAPI
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
                Logger.Debug($"[{moduleId}] {message}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="moduleId">The module identifier.</param>
        /// <param name="message">The message to log.</param>
        public static void LogWarn(string moduleId, string message)
        {
            Logger.Warn($"[{moduleId}] {message}");
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="moduleId">The module identifier.</param>
        /// <param name="message">The message to log.</param>
        public static void LogInfo(string moduleId, string message)
        {
            Logger.Info($"[{moduleId}] {message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="moduleId">The module identifier.</param>
        /// <param name="message">The message to log.</param>
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
        /// <param name="message">The message to send.</param>
        /// <param name="glitchChance">The float value of percent chance of glitching per word.</param>
        /// <param name="jamChance">The float value of percent chance of jamming per word.</param>
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
                Announcer.Message($"pitch_0.95 {message}", string.Empty, playBackground: false);
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
                Announcer.Message($"Pitch_1.05 {message}", string.Empty, playBackground: false);
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
