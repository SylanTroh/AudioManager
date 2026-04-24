using System;
using UdonSharp;
using UnityEngine;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ShortAudioZoneSync : AudioZoneSyncArrayCore
    {
        // TODO: remove SerializeField, just used for testing
        /// <summary>
        /// <para>Sorted ascending. Can check for <see cref="AudioZoneManager.EmptyZoneIdIndex"/> by just
        /// checking index <c>0</c>, and can use <see cref="Array.BinarySearch(Array, object)"/>.</para>
        /// </summary>
        [UdonSynced, SerializeField] private ushort[] AudioZones = Array.Empty<ushort>();

        public override void OnDeserialization()
        {
            base.OnDeserialization();
            int count = AudioZones.Length;
            var audioZoneIndexes = new int[count];
            for (int i = 0; i < count; i++)
            {
                audioZoneIndexes[i] = AudioZones[i];
            }
            LogAudioZones(audioZoneIndexes);
        }

        protected override void InternalOnPreSerialization(int[] audioZonesIndexes, int[] audioSettingsIndexes)
        {
            // TODO: handle audioSettingsIndexes
            int count = audioZonesIndexes.Length;
            AudioZones = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                AudioZones[i] = (ushort)audioZonesIndexes[i];
            }
            LogAudioZones(audioZonesIndexes);
        }

        public override bool SharesAudioZoneWith(AudioZoneSyncCore other)
        {
            var remoteZoneIds = ((ShortAudioZoneSync)other).AudioZones;

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
    }
}
