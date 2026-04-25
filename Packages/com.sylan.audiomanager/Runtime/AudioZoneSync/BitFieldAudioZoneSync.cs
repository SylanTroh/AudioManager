using UdonSharp;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class BitFieldAudioZoneSync : AudioZoneSyncCore
    {
        [UdonSynced] protected ulong highestSyncedAudioZonesField = 0uL;

        protected abstract void InternalOnPreSerialization(ulong shiftedAudioSettingIndex);

        protected override void InternalOnPreSerialization(int audioSettingIndex)
        {
            InternalOnPreSerialization(((ulong)(audioSettingIndex + 1)) << AudioZoneManager.audioSettingsIndexBitShift);
        }

        public override void OnDeserialization()
        {
            syncedAudioSettingIndex = (int)(highestSyncedAudioZonesField >> AudioZoneManager.audioSettingsIndexBitShift) - 1;
            base.OnDeserialization();
        }
    }
}
