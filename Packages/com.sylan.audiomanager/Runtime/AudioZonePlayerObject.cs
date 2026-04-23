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

        private DataDictionary positiveDict = new DataDictionary();
        private DataDictionary oldPositiveDict = new DataDictionary();
        private DataDictionary negativeDict = new DataDictionary();
        private DataDictionary oldNegativeDict = new DataDictionary();
        private readonly DataDictionary serialisationHelperDict = new DataDictionary();
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
            NotifyAudioManager(owner);
        }

        private void NotifyAudioManager(VRCPlayerApi player)
        {
            AudioZoneManager.ClearAudioZones(player);
            foreach (var audioZone in AudioZones)
            {
                AudioZoneManager.EnterAudioZone(player, audioZone, false);
            }

            AudioZoneManager.UpdateAudioZoneSetting(player);
        }

        public override void OnPreSerialization()
        {
            var keys = oldNegativeDict.GetKeys();
            for (var i = 0; i < keys.Count; i++)
            {
                //TODO can we somehow do it without an this extra dict...?
                serialisationHelperDict.Remove(keys[i].Int);
            }

            AudioZones = GetAllKeysArray(serialisationHelperDict);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal) return;
            RequestSerialization();
        }

        public void ValidateAudioZones()
        {
            // stopwatch.Restart();
            var trackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.SetPositionAndRotation(trackingData.position, trackingData.rotation);

            var hasZonesChanged = CheckForAudioZoneColliders();
            hasZonesChanged = hasZonesChanged || oldPositiveDict.Count != positiveDict.Count || oldNegativeDict.Count != negativeDict.Count;
            SwapDictionaries();

            if (hasZonesChanged)
            {
                Debug.Log("Zone Changed");
                RequestSerialization();
                // OnPreSerialization(); //TODO remove, just here for testing
                NotifyAudioManager(localPlayer);
            }

            // stopwatch.Stop();
            // Debug.Log($"finished after {stopwatch.Elapsed.TotalMilliseconds}ms with {hitCount} hits and {hasZonesChanged}");
            SendCustomEventDelayedSeconds(nameof(ValidateAudioZones), IntervalInSeconds, EventTiming.LateUpdate);
        }

        private void SwapDictionaries()
        {
            var tmpSwappingDict = oldPositiveDict;
            oldPositiveDict = positiveDict;
            positiveDict = tmpSwappingDict;
            tmpSwappingDict.Clear();

            tmpSwappingDict = oldNegativeDict;
            oldNegativeDict = negativeDict;
            negativeDict = tmpSwappingDict;
            tmpSwappingDict.Clear();
        }

        private bool CheckForAudioZoneColliders()
        {
            serialisationHelperDict.Clear();
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

                hasZonesChanged = AddZoneId(oldDict, audioZoneCollider.zoneIdIndex, hasZonesChanged, dict, audioZoneCollider.isNegativeZone);
                foreach (var zoneId in audioZoneCollider.transitionZoneIdIndexes)
                {
                    hasZonesChanged = AddZoneId(oldDict, zoneId, hasZonesChanged, dict, audioZoneCollider.isNegativeZone);
                }
            }

            return hasZonesChanged;
        }

        private bool AddZoneId(DataDictionary oldDict, int zoneId, bool hasZonesChanged, DataDictionary dict, bool isNegativeZone)
        {
            if (!oldDict.ContainsKey(zoneId))
            {
                hasZonesChanged = true;
            }

            if (!isNegativeZone)
            {
                serialisationHelperDict.SetValue(zoneId, true);
            }

            dict.SetValue(zoneId, true);
            return hasZonesChanged;
        }

        private static int[] GetAllKeysArray(DataDictionary dict)
        {
            var keys = new int[dict.Count];
            var list = dict.GetKeys();
            for (var i = 0; i < list.Count; i++)
            {
                keys[i] = list[i].Int;
            }

            return keys;
        }
    }
}