using System;
using UdonSharp;
using UnityEngine;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class IntegerAudioZoneSync : AudioZoneSyncArrayCore
    {
        // TODO: remove SerializeField, just used for testing
        /// <summary>
        /// <para>Sorted ascending. Can check for <see cref="AudioZoneManager.EmptyZoneIdIndex"/> by just
        /// checking index <c>0</c>, and can use <see cref="Array.BinarySearch(Array, object)"/>.</para>
        /// </summary>
        [UdonSynced, SerializeField] private int[] AudioZones = Array.Empty<int>();

#if AUDIO_MANAGER_DEBUG
        public override void OnDeserialization()
        {
            base.OnDeserialization();
            LogAudioZones(AudioZones);
        }
#endif

        protected override void InternalOnPreSerialization(int[] audioZonesIndexes, int[] audioSettingsIndexes)
        {
            // TODO: handle audioSettingsIndexes
            AudioZones = audioZonesIndexes;
#if AUDIO_MANAGER_DEBUG
            LogAudioZones(AudioZones);
#endif
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
    }
}
