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
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AudioZonePlayerObject : UdonSharpBehaviour
    {
        [HideInInspector, SerializeField] private AudioZoneManager AudioZoneManager;
        public const string AudioZoneManagerPropertyName = nameof(AudioZoneManager);

        [UdonSynced, SerializeField] private int[] AudioZones = Array.Empty<int>();
        [UdonSynced, SerializeField] private int[] NegativeAudioZones = Array.Empty<int>();

        private DataDictionary positiveDict = new DataDictionary();
        private DataDictionary oldPositiveDict = new DataDictionary();
        private DataDictionary negativeDict = new DataDictionary();
        private DataDictionary oldNegativeDict = new DataDictionary();
        private readonly DataDictionary colliderCache = new DataDictionary();

        private const float IntervalInSeconds = .2f;
        private int audioZoneColliderLayerMask;
        private VRCPlayerApi localPlayer;
        private int hitCount = 0;
        
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

            if (!Networking.IsOwner(gameObject)) return;

            localPlayer = Networking.LocalPlayer;
            audioZoneColliderLayerMask = LayerMask.GetMask("AudioZones");
            SendCustomEventDelayedSeconds(nameof(ValidateAudioZones), 10, EventTiming.LateUpdate);
        }

        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject)) return;
            Debug.Log("OnDeserialization");
            var owner = Networking.GetOwner(gameObject);
            AudioZoneManager.ClearAudioZones(owner);
            foreach (var audioZone in AudioZones)
            {
                AudioZoneManager.EnterAudioZone(owner, audioZone, false);
            }

            foreach (var audioZone in NegativeAudioZones)
            {
                AudioZoneManager.EnterAudioZone(owner, audioZone, true);
            }

            AudioZoneManager.UpdateAudioZoneSetting(owner);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal) return;
            RequestSerialization();
        }

        public void ValidateAudioZones()
        {
            stopwatch.Restart();
            var trackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.SetPositionAndRotation(trackingData.position, trackingData.rotation);

            var hasZonesChanged = CheckForAudioZoneColliders();
            hasZonesChanged = hasZonesChanged || oldPositiveDict.Count != positiveDict.Count || oldNegativeDict.Count != negativeDict.Count;
            if (hasZonesChanged)
            {
                AudioZones = GetAllKeysArray(positiveDict);
                NegativeAudioZones = GetAllKeysArray(negativeDict);
                RequestSerialization();
            }

            SwapDictionaries();

            stopwatch.Stop();
            Debug.Log($"finished after {stopwatch.Elapsed.TotalMilliseconds}ms with {hitCount} hits and {hasZonesChanged}");
            SendCustomEventDelayedSeconds(nameof(ValidateAudioZones), IntervalInSeconds, EventTiming.LateUpdate);
        }

        private void SwapDictionaries()
        {
            var tmpSwappingDict = oldPositiveDict;
            oldPositiveDict = positiveDict;
            positiveDict = tmpSwappingDict;
            positiveDict.Clear();

            tmpSwappingDict = oldNegativeDict;
            oldNegativeDict = negativeDict;
            negativeDict = tmpSwappingDict;
            negativeDict.Clear();
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

                var dict = audioZoneCollider.isNegativeZone ? negativeDict : positiveDict;
                var oldDict = audioZoneCollider.isNegativeZone ? oldNegativeDict : oldPositiveDict;

                hasZonesChanged = AddZoneId(oldDict, audioZoneCollider.zoneIdIndex, hasZonesChanged, dict, audioZoneCollider);
                foreach (var zoneId in audioZoneCollider.transitionZoneIdIndexes)
                {
                    hasZonesChanged = AddZoneId(oldDict, zoneId, hasZonesChanged, dict, audioZoneCollider);
                }
            }

            return hasZonesChanged;
        }

        private static bool AddZoneId(DataDictionary oldDict, int zoneId, bool hasZonesChanged, DataDictionary dict, AudioZoneCollider audioZoneCollider)
        {
            if (!oldDict.ContainsKey(zoneId))
            {
                hasZonesChanged = true;
            }

            dict.SetValue(zoneId, true);
            return hasZonesChanged;
        }

        private static int[] GetAllKeysArray(DataDictionary dict)
        {
            var keys = new int[dict.Count];
            var list = dict.GetKeys();
            for (var i = list.Count - 1; i >= 0; i--)
            {
                keys[i] = list[i].Int;
            }

            return keys;
        }
    }
}