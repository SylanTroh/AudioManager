using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BitField64AudioZoneSync : AudioZoneSyncCore
    {
        // TODO: remove SerializeField, just used for testing
        [UdonSynced, SerializeField] private ulong syncedAudioZonesField = 0uL;

        private ulong oldSettingZonesField;
        private ulong oldFinalAudioZonesField;
        private ulong finalAudioZonesField;

        private ulong settingZonesField;
        private ulong audioZonesField;
        private ulong negativeZonesField;

        public override void OnValidateAudioZonesStart()
        {
            settingZonesField = 0ul;
            audioZonesField = 0ul;
            negativeZonesField = 0ul;
        }

        public override void NotifyAudioSettingCollider(AudioSettingCollider audioSettingCollider)
        {
            // TODO: generate SettingIndex flag at build time
            settingZonesField |= 1uL << audioSettingCollider.SettingIndex;
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

        public override bool HasZoneChanged()
        {
            finalAudioZonesField = audioZonesField & ~negativeZonesField;

            bool hasChanged = oldSettingZonesField != settingZonesField
                || oldFinalAudioZonesField != finalAudioZonesField;

            if (!hasChanged) return false;

            oldSettingZonesField = settingZonesField;
            oldFinalAudioZonesField = finalAudioZonesField;

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
            syncedAudioZonesField = oldFinalAudioZonesField;
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
