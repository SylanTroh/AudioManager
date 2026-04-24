using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BitField192AudioZoneSync : AudioZoneSyncCore
    {
        // TODO: remove SerializeField, just used for testing
        [UdonSynced, SerializeField] private ulong syncedAudioZonesField1 = 0uL;
        [UdonSynced, SerializeField] private ulong syncedAudioZonesField2 = 0uL;
        [UdonSynced, SerializeField] private ulong syncedAudioZonesField3 = 0uL;

        private ulong oldSettingZonesField1;
        private ulong oldSettingZonesField2;
        private ulong oldSettingZonesField3;
        private ulong oldFinalAudioZonesField1;
        private ulong oldFinalAudioZonesField2;
        private ulong oldFinalAudioZonesField3;
        private ulong finalAudioZonesField1;
        private ulong finalAudioZonesField2;
        private ulong finalAudioZonesField3;

        private ulong settingZonesField1;
        private ulong settingZonesField2;
        private ulong settingZonesField3;
        private ulong audioZonesField1;
        private ulong audioZonesField2;
        private ulong audioZonesField3;
        private ulong negativeZonesField1;
        private ulong negativeZonesField2;
        private ulong negativeZonesField3;

        public override void OnValidateAudioZonesStart()
        {
            settingZonesField1 = 0ul;
            settingZonesField2 = 0ul;
            settingZonesField3 = 0ul;
            audioZonesField1 = 0ul;
            audioZonesField2 = 0ul;
            audioZonesField3 = 0ul;
            negativeZonesField1 = 0ul;
            negativeZonesField2 = 0ul;
            negativeZonesField3 = 0ul;
        }

        public override void NotifyAudioSettingCollider(AudioSettingCollider audioSettingCollider)
        {
            // TODO: generate SettingIndex flag at build time
            // settingZonesField1 |= 1uL << audioSettingCollider.SettingIndex;
            // settingZonesField2 |= 1uL << audioSettingCollider.SettingIndex;
            // settingZonesField3 |= 1uL << audioSettingCollider.SettingIndex;
        }

        public override void NotifyHitAudioZoneCollider(AudioZoneCollider audioZoneCollider)
        {
            if (audioZoneCollider.isNegativeZone)
            {
                negativeZonesField1 |= audioZoneCollider.combinedZoneIdsField1;
                negativeZonesField2 |= audioZoneCollider.combinedZoneIdsField2;
                negativeZonesField3 |= audioZoneCollider.combinedZoneIdsField3;
            }
            else
            {
                audioZonesField1 |= audioZoneCollider.combinedZoneIdsField1;
                audioZonesField2 |= audioZoneCollider.combinedZoneIdsField2;
                audioZonesField3 |= audioZoneCollider.combinedZoneIdsField3;
            }
        }

        public override bool HasZoneChanged()
        {
            finalAudioZonesField1 = audioZonesField1 & ~negativeZonesField1;
            finalAudioZonesField2 = audioZonesField2 & ~negativeZonesField2;
            finalAudioZonesField3 = audioZonesField3 & ~negativeZonesField3;

            bool hasChanged = oldSettingZonesField1 != settingZonesField1
                           || oldSettingZonesField2 != settingZonesField2
                           || oldSettingZonesField3 != settingZonesField3
                           || oldFinalAudioZonesField1 != finalAudioZonesField1
                           || oldFinalAudioZonesField2 != finalAudioZonesField2
                           || oldFinalAudioZonesField3 != finalAudioZonesField3;

            if (!hasChanged) return false;

            oldSettingZonesField1 = settingZonesField1;
            oldSettingZonesField2 = settingZonesField2;
            oldSettingZonesField3 = settingZonesField3;
            oldFinalAudioZonesField1 = finalAudioZonesField1;
            oldFinalAudioZonesField2 = finalAudioZonesField2;
            oldFinalAudioZonesField3 = finalAudioZonesField3;

            return true;
        }

#if AUDIO_MANAGER_DEBUG
        public override void OnDeserialization()
        {
            base.OnDeserialization();
            LogAudioZones();
        }
#endif

        public override void OnPreSerialization()
        {
            // TODO: handle oldSettingZonesField
            syncedAudioZonesField1 = oldFinalAudioZonesField1;
            syncedAudioZonesField2 = oldFinalAudioZonesField2;
            syncedAudioZonesField3 = oldFinalAudioZonesField3;
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
                if ((syncedAudioZonesField1 & (1uL << i)) != 0uL)
                {
                    audioZoneIndexes.Add(i);
                }
                if ((syncedAudioZonesField2 & (1uL << i)) != 0uL)
                {
                    audioZoneIndexes.Add(64 + i);
                }
                if ((syncedAudioZonesField3 & (1uL << i)) != 0uL)
                {
                    audioZoneIndexes.Add(128 + i);
                }
            }

            LogAudioZones(ToIntArray(audioZoneIndexes));
        }
#endif

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

        public override bool SharesAudioZoneWith(AudioZoneSyncCore other)
        {
            var remoteAudioZonesField1 = ((BitField192AudioZoneSync)other).syncedAudioZonesField1;
            var remoteAudioZonesField2 = ((BitField192AudioZoneSync)other).syncedAudioZonesField2;
            var remoteAudioZonesField3 = ((BitField192AudioZoneSync)other).syncedAudioZonesField3;

            if (IsInNoneOrEmpty(syncedAudioZonesField1, syncedAudioZonesField2, remoteAudioZonesField3)
                && IsInNoneOrEmpty(remoteAudioZonesField1, remoteAudioZonesField2, remoteAudioZonesField3))
            {
                return true;
            }

            return (syncedAudioZonesField1 & remoteAudioZonesField1) != 0uL
                || (syncedAudioZonesField2 & remoteAudioZonesField2) != 0uL
                || (syncedAudioZonesField3 & remoteAudioZonesField3) != 0uL;
        }

        private bool IsInNoneOrEmpty(ulong zonesField1, ulong zonesField2, ulong zonesField3)
        {
            return (zonesField1 == 0uL && zonesField2 == 0uL && zonesField3 == 0uL)
                || (zonesField1 & AudioZoneManager.EmptyZoneIdBitFlag) != 0uL;
        }
    }
}
