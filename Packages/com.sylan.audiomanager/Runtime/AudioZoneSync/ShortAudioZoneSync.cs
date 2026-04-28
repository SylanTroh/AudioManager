using System;
using UdonSharp;
using UnityEngine;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ShortAudioZoneSync : AudioZoneSyncArrayCore
    {
        /// <summary>
        /// <para>Sorted ascending. Can check for <see cref="AudioZoneManager.EmptyZoneIdIndex"/> by just
        /// checking index <c>0</c>, and can use <see cref="Array.BinarySearch(Array, object)"/>.</para>
        /// </summary>
        [UdonSynced] private ushort[] syncedIds = Array.Empty<ushort>();
        // TODO: remove SerializeField, just used for testing
        [SerializeField] private ushort[] syncedAudioZones;

        public override void OnDeserialization()
        {
            // Basically identical to IntegerAudioZoneSync.
            int count = syncedIds.Length;
            if (count == 0 || syncedIds[count - 1] < AudioZoneManager.totalAudioZonesCount)
            {
                syncedAudioSettingIndex = -1;
                syncedAudioZones = syncedIds;
            }
            else
            {
                syncedAudioSettingIndex = syncedIds[count - 1] - AudioZoneManager.totalAudioZonesCount;
                syncedAudioZones = new ushort[count - 1];
                Array.Copy(syncedIds, syncedAudioZones, count - 1);
            }

            base.OnDeserialization();
#if AUDIO_MANAGER_DEBUG
            var audioZoneIndexes = new int[syncedAudioZones.Length];
            for (int i = 0; i < syncedAudioZones.Length; i++)
            {
                audioZoneIndexes[i] = syncedAudioZones[i];
            }
            LogAudioZones(audioZoneIndexes);
#endif
        }

        protected override void InternalOnPreSerialization(int[] audioZonesIndexes, int audioSettingsIndex)
        {
            int count = audioZonesIndexes.Length;
            syncedAudioZones = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                syncedAudioZones[i] = (ushort)audioZonesIndexes[i];
            }

            // Basically identical to IntegerAudioZoneSync.
            if (audioSettingsIndex == -1)
            {
                syncedIds = syncedAudioZones;
            }
            else
            {
                int newCount = syncedAudioZones.Length + 1;
                syncedIds = new ushort[newCount];
                Array.Copy(syncedAudioZones, syncedIds, newCount - 1);
                syncedIds[newCount - 1] = (ushort)(audioSettingsIndex + AudioZoneManager.totalAudioZonesCount);
            }

#if AUDIO_MANAGER_DEBUG
            LogAudioZones(audioZonesIndexes);
#endif
        }

        public override bool SharesAudioZoneWith(AudioZoneSyncCore other)
        {
            var remoteZoneIds = ((ShortAudioZoneSync)other).syncedAudioZones;

            var localInNullOrEmpty = syncedAudioZones.Length == 0 || syncedAudioZones[0] == AudioZoneManager.EmptyZoneIdIndex;
            var remoteInNullOrEmpty = remoteZoneIds.Length == 0 || remoteZoneIds[0] == AudioZoneManager.EmptyZoneIdIndex;

            if (localInNullOrEmpty && remoteInNullOrEmpty) return true;
            if (localInNullOrEmpty != remoteInNullOrEmpty) return false;

            foreach (var remoteZoneId in remoteZoneIds)
            {
                if (Array.BinarySearch(syncedAudioZones, remoteZoneId) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
