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
        [HideInInspector, SerializeField] public AudioZoneManager audioZoneManager;
        public const string AudioZoneManagerPropertyName = nameof(audioZoneManager);

        private AudioZoneSyncCore audioZonePlayerObjectSync;

        private const float IntervalInSeconds = 0.2f;
        [HideInInspector, SerializeField] private LayerMask audioZoneColliderLayerMask;
        public const string AudioZoneColliderLayerMaskPropertyName = nameof(audioZoneColliderLayerMask);
        private VRCPlayerApi localPlayer;

        private readonly DataDictionary audioSettingColliderCache = new DataDictionary();
        private readonly DataDictionary audioZoneColliderCache = new DataDictionary();
        public const int MaxOverlappingZoneColliders = 25;
        private readonly Collider[] hits = new Collider[MaxOverlappingZoneColliders];
        private float headCheckRadius;

        private void Start()
        {
            if (audioZoneManager == null)
            {
                Debug.LogError($"[AudioManager] {nameof(AudioZonePlayerObject)} has no {nameof(audioZoneManager)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            audioZonePlayerObjectSync = GetComponentInChildren<AudioZoneSyncCore>();
            if (audioZonePlayerObjectSync == null)
            {
                Debug.LogError($"[AudioManager] {nameof(AudioZonePlayerObject)} has no {nameof(audioZonePlayerObjectSync)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            if (!Networking.IsOwner(gameObject)) return;

            Debug.Log($"[AudioManager] Using the {audioZonePlayerObjectSync.SyncScriptName} script for syncing audio and setting zones.");

            localPlayer = Networking.LocalPlayer;
            headCheckRadius = audioZoneManager.HeadCheckRadius;
            SendCustomEventDelayedSeconds(nameof(CheckForAudioZonesLoop), 1, EventTiming.LateUpdate);
        }

        public void CheckForAudioZonesLoop()
        {
            audioZonePlayerObjectSync.OnCheckForChangedAudioZones();
            if (CheckForChangedAudioZones())
            {
                audioZonePlayerObjectSync.OnZonesChanged();
            }
            SendCustomEventDelayedSeconds(nameof(CheckForAudioZonesLoop), IntervalInSeconds, EventTiming.LateUpdate);
        }

        private bool CheckForChangedAudioZones()
        {
            var trackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            int hitCount = Physics.OverlapSphereNonAlloc(trackingData.position, headCheckRadius, hits, audioZoneColliderLayerMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hitCount; i++)
            {
                GameObject hitGo = hits[i].gameObject;
                if (hitGo == null) continue; // Some VRChat internal object, would be impossible to be null in normal Unity.

                var audioZoneCollider = GetZoneCached<AudioZoneCollider>(hitGo, audioZoneColliderCache);
                if (audioZoneCollider != null)
                {
                    audioZonePlayerObjectSync.NotifyHitAudioZoneCollider(audioZoneCollider);
                }

                var audioSettingCollider = GetZoneCached<AudioSettingCollider>(hitGo, audioSettingColliderCache);
                if (audioSettingCollider != null)
                {
                    audioZonePlayerObjectSync.NotifyAudioSettingCollider(audioSettingCollider);
                }
            }

            return audioZonePlayerObjectSync.HaveZonesChanged();
        }

        private static T GetZoneCached<T>(GameObject go, DataDictionary cache)
            where T : UdonSharpBehaviour
        {
            if (cache.TryGetValue(go, out DataToken token))
            {
                return (T)token.Reference;
            }

            T zone = go.GetComponent<T>();
            cache.Add(go, zone); // Do save null too to avoid future GetComponent calls.
            return zone;
        }
    }
}
