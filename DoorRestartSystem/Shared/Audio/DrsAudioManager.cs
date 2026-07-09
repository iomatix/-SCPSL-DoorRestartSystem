namespace DoorRestartSystem.Shared.Audio
{
    using AudioManagerAPI.Defaults;
    using AudioManagerAPI.Features.Enums;
    using AudioManagerAPI.Features.Management;
    using DoorRestartSystem.Shared.Audio.Enums;
    using LabApi.Extensions;
    using LabApi.Features.Wrappers;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using Logger = LabApi.Extensions.Misc.iLogger;

    /// <summary>
    /// Central manager for the DoorRestartSystem audio pipelines.
    /// Controls resource streams, spatialized speaker lifecycles, and structural multi-channel playback.
    /// </summary>
    public class DrsAudioManager
    {
        #region Private Fields & Registries
        private readonly Plugin _plugin;
        private readonly IAudioManager _audioEngine;
        private readonly HashSet<int> _activeSessionIds;

        private readonly Dictionary<DrsAudioKey, AudioTrackProfile> _audioRegistry = new Dictionary<DrsAudioKey, AudioTrackProfile>()
        {
            { DrsAudioKey.LockdownSirenLoop, new AudioTrackProfile("drs.lockdown_siren", 0.75f, 6f, 35f, true, AudioPriority.Max, 0f) },
            { DrsAudioKey.MechanicalLockSlam, new AudioTrackProfile("drs.mechanical_lock_slam", 0.95f, 8f, 45f, true, AudioPriority.Medium, 2.75f) },
            { DrsAudioKey.ElectricalBuzzLoop, new AudioTrackProfile("drs.electrical_buzz_loop", 0.65f, 5f, 30f, true, AudioPriority.Low, 0f) },
            { DrsAudioKey.LockdownReleaseGlobal, new AudioTrackProfile("drs.lockdown_release_global", 0.80f, 0f, 999.99f, false, AudioPriority.High, 6f) }
        };
        #endregion

        #region Reusable Allocation-Free Filter State
        /// <summary>
        /// Optimized state holder utilizing a cached static delegate to avoid closure allocations on the heap.
        /// </summary>
        private static class ProximityFilterContext
        {
            [ThreadStatic]
            public static Vector3 TargetPosition;

            [ThreadStatic]
            public static float MaxAllowedDistance;

            /// <summary>
            /// Cached static delegate instance ensuring zero runtime allocation when passed into the audio engine.
            /// </summary>
            public static readonly Func<Player, bool> CachedFilterDelegate = ValidatePlayerProximity;

            private static bool ValidatePlayerProximity(Player player)
            {
                return player != null
                    && player.IsReady
                    && !player.IsHost
                    && player.IsWithinRadius(TargetPosition, MaxAllowedDistance);
            }
        }
        #endregion

        #region Initialization
        public DrsAudioManager(Plugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin infrastructure context cannot be null.");
            _audioEngine = DefaultAudioManager.Instance;
            _activeSessionIds = new HashSet<int>();

            RegisterEmbeddedAudioResources();
        }
        #endregion

        #region Public Playback Controls
        /// <summary>
        /// Fires a non-spatialized voice broadcast channel tracking globally across the server environment.
        /// </summary>
        public int PlayGlobal(DrsAudioKey key, bool loop = false, float? customLifespan = null)
        {
            if (!_audioRegistry.TryGetValue(key, out var profile)) return 0;

            int sessionId = _audioEngine.PlayGlobalAudio(
                profile.Key,
                loop,
                profile.Volume,
                profile.Priority,
                validPlayersFilter: null,
                queue: false,
                fadeInDuration: 0.5f,
                lifespan: customLifespan ?? profile.DefaultLifespan,
                autoCleanup: true);

            if (sessionId != 0)
                _activeSessionIds.Add(sessionId);

            return sessionId;
        }

        /// <summary>
        /// Deploys a spatialized audio channel tied to precise world coordinates without generating closure heap allocations.
        /// </summary>
        public int PlayAtPosition(DrsAudioKey key, Vector3 position, bool loop = false, float? customLifespan = null)
        {
            if (!_audioRegistry.TryGetValue(key, out var profile)) return 0;

            // Allocation-Free Optimization: Store variables in ThreadStatic fields and pass the pre-allocated static delegate reference
            ProximityFilterContext.TargetPosition = position;
            ProximityFilterContext.MaxAllowedDistance = profile.MaxDistance;

            int sessionId = _audioEngine.PlayAudio(
                profile.Key,
                position,
                loop,
                profile.Volume,
                profile.MinDistance,
                profile.MaxDistance,
                profile.IsSpatial,
                profile.Priority,
                validPlayersFilter: ProximityFilterContext.CachedFilterDelegate,
                queue: false,
                fadeInDuration: 0f,
                lifespan: customLifespan ?? profile.DefaultLifespan,
                autoCleanup: true);

            if (sessionId != 0)
                _activeSessionIds.Add(sessionId);

            return sessionId;
        }

        /// <summary>
        /// Gracefully fades out an active voice stream and clears its structural state tracking.
        /// </summary>
        public void StopSession(int sessionId)
        {
            if (sessionId == 0) return;

            try
            {
                _audioEngine.FadeOutAudio(sessionId, 0.5f);
            }
            catch (Exception ex)
            {
                Logger.Debug(nameof(DrsAudioManager), $"Suppressed voice engine cleanup artifact for session {sessionId}: {ex.Message}", _plugin.Debug);
            }
            finally
            {
                _activeSessionIds.Remove(sessionId);
            }
        }

        /// <summary>
        /// Forcefully halts all active audio sessions using an allocation-free array copy operation.
        /// </summary>
        public void Clean()
        {
            if (_activeSessionIds.Count == 0) return;

            int[] sessionsBuffer = new int[_activeSessionIds.Count];
            _activeSessionIds.CopyTo(sessionsBuffer, 0);

            for (int i = 0; i < sessionsBuffer.Length; i++)
            {
                int sessionId = sessionsBuffer[i];
                if (sessionId == 0) continue;

                try
                {
                    _audioEngine.FadeOutAudio(sessionId, 0.2f);
                }
                catch (Exception ex)
                {
                    Logger.Debug(nameof(DrsAudioManager), $"Suppressed final audio track fade out anomaly on session {sessionId}: {ex.Message}", _plugin.Debug);
                }
            }

            _activeSessionIds.Clear();
        }
        #endregion

        #region Resource Registration Engines
        /// <summary>
        /// Discovers embedded assembly .wav tracks dynamically, verifying targets against fluent enum identifiers.
        /// </summary>
        private void RegisterEmbeddedAudioResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();

            foreach (var pair in _audioRegistry)
            {
                string targetKey = pair.Value.Key;
                string fluentEnumKey = pair.Key.ToAudioKey();

                string match = assembly.FindEmbeddedAsset(targetKey, ".wav", fluentEnumKey);

                if (string.IsNullOrEmpty(match))
                {
                    Logger.Warn(nameof(DrsAudioManager), $"Failed to bind audio asset mapping: No manifest resource matches identifier key '{targetKey}' or token '{fluentEnumKey}'.");
                    continue;
                }

                _audioEngine.RegisterAudio(targetKey, () => assembly.GetManifestResourceStream(match));
                Logger.Debug(nameof(DrsAudioManager), $"Successfully mapped and registered audio stream token: [{fluentEnumKey}] -> manifest resource target.", _plugin.Debug);
            }
        }
        #endregion
    }

    /// <summary>
    /// Profile container moved to namespace level to guarantee compiler visibility across assemblies.
    /// </summary>
    internal sealed class AudioTrackProfile
    {
        public string Key { get; }
        public float Volume { get; }
        public float MinDistance { get; }
        public float MaxDistance { get; }
        public bool IsSpatial { get; }
        public AudioPriority Priority { get; }
        public float DefaultLifespan { get; }

        public AudioTrackProfile(string key, float volume, float minDistance, float maxDistance, bool isSpatial, AudioPriority priority, float defaultLifespan)
        {
            Key = key;
            Volume = volume;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            IsSpatial = isSpatial;
            Priority = priority;
            DefaultLifespan = defaultLifespan;
        }
    }
}