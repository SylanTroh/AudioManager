using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class BitField64AudioZoneSync : AudioZoneSyncCore
    {
        [UdonSynced, SerializeField] private ulong audioZonesField = 0uL;

        public override void OnDeserialization()
        {
            base.OnDeserialization();
            LogAudioZones();
        }

        protected override void InternalOnPreSerialization(int[] audioZonesIndexes, int[] audioSettingsIndexes)
        {
            // TODO: handle audioSettingsIndexes
            audioZonesField = 0uL;
            foreach (var index in audioZonesIndexes)
            {
                audioZonesField |= 1uL << index;
            }
            LogAudioZones();
        }

        private void LogAudioZones()
        {
            var audioZoneIndexes = new DataList();
            for (int i = 0; i < 64; i++)
            {
                if ((audioZonesField & (1uL << i)) != 0uL)
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

        public override bool SharesAudioZoneWith(AudioZoneSyncCore other)
        {
            var remoteAudioZonesField = ((BitField64AudioZoneSync)other).audioZonesField;

            if (IsInNoneOrEmpty(audioZonesField) && IsInNoneOrEmpty(remoteAudioZonesField)) return true;

            return (audioZonesField & remoteAudioZonesField) != 0uL;
        }

        private bool IsInNoneOrEmpty(ulong zonesField)
        {
            return zonesField == 0uL || (zonesField & AudioZoneManager.EmptyZoneIdBitFlag) != 0uL;
        }
    }
}
