using System;
using UdonSharp;
using UnityEngine;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class IntegerAudioZoneSync : AudioZoneSyncArrayCore
    {
        protected override string SyncScriptName => nameof(IntegerAudioZoneSync);

        /// <summary>
        /// <para>Sorted ascending. Can check for <see cref="AudioZoneManager.EmptyZoneIdIndex"/> by just
        /// checking index <c>0</c>, and can use <see cref="Array.BinarySearch(Array, object)"/>.</para>
        /// <para>When <see cref="AudioZoneSyncCore.syncedAudioSettingIndex"/> is not <c>-1</c>, it will be
        /// contained in this array at the end, with <see cref="AudioZoneManager.totalAudioZonesCount"/> added to it.</para>
        /// </summary>
        [UdonSynced] private int[] syncedIds = Array.Empty<int>();
        // TODO: remove SerializeField, just used for testing
        /// <summary>
        /// <para>Must not be <see langword="null"/>, could be used before running
        /// <see cref="OnDeserialization"/> nor <see cref="InternalOnPreSerialization(int[], int)"/>.</para>
        /// </summary>
        [SerializeField] private int[] syncedAudioZones = Array.Empty<int>();

        public override void OnDeserialization()
        {
            // Basically identical to ShortAudioZoneSync.
            int count = syncedIds.Length;
            if (count == 0 || syncedIds[count - 1] < AudioZoneManager.totalAudioZonesCount)
            {
                syncedAudioSettingIndex = -1;
                syncedAudioZones = syncedIds;
            }
            else
            {
                syncedAudioSettingIndex = syncedIds[count - 1] - AudioZoneManager.totalAudioZonesCount;
                syncedAudioZones = new int[count - 1];
                Array.Copy(syncedIds, syncedAudioZones, count - 1);
            }

            base.OnDeserialization();
#if AUDIO_MANAGER_DEBUG
            LogAudioZones(syncedAudioZones);
#endif
        }

        protected override void InternalOnPreSerialization(int[] audioZonesIndexes, int audioSettingsIndex)
        {
            // Basically identical to ShortAudioZoneSync.
            syncedAudioZones = audioZonesIndexes;
            if (audioSettingsIndex == -1)
            {
                syncedIds = syncedAudioZones;
            }
            else
            {
                int newCount = syncedAudioZones.Length + 1;
                syncedIds = new int[newCount];
                Array.Copy(syncedAudioZones, syncedIds, newCount - 1);
                syncedIds[newCount - 1] = audioSettingsIndex + AudioZoneManager.totalAudioZonesCount;
            }

#if AUDIO_MANAGER_DEBUG
            LogAudioZones(syncedAudioZones);
#endif
        }

        public override bool SharesAudioZoneWith(AudioZoneSyncCore other)
        {
            var remoteZoneIds = ((IntegerAudioZoneSync)other).syncedAudioZones;

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
