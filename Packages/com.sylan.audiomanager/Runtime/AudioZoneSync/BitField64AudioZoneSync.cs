using UdonSharp;
using VRC.SDK3.Data;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BitField64AudioZoneSync : BitFieldAudioZoneSync
    {
        public override string SyncScriptName => nameof(BitField64AudioZoneSync);

        /// <summary>
        /// <para>Packed into <see cref="BitFieldAudioZoneSync.highestSyncedAudioZonesField"/> for actual syncing.</para>
        /// </summary>
        private ulong syncedAudioZonesField = 0uL; // Must have the proper "nothing" default value.

        private ulong oldFinalAudioZonesField;
        private ulong finalAudioZonesField;

        private ulong audioZonesField;
        private ulong negativeZonesField;

        public override void OnCheckForChangedAudioZones()
        {
            base.OnCheckForChangedAudioZones();
            audioZonesField = 0ul;
            negativeZonesField = 0ul;
        }

        public override void NotifyHitAudioZoneCollider(AudioZoneCollider audioZoneCollider)
        {
            if (audioZoneCollider.isNegativeZone)
            {
                negativeZonesField |= audioZoneCollider.combinedZoneIdsField1;
            }
            else
            {
                audioZonesField |= audioZoneCollider.combinedZoneIdsField1;
            }
        }

        public override bool HaveZonesChanged()
        {
            finalAudioZonesField = audioZonesField & ~negativeZonesField;

            bool hasChanged = oldFinalAudioZonesField != finalAudioZonesField;

            if (!hasChanged) return base.HaveZonesChanged();

            oldFinalAudioZonesField = finalAudioZonesField;

            return true;
        }

        protected override void PrepareForSerialization(ulong shiftedAudioSettingIndex)
        {
            syncedAudioZonesField = oldFinalAudioZonesField;
            highestSyncedAudioZonesField = syncedAudioZonesField | shiftedAudioSettingIndex;
#if AUDIO_MANAGER_DEBUG
            LogAudioZones();
#endif
        }

        public override void OnDeserialization()
        {
            syncedAudioZonesField = highestSyncedAudioZonesField & ~audioZoneManager.audioSettingsIndexBitMask;
            base.OnDeserialization();
#if AUDIO_MANAGER_DEBUG
            LogAudioZones();
#endif
        }

#if AUDIO_MANAGER_DEBUG
        private void LogAudioZones()
        {
            var audioZoneIndexes = new DataList();
            for (int i = 0; i < 64; i++)
            {
                if ((syncedAudioZonesField & (1uL << i)) != 0uL)
                {
                    audioZoneIndexes.Add(i);
                }
            }

            LogAudioZones(ToIntArray(audioZoneIndexes));
        }

        private int[] ToIntArray(DataList list)
        {
            int count = list.Count;
            int[] result = new int[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = list[i].Int;
            }
            return result;
        }
#endif

        public override bool SharesAudioZoneWith(AudioZoneSyncCore other)
        {
            var remoteAudioZonesField = ((BitField64AudioZoneSync)other).syncedAudioZonesField;

            if (IsInNoneOrEmpty(syncedAudioZonesField) && IsInNoneOrEmpty(remoteAudioZonesField)) return true;

            return (syncedAudioZonesField & remoteAudioZonesField) != 0uL;
        }

        private bool IsInNoneOrEmpty(ulong zonesField)
        {
            return zonesField == 0uL || (zonesField & AudioZoneManager.EmptyZoneIdBitFlag) != 0uL;
        }
    }
}
