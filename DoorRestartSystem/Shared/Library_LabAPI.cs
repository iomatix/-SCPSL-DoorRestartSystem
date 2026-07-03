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
        /// <returns>The duration of the message playback in seconds.</returns>
        public static double Cassie_GlitchyMessage(string message, float glitchChance, float jamChance)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                LogWarn("Cassie.GlitchyMessage", "Attempted to send empty CASSIE message.");
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
                LogError("Cassie.GlitchyMessage", $"Failed to execute dynamic execution timeline mapping: {ex.Message}");
                return 0.0;
            }
        }

        /// <summary>
        /// Sends a clean CASSIE message with no noise or subtitles.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>The duration of the message playback in seconds.</returns>
        public static double Cassie_Message(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                LogWarn("Cassie.Message", "Attempted to send empty CASSIE message.");
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
                LogError("Cassie.Message", $"Failed to send clean CASSIE message: {ex.Message}");
                return 0.0;
            }
        }

        #endregion
    }

}
