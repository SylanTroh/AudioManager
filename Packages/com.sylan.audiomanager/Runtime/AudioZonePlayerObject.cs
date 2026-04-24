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

        private AudioZoneSyncCore audioZonePlayerObjectSync;

        private const float IntervalInSeconds = .2f;
        private int audioZoneColliderLayerMask;
        private VRCPlayerApi localPlayer;
        private int hitCount = 0;

        private readonly DataDictionary audioSettingColliderCache = new DataDictionary();
        private readonly DataDictionary audioZoneColliderCache = new DataDictionary();
        private readonly Collider[] hits = new Collider[25];
#if AUDIO_MANAGER_DEBUG
        private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
#endif

        private void Start()
        {
            if (AudioZoneManager == null)
            {
                Debug.Log($"{nameof(AudioZonePlayerObject)} has no {nameof(AudioZoneManager)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            audioZonePlayerObjectSync = GetComponentInChildren<AudioZoneSyncCore>();
            if (audioZonePlayerObjectSync == null)
            {
                Debug.Log($"{nameof(AudioZonePlayerObject)} has no {nameof(audioZonePlayerObjectSync)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            if (!Networking.IsOwner(gameObject)) return;

            localPlayer = Networking.LocalPlayer;
            audioZoneColliderLayerMask = LayerMask.GetMask("AudioZones");
            SendCustomEventDelayedSeconds(nameof(ValidateAudioZones), 1, EventTiming.LateUpdate);
        }

        public void ValidateAudioZones()
        {
#if AUDIO_MANAGER_DEBUG
            stopwatch.Restart();
#endif
            audioZonePlayerObjectSync.OnValidateAudioZonesStart();
            if (TestForChangedAudioZone())
            {
#if AUDIO_MANAGER_DEBUG
                Debug.Log("Zone Changed");
#endif
                audioZonePlayerObjectSync.OnZoneChanged();
            }

#if AUDIO_MANAGER_DEBUG
            stopwatch.Stop();
            Debug.Log($"finished after {stopwatch.Elapsed.TotalMilliseconds}ms with {hitCount} hits");
#endif
            SendCustomEventDelayedSeconds(nameof(ValidateAudioZones), IntervalInSeconds, EventTiming.LateUpdate);
        }

        private bool TestForChangedAudioZone()
        {
            var trackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.SetPositionAndRotation(trackingData.position, trackingData.rotation);

            var size = Physics.OverlapSphereNonAlloc(transform.position, .01f, hits, audioZoneColliderLayerMask, QueryTriggerInteraction.Collide);
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