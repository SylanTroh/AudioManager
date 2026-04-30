using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

namespace Sylan.AudioManager
{
    [RequireComponent(typeof(VRCPlayerObject))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioZonePlayerObject : UdonSharpBehaviour
    {
        [HideInInspector, SerializeField] public AudioZoneManager AudioZoneManager;
        public const string AudioZoneManagerPropertyName = nameof(AudioZoneManager);
#if AUDIO_MANAGER_DEBUG_SW
        [HideInInspector, SerializeField, JanSharp.SingletonReference] private JanSharp.QuickDebugUI qd;
#endif

        private AudioZoneSyncCore audioZonePlayerObjectSync;

        private const float IntervalInSeconds = .2f;
        /// <summary>
        /// <para>Cannot use <see cref="LayerMask.GetMask(string[])"/> with <c>"AudioZones"</c> at runtime
        /// because that returns <c>0</c> in VRChat.</para>
        /// </summary>
        [HideInInspector, SerializeField] private LayerMask audioZoneColliderLayerMask;
        public const string AudioZoneColliderLayerMaskPropertyName = nameof(audioZoneColliderLayerMask);
        private VRCPlayerApi localPlayer;
        private int hitCount = 0;

        private readonly DataDictionary audioSettingColliderCache = new DataDictionary();
        private readonly DataDictionary audioZoneColliderCache = new DataDictionary();
        private readonly Collider[] hits = new Collider[25];
#if AUDIO_MANAGER_DEBUG_SW
        private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private object[] stopwatchData;
        private double lastPhysicsLoopTime = 0d;
        private int lastHitCount = 0;
#endif

        private void Start()
        {
            if (AudioZoneManager == null)
            {
                Debug.Log($"[AudioManager] {nameof(AudioZonePlayerObject)} has no {nameof(AudioZoneManager)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            audioZonePlayerObjectSync = GetComponentInChildren<AudioZoneSyncCore>();
            if (audioZonePlayerObjectSync == null)
            {
                Debug.Log($"[AudioManager] {nameof(AudioZonePlayerObject)} has no {nameof(audioZonePlayerObjectSync)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            if (!Networking.IsOwner(gameObject)) return;

#if AUDIO_MANAGER_DEBUG_SW
            Debug.Log($"[AudioManager] audioZoneColliderLayerMask: 0x{(int)audioZoneColliderLayerMask:x8}");
            stopwatchData = JanSharp.StopwatchUtil.CreateDataContainer();
            qd.Add(this, "Zones Update", nameof(UpdateStopwatchUI1));
            qd.Add(this, "Zones Last Update", nameof(UpdateStopwatchUI2));
            qd.Add(this, "Last Hit Count", nameof(UpdateLastHitCountUI));
#endif

            localPlayer = Networking.LocalPlayer;
            SendCustomEventDelayedSeconds(nameof(ValidateAudioZones), 1, EventTiming.LateUpdate);
        }

#if AUDIO_MANAGER_DEBUG_SW
        public void UpdateStopwatchUI1()
        {
            qd.DisplayValue = JanSharp.StopwatchUtil.FormatAvgMinMax(stopwatch, stopwatchData);
            stopwatch.Reset();
        }

        public void UpdateStopwatchUI2() => qd.DisplayValue = $"{lastPhysicsLoopTime:f3}ms";

        public void UpdateLastHitCountUI() => qd.DisplayValue = lastHitCount.ToString();
#endif

        public void ValidateAudioZones()
        {
#if AUDIO_MANAGER_DEBUG_SW
            stopwatch.Restart();
#endif
            audioZonePlayerObjectSync.OnValidateAudioZonesStart();
            if (TestForChangedAudioZone())
            {
#if AUDIO_MANAGER_DEBUG_SW
                Debug.Log("[AudioManager] Zone Changed");
#endif
                audioZonePlayerObjectSync.OnZoneChanged();
            }

#if AUDIO_MANAGER_DEBUG_SW
            stopwatch.Stop();
            lastPhysicsLoopTime = stopwatch.Elapsed.TotalMilliseconds;
            // Debug.Log($"[AudioManager] Finished after {stopwatch.Elapsed.TotalMilliseconds}ms with {hitCount} hits");
#endif
            SendCustomEventDelayedSeconds(nameof(ValidateAudioZones), IntervalInSeconds, EventTiming.LateUpdate);
        }

        private bool TestForChangedAudioZone()
        {
            var trackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.SetPositionAndRotation(trackingData.position, trackingData.rotation);

            var size = Physics.OverlapSphereNonAlloc(transform.position, .01f, hits, audioZoneColliderLayerMask, QueryTriggerInteraction.Collide);
#if AUDIO_MANAGER_DEBUG_SW
            lastHitCount = size;
#endif
            for (hitCount = 0; hitCount < size; hitCount++)
            {
                var hit = hits[hitCount];
                var audioZoneCollider = ComputeIfAbsent<AudioZoneCollider>(hit, audioZoneColliderCache);
                var audioSettingCollider = ComputeIfAbsent<AudioSettingCollider>(hit, audioSettingColliderCache);

                if (audioZoneCollider != null)
                {
                    audioZonePlayerObjectSync.NotifyHitAudioZoneCollider(audioZoneCollider);
                }

                if (audioSettingCollider != null)
                {
                    audioZonePlayerObjectSync.NotifyAudioSettingCollider(audioSettingCollider);
                }
            }

            return audioZonePlayerObjectSync.HasZoneChanged();
        }

        private static T ComputeIfAbsent<T>(Collider hit, DataDictionary dictionary) where T : UdonSharpBehaviour
        {
            if (dictionary.TryGetValue(hit.gameObject, out var token))
            {
                return (T)token.Reference;
            }

            var hitCollider = hit.GetComponent<T>();
            dictionary.Add(hit.gameObject, hitCollider);
            return hitCollider;
        }
    }
}