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
    [RequireComponent(typeof(AudioZonePlayerObject))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class AbstractAudioZonePlayerObjectSync : UdonSharpBehaviour
    {
        protected AudioZoneManager AudioZoneManager;
        protected AudioZonePlayerObject AudioZonePlayerObject;

        protected DataDictionary positiveDict = new DataDictionary();
        protected DataDictionary oldPositiveDict = new DataDictionary();
        protected DataDictionary negativeDict = new DataDictionary();
        protected DataDictionary oldNegativeDict = new DataDictionary();

        protected VRCPlayerApi localPlayer;

        void Start()
        {
            AudioZonePlayerObject = GetComponent<AudioZonePlayerObject>();
            AudioZoneManager = GetComponent<AudioZonePlayerObject>().AudioZoneManager;
            if (AudioZoneManager == null)
            {
                Debug.Log($" has no {nameof(AudioZoneManager)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            if (!Networking.IsOwner(gameObject)) return;

            localPlayer = Networking.LocalPlayer;
        }

        public abstract void OnValidateAudioZonesStart();

        protected abstract void NotifyAudioManager(VRCPlayerApi player);

        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject)) return;
            Debug.Log("OnDeserialization");
            var owner = Networking.GetOwner(gameObject);
            NotifyAudioManager(owner);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal) return;
            RequestSerialization();
        }

        public bool AddZoneId(AudioZoneCollider audioZoneCollider, bool hasZonesChanged)
        {
            var dict = audioZoneCollider.isNegativeZone ? negativeDict : positiveDict;
            var oldDict = audioZoneCollider.isNegativeZone ? oldNegativeDict : oldPositiveDict;

            hasZonesChanged = AddZoneId(oldDict, audioZoneCollider.zoneIdIndex, hasZonesChanged, dict, audioZoneCollider.isNegativeZone);
            foreach (var zoneId in audioZoneCollider.transitionZoneIdIndexes)
            {
                hasZonesChanged = AddZoneId(oldDict, zoneId, hasZonesChanged, dict, audioZoneCollider.isNegativeZone);
            }

            return hasZonesChanged;
        }

        public virtual void OnZoneChanged()
        {
            RequestSerialization();
            NotifyAudioManager(localPlayer);
        }

        public bool SwapDictionaries(bool hasZonesChanged)
        {
            // Debug.Log($"SwapDictionaries with {nameof(hasZonesChanged)} '{hasZonesChanged}',  {oldPositiveDict.Count}, {positiveDict.Count}, {oldNegativeDict.Count}, {negativeDict.Count}");
            hasZonesChanged = hasZonesChanged || oldPositiveDict.Count != positiveDict.Count || oldNegativeDict.Count != negativeDict.Count;
            //TODO currently when u go from no zone into a negative zone + positive zone with same Id it counts as changing the zone because the dict counts changes
            //But the negative with positive zone combined would be no zone again => no change.
            //We could only catch that by combining the dicts every time and compare with the old combined dict... which would be a few loops to do.

            var tmpSwappingDict = oldPositiveDict;
            oldPositiveDict = positiveDict;
            positiveDict = tmpSwappingDict;
            tmpSwappingDict.Clear();

            tmpSwappingDict = oldNegativeDict;
            oldNegativeDict = negativeDict;
            negativeDict = tmpSwappingDict;
            tmpSwappingDict.Clear();

            return hasZonesChanged;
        }

        protected virtual bool AddZoneId(DataDictionary oldDict, int zoneId, bool hasZonesChanged, DataDictionary dict, bool isNegativeZone)
        {
            if (!oldDict.ContainsKey(zoneId))
            {
                Debug.Log($"Zone changed because of zondeId {zoneId}");
                hasZonesChanged = true;
            }

            dict.SetValue(zoneId, true);
            return hasZonesChanged;
        }
    }
}