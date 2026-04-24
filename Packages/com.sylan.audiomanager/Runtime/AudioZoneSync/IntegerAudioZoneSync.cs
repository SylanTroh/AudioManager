using System;
using UdonSharp;
using UnityEngine;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class IntegerAudioZoneSync : AudioZoneSyncCore
    {
        [UdonSynced, SerializeField] private int[] AudioZones = Array.Empty<int>();

        public override void OnDeserialization()
        {
            base.OnDeserialization();
            LogAudioZones();
        }

        protected override void InternalOnPreSerialization(int[] audioZonesIndexes, int[] audioSettingsIndexes)
        {
            AudioZones = audioZonesIndexes;
            LogAudioZones();
        }

        private void LogAudioZones()
        {
            var zoneNames = new string[AudioZones.Length];
            for (var index = 0; index < AudioZones.Length; index++)
            {
                var audioZoneIndex = AudioZones[index];
                zoneNames[index] = AudioZoneManager.ZoneIdMapping[audioZoneIndex];
            }

            Debug.Log($"Player {OwningPlayer.PrintName()} entered Zones: '{string.Join("', '", zoneNames)}'");
        }

        public override bool SharesAudioZoneWith(AudioZoneSyncCore other)
        {
            var remoteZoneIds = ((IntegerAudioZoneSync)other).AudioZones;

            var localInNullOrEmpty = AudioZones.Length == 0 || AudioZones[0] == AudioZoneManager.EmptyZoneIdIndex;
            var remoteInNullOrEmpty = remoteZoneIds.Length == 0 || remoteZoneIds[0] == AudioZoneManager.EmptyZoneIdIndex;

            if (localInNullOrEmpty && remoteInNullOrEmpty) return true;
            if (localInNullOrEmpty != remoteInNullOrEmpty) return false;

            foreach (var remoteZoneId in remoteZoneIds)
            {
                if (Array.BinarySearch(AudioZones, remoteZoneId) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnZoneChanged()
        {
            OnPreSerialization(); //TODO remove, just here for testing
            base.OnZoneChanged();
        }
    }
}