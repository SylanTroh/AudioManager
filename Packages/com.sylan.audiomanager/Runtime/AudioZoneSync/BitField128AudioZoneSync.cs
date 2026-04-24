using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BitField128AudioZoneSync : AudioZoneSyncCore
    {
        // TODO: remove SerializeField, just used for testing
        [UdonSynced, SerializeField] private ulong syncedAudioZonesField1 = 0uL;
        [UdonSynced, SerializeField] private ulong syncedAudioZonesField2 = 0uL;

        private ulong oldSettingZonesField1;
        private ulong oldSettingZonesField2;
        private ulong oldFinalAudioZonesField1;
        private ulong oldFinalAudioZonesField2;
        private ulong finalAudioZonesField1;
        private ulong finalAudioZonesField2;

        private ulong settingZonesField1;
        private ulong settingZonesField2;
        private ulong audioZonesField1;
        private ulong audioZonesField2;
        private ulong negativeZonesField1;
        private ulong negativeZonesField2;

        public override void OnValidateAudioZonesStart()
        {
            settingZonesField1 = 0ul;
            settingZonesField2 = 0ul;
            audioZonesField1 = 0ul;
            audioZonesField2 = 0ul;
            negativeZonesField1 = 0ul;
            negativeZonesField2 = 0ul;
        }

        public override void NotifyAudioSettingCollider(AudioSettingCollider audioSettingCollider)
        {
            // TODO: generate SettingIndex flag at build time
            // settingZonesField1 |= 1uL << audioSettingCollider.SettingIndex;
            // settingZonesField2 |= 1uL << audioSettingCollider.SettingIndex;
        }

        public override void NotifyHitAudioZoneCollider(AudioZoneCollider audioZoneCollider)
        {
            if (audioZoneCollider.isNegativeZone)
            {
                negativeZonesField1 |= audioZoneCollider.combinedZoneIdsField1;
                negativeZonesField2 |= audioZoneCollider.combinedZoneIdsField2;
            }
            else
            {
                audioZonesField1 |= audioZoneCollider.combinedZoneIdsField1;
                audioZonesField2 |= audioZoneCollider.combinedZoneIdsField2;
            }
        }

        public override bool HasZoneChanged()
        {
            finalAudioZonesField1 = audioZonesField1 & ~negativeZonesField1;
            finalAudioZonesField2 = audioZonesField2 & ~negativeZonesField2;

            bool hasChanged = oldSettingZonesField1 != settingZonesField1
                           || oldSettingZonesField2 != settingZonesField2
                           || oldFinalAudioZonesField1 != finalAudioZonesField1
                           || oldFinalAudioZonesField2 != finalAudioZonesField2;

            if (!hasChanged) return false;

            oldSettingZonesField1 = settingZonesField1;
            oldSettingZonesField2 = settingZonesField2;
            oldFinalAudioZonesField1 = finalAudioZonesField1;
            oldFinalAudioZonesField2 = finalAudioZonesField2;

            return true;
        }

        public override void OnDeserialization()
        {
            base.OnDeserialization();
            LogAudioZones();
        }

        public override void OnPreSerialization()
        {
            // TODO: handle oldSettingZonesField
            syncedAudioZonesField1 = oldFinalAudioZonesField1;
            syncedAudioZonesField2 = oldFinalAudioZonesField2;
            LogAudioZones();
        }

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

        public override bool SharesAudioZoneWith(AudioZoneSyncCore other)
        {
            var remoteAudioZonesField1 = ((BitField128AudioZoneSync)other).syncedAudioZonesField1;
            var remoteAudioZonesField2 = ((BitField128AudioZoneSync)other).syncedAudioZonesField2;

            if (IsInNoneOrEmpty(syncedAudioZonesField1, syncedAudioZonesField2)
                && IsInNoneOrEmpty(remoteAudioZonesField1, remoteAudioZonesField2))
            {
                return true;
            }

            return (syncedAudioZonesField1 & remoteAudioZonesField1) != 0uL
                || (syncedAudioZonesField2 & remoteAudioZonesField2) != 0uL;
        }

        private bool IsInNoneOrEmpty(ulong zonesField1, ulong zonesField2)
        {
            return (zonesField1 == 0uL && zonesField2 == 0uL) || (zonesField1 & AudioZoneManager.EmptyZoneIdBitFlag) != 0uL;
        }
    }
}
