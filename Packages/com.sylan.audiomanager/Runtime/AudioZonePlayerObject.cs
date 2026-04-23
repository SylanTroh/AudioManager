using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Enums;
using Debug = UnityEngine.Debug;


namespace Sylan.AudioManager
{
    [RequireComponent(typeof(VRCPlayerObject))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class AudioZonePlayerObject : UdonSharpBehaviour
    {
        [HideInInspector, SerializeField] public AudioZoneManager AudioZoneManager;
        public const string AudioZoneManagerPropertyName = nameof(AudioZoneManager);

        private AbstractAudioZonePlayerObjectSync audioZonePlayerObjectSync;

        private const float IntervalInSeconds = .2f;
        private int audioZoneColliderLayerMask;
        private VRCPlayerApi localPlayer;
        private int hitCount = 0;

        private readonly DataDictionary colliderCache = new DataDictionary();
        private readonly Collider[] hits = new Collider[25];
        private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        private void Start()
        {
            if (AudioZoneManager == null)
            {
                Debug.Log($" has no {nameof(AudioZoneManager)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            audioZonePlayerObjectSync = GetComponent<AbstractAudioZonePlayerObjectSync>();
            if (audioZonePlayerObjectSync == null)
            {
                Debug.Log($" has no {nameof(audioZonePlayerObjectSync)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }
            
            if (!Networking.IsOwner(gameObject)) return;

            localPlayer = Networking.LocalPlayer;
            audioZoneColliderLayerMask = LayerMask.GetMask("AudioZones");
            SendCustomEventDelayedSeconds(nameof(ValidateAudioZones), 10, EventTiming.LateUpdate);
        }

        public void ValidateAudioZones()
        {
            // stopwatch.Restart();
            audioZonePlayerObjectSync.OnValidateAudioZonesStart();
            var trackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.SetPositionAndRotation(trackingData.position, trackingData.rotation);

            var hasZonesChanged = CheckForAudioZoneColliders();
            hasZonesChanged = audioZonePlayerObjectSync.SwapDictionaries(hasZonesChanged);

            if (hasZonesChanged)
            {
                Debug.Log("Zone Changed");
                audioZonePlayerObjectSync.OnZoneChanged();
            }

            // stopwatch.Stop();
            // Debug.Log($"finished after {stopwatch.Elapsed.TotalMilliseconds}ms with {hitCount} hits and {hasZonesChanged}");
            SendCustomEventDelayedSeconds(nameof(ValidateAudioZones), IntervalInSeconds, EventTiming.LateUpdate);
        }

        private bool CheckForAudioZoneColliders()
        {
            var size = Physics.OverlapSphereNonAlloc(transform.position, .01f, hits, audioZoneColliderLayerMask, QueryTriggerInteraction.Collide);
            var hasZonesChanged = false;
            for (hitCount = 0; hitCount < size; hitCount++)
            {
                var hit = hits[hitCount];
                AudioZoneCollider audioZoneCollider;
                if (colliderCache.TryGetValue(hit.gameObject, out var token))
                {
                    audioZoneCollider = (AudioZoneCollider)token.Reference;
                }
                else
                {
                    audioZoneCollider = hit.GetComponent<AudioZoneCollider>();
                    colliderCache.Add(hit.gameObject, audioZoneCollider);
                }

                if (audioZoneCollider == null)
                {
                    Debug.LogError($"hit gameobject {hit.gameObject.name} which has no {nameof(AudioZoneCollider)}.");
                    continue;
                }

                hasZonesChanged = audioZonePlayerObjectSync.AddZoneId(audioZoneCollider, hasZonesChanged);
            }

            return hasZonesChanged;
        }
    }
}