using UdonSharp;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class BitFieldAudioZoneSync : AudioZoneSyncCore
    {
        /// <summary>
        /// <para>Uses lower bits as audio zone indexes.</para>
        /// <para>Uses higher bits for the audio setting zone index.</para>
        /// </summary>
        [UdonSynced] protected ulong highestSyncedAudioZonesField = 0uL; // Must have the proper "nothing" default value.

        protected abstract void PrepareForSerialization(ulong shiftedAudioSettingIndex);

        protected override void PrepareForSerialization(int audioSettingIndex)
        {
            PrepareForSerialization(((ulong)(audioSettingIndex + 1)) << audioZoneManager.audioSettingsIndexBitShift);
        }

        public override void OnDeserialization()
        {
            syncedAudioSettingIndex = (int)(highestSyncedAudioZonesField >> audioZoneManager.audioSettingsIndexBitShift) - 1;
            base.OnDeserialization();
        }
    }
}
