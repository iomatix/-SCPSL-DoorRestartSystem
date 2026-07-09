using AudioManagerAPI.Defaults;
using AudioManagerAPI.Features.Enums;
using AudioManagerAPI.Features.Management;
using DoorRestartSystem.Shared.Audio.Enums;
using LabApi.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Logger = LabApi.Extensions.Misc.iLogger;

namespace DoorRestartSystem.Shared.Audio
{
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

        private readonly Dictionary<DrsAudioKey, AudioTrackProfile> _audioRegistry = new()
        {
            { DrsAudioKey.LockdownSirenLoop, new("drs.lockdown_siren", 0.75f, 6f, 35f, true, AudioPriority.Max, 0f) },
            { DrsAudioKey.MechanicalLockSlam, new("drs.mechanical_lock_slam", 0.95f, 8f, 45f, true, AudioPriority.Medium, 2.75f) },
            { DrsAudioKey.ElectricalBuzzLoop, new("drs.electrical_buzz_loop", 0.65f, 5f, 30f, true, AudioPriority.Low, 0f) },
            { DrsAudioKey.LockdownReleaseGlobal, new("drs.lockdown_release_global", 0.80f, 0f, 999.99f, false, AudioPriority.High, 6f) }
        };
        #endregion

        #region Reusable High-Performance State Structures
        /// <summary>
        /// Read-only structure carrying context parameters to ensure zero-allocation player filtration.
        /// </summary>
        private readonly struct SpatialFilterState
        {
            public Vector3 Position { get; }
            public float MaxDistance { get; }

            public SpatialFilterState(Vector3 position, float maxDistance)
            {
                Position = position;
                MaxDistance = maxDistance;
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

            // Architectural Upgrade: Transitioned to the generic PlayGlobalAudio pipeline using a dummy state context
            int sessionId = _audioEngine.PlayGlobalAudio<object>(
                key: profile.Key,
                state: null,
                validPlayersFilter: (player, _) => player != null && player.IsReady,
                loop: loop,
                volume: profile.Volume,
                priority: profile.Priority,
                queue: false,
                fadeInDuration: 0.5f,
                persistent: false,
                lifespan: customLifespan ?? profile.DefaultLifespan,
                autoCleanup: true);

            if (sessionId != 0)
                _activeSessionIds.Add(sessionId);

            return sessionId;
        }

        /// <summary>
        /// Deploys a spatialized audio channel tied to precise world coordinates. 
        /// Leverages the zero-allocation state-passing API matrix to eliminate closure garbage.
        /// </summary>
        public int PlayAtPosition(DrsAudioKey key, Vector3 position, bool loop = false, float? customLifespan = null)
        {
            if (!_audioRegistry.TryGetValue(key, out var profile)) return 0;

            // Allocation Optimization: Instantiate the context parameters on the stack (zero heap footprint)
            SpatialFilterState stateContext = new SpatialFilterState(position, profile.MaxDistance);

            // Architectural Upgrade: Execute using the new generic pipeline from AudioManagerAPI 2.4.1
            int sessionId = _audioEngine.PlayAudio<SpatialFilterState>(
                key: profile.Key,
                position: position,
                state: stateContext,
                validPlayersFilter: (player, state) => player != null
                    && player.IsReady
                    && !player.IsHost
                    && player.IsWithinRadius(state.Position, state.MaxDistance),
                loop: loop,
                volume: profile.Volume,
                minDistance: profile.MinDistance,
                maxDistance: profile.MaxDistance,
                isSpatial: profile.IsSpatial,
                priority: profile.Priority,
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