using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class IntegerAudioZonePlayerObjectSync : AbstractAudioZonePlayerObjectSync
    {
        [UdonSynced, SerializeField] private int[] AudioZones = Array.Empty<int>();

        private readonly DataDictionary serializationHelperDict = new DataDictionary();


        public override void OnPreSerialization()
        {
            var keys = oldNegativeDict.GetKeys();
            Debug.Log("OldNegativeDict:" + oldNegativeDict.Count);
            Debug.Log("negativeDict:" + negativeDict.Count);
            for (var i = 0; i < keys.Count; i++)
            {
                //TODO can we somehow do it without an this extra dict...?
                serializationHelperDict.Remove(keys[i].Int);
            }

            AudioZones = GetAllKeysArray(serializationHelperDict);
        }

        protected override void NotifyAudioManager(VRCPlayerApi player)
        {
            AudioZoneManager.ClearAudioZones(player);
            foreach (var audioZone in AudioZones)
            {
                AudioZoneManager.EnterAudioZone(player, audioZone, false);
            }

            AudioZoneManager.UpdateAudioZoneSetting(player);
        }

        public override void OnZoneChanged()
        {
            OnPreSerialization(); //TODO remove, just here for testing
            base.OnZoneChanged();
        }

        public override void OnValidateAudioZonesStart()
        {
            serializationHelperDict.Clear();
        }

        protected override bool AddZoneId(DataDictionary oldDict, int zoneId, bool hasZonesChanged, DataDictionary dict, bool isNegativeZone)
        {
            if (!isNegativeZone)
            {
                serializationHelperDict.SetValue(zoneId, true);
            }

            return base.AddZoneId(oldDict, zoneId, hasZonesChanged, dict, isNegativeZone);
        }

        private int[] GetAllKeysArray(DataDictionary dict)
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