namespace DoorRestartSystem.Shared.Audio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;
    using MEC;
    using LabApi.Features.Wrappers;
    using AudioManagerAPI.Defaults;
    using AudioManagerAPI.Features.Enums;
    using AudioManagerAPI.Features.Management;
    using DoorRestartSystem.Shared.Audio.Enums;

    /// <summary>
    /// Central manager for the DoorRestartSystem audio pipelines.
    /// Controls resource streams, spatialized speaker lifecycles, and structural multi-channel playback.
    /// </summary>
    public class DrsAudioManager
    {
        private readonly Plugin _plugin;
        private readonly IAudioManager _audioEngine;
        private readonly HashSet<int> _activeSessionIds;

        private readonly Dictionary<DrsAudioKey, AudioTrackProfile> _audioRegistry = new()
        {
            { DrsAudioKey.LockdownSirenLoop, new("drs.lockdown_siren", 0.75f, 6f, 35f, true, AudioPriority.Max, 0f) },
            { DrsAudioKey.MechanicalLockSlam, new("drs.mechanical_lock_slam", 0.95f, 8f, 45f, true, AudioPriority.High, 2.5f) },
            { DrsAudioKey.ElectricalBuzzLoop, new("drs.electrical_buzz_loop", 0.65f, 5f, 30f, true, AudioPriority.Medium, 0f) },
            { DrsAudioKey.LockdownReleaseGlobal, new("drs.lockdown_release_global", 0.80f, 0f, 999.99f, false, AudioPriority.High, 6f) }
        };

        public DrsAudioManager(Plugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin infrastructure context cannot be null.");
            _audioEngine = DefaultAudioManager.Instance;
            _activeSessionIds = new HashSet<int>();

            RegisterEmbeddedAudioResources();
        }

        /// <summary>
        /// Emits a non-spatialized global soundscape layer directly into every active client's headspace.
        /// </summary>
        public int PlayGlobal(DrsAudioKey key, bool loop = false, float? customLifespan = null)
        {
            if (!_audioRegistry.TryGetValue(key, out var profile)) return 0;

            int sessionId = _audioEngine.PlayGlobalAudio(
                profile.Key, loop, profile.Volume, profile.Priority,
                validPlayersFilter: null, queue: false, fadeInDuration: 0.5f, lifespan: customLifespan ?? profile.DefaultLifespan, autoCleanup: true);

            if (sessionId != 0) _activeSessionIds.Add(sessionId);
            return sessionId;
        }

        /// <summary>
        /// Materializes a static 3D spatialized speaker entity bound to specific room coordinates.
        /// </summary>
        public int PlayAtPosition(DrsAudioKey key, Vector3 position, bool loop = false, float? customLifespan = null)
        {
            if (!_audioRegistry.TryGetValue(key, out var profile)) return 0;

            // Enforce linear distance check fallbacks to restrict audio network serialization data overhead
            Func<Player, bool> proximityFilter = p => p != null && p.IsReady && !p.IsHost
                && Vector3.Distance(p.Position, position) <= profile.MaxDistance;

            int sessionId = _audioEngine.PlayAudio(
                profile.Key, position, loop, profile.Volume,
                profile.MinDistance, profile.MaxDistance, profile.IsSpatial, profile.Priority,
                validPlayersFilter: proximityFilter, queue: false, fadeInDuration: 0f, lifespan: customLifespan ?? profile.DefaultLifespan, autoCleanup: true);

            if (sessionId != 0) _activeSessionIds.Add(sessionId);
            return sessionId;
        }

        /// <summary>
        /// Stops a distinct running audio streaming session smoothly utilizing cross-fade dampening.
        /// </summary>
        public void StopSession(int sessionId)
        {
            if (sessionId == 0 || !_activeSessionIds.Contains(sessionId)) return;

            try
            {
                _audioEngine.FadeOutAudio(sessionId, 0.5f);
            }
            catch (Exception ex)
            {
                Library_LabAPI.LogInfo("DrsAudioManager.StopSession", $"Suppressed voice engine cleanup artifact for session {sessionId}: {ex.Message}");
            }
            finally
            {
                _activeSessionIds.Remove(sessionId);
            }
        }

        /// <summary>
        /// Hard-clears all active trackers, killing running speakers to release engine resources.
        /// </summary>
        public void Clean()
        {
            foreach (int sessionId in _activeSessionIds.ToList())
            {
                if (sessionId == 0) continue;
                try
                {
                    _audioEngine.FadeOutAudio(sessionId, 0.2f);
                }
                catch { /* Safeguard against assembly detach anomalies */ }
            }

            _activeSessionIds.Clear();
        }

        private void RegisterEmbeddedAudioResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();

            foreach (var pair in _audioRegistry)
            {
                string targetKey = pair.Value.Key;
                string match = resourceNames.FirstOrDefault(r =>
                    r.EndsWith($"{targetKey}.wav", StringComparison.OrdinalIgnoreCase) ||
                    r.EndsWith($"{targetKey.Replace(".", "_")}.wav", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(match)) continue;

                _audioEngine.RegisterAudio(targetKey, () => assembly.GetManifestResourceStream(match));
            }
        }

        private sealed class AudioTrackProfile
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
                Key = key; Volume = volume; MinDistance = minDistance; MaxDistance = maxDistance; IsSpatial = isSpatial; Priority = priority; DefaultLifespan = defaultLifespan;
            }
        }
    }
}