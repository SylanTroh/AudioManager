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
