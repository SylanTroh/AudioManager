using UdonSharp;
using UnityEngine;
using VRC.SDK3.Network;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class AudioZoneSyncCore : UdonSharpBehaviour
    {
        public VRCPlayerApi owningPlayer;

        protected AudioZoneManager audioZoneManager;
        protected AudioZonePlayerObject audioZonePlayerObject;

        /// <summary>
        /// <para><c>-1</c> indicates not being in any setting zone.</para>
        /// <para>Not marked with <see cref="UdonSyncedAttribute"/>, deriving classes must sync this value.</para>
        /// </summary>
        protected int syncedAudioSettingIndex = -1; // Must have the proper "nothing" default value.

        private AudioSettingCollider activeSettingZone;
        private AudioSettingCollider oldActiveSettingZone;

        public abstract bool SharesAudioZoneWith(AudioZoneSyncCore other);

        public abstract string SyncScriptName { get; }

        private bool didGetAppliedOnce = false;
        private bool requestSerializationIsQueued = false;

        private void Start()
        {
            audioZonePlayerObject = transform.parent.GetComponent<AudioZonePlayerObject>();
            audioZoneManager = audioZonePlayerObject.audioZoneManager;
            if (audioZoneManager == null)
            {
                Debug.LogError($"[AudioManager] {nameof(AudioZoneSyncCore)} has no {nameof(audioZoneManager)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            audioZoneManager.Register(this);
            owningPlayer = Networking.GetOwner(gameObject);

            // Small delay in case we get serialized data, at which point the double apply would be redundant.
            SendCustomEventDelayedSeconds(nameof(StartDelayed), 0.2f);
        }

        public void StartDelayed()
        {
            if (didGetAppliedOnce) return;
            // Relies on all synced setting and zone variables's default values, this is the reason for why
            // those have the comment "Must have the proper "nothing" default value.".
            ApplySettingAndAudioZoneSetting();
        }

        private void OnDestroy()
        {
            audioZoneManager.Deregister(this);
        }

#if AUDIO_MANAGER_DEBUG
        protected void LogAudioZones(int[] audioZoneIndexes)
        {
            var zoneNames = new string[audioZoneIndexes.Length];
            for (var index = 0; index < audioZoneIndexes.Length; index++)
            {
                var audioZoneIndex = audioZoneIndexes[index];
                zoneNames[index] = $"\"{audioZoneManager.zoneIdMapping[audioZoneIndex]}\"";
            }

            Debug.Log($"[AudioManager] Player: {owningPlayer.PrintName()}, Setting: {syncedAudioSettingIndex}, "
                + $"Zones ({zoneNames.Length}): {string.Join(", ", zoneNames)}");
        }
#endif

        public virtual void OnCheckForChangedAudioZones()
        {
            activeSettingZone = null;
        }

        public void NotifyAudioSettingCollider(AudioSettingCollider audioSettingCollider)
        {
            if (activeSettingZone == null
                || audioSettingCollider.priority < activeSettingZone.priority
                // Fallback to setting id purely for consistency, though that "consistency" may change between builds.
                // SettingIndexes are not guaranteed to be assigned in any specific order.
                || (audioSettingCollider.priority == activeSettingZone.priority
                    && audioSettingCollider.settingIndex < activeSettingZone.settingIndex))
            {
                activeSettingZone = audioSettingCollider;
            }
        }

        public abstract void NotifyHitAudioZoneCollider(AudioZoneCollider audioZoneCollider);

        /// <summary>
        /// <para>The core class only detects setting zone changes.</para>
        /// <para>Deriving classes must implement audio zone change detection.</para>
        /// </summary>
        /// <returns></returns>
        public virtual bool HaveZonesChanged()
        {
            bool hasSettingZoneChanged = activeSettingZone != oldActiveSettingZone;
            oldActiveSettingZone = activeSettingZone;
            return hasSettingZoneChanged;
        }

        /// <summary>
        /// </summary>
        /// <param name="audioSettingIndex"><c>-1</c> when not in any <see cref="AudioSettingCollider"/>.</param>
        protected abstract void PrepareForSerialization(int audioSettingIndex);

        private void PrepareForSerialization()
        {
            // Saving which setting zone the local player is in this "synced" variable too purely for cleanliness.
            // The local player does not actually care about which setting zones they themselves are in,
            // so this is not actually used locally.
            syncedAudioSettingIndex = oldActiveSettingZone == null ? -1 : oldActiveSettingZone.settingIndex;
            PrepareForSerialization(syncedAudioSettingIndex);
        }

        public override void OnDeserialization()
        {
            ApplySettingAndAudioZoneSetting();
        }

        public void ApplySettingAndAudioZoneSetting()
        {
            audioZoneManager.UpdateSettingZoneAudioSetting(this, syncedAudioSettingIndex);
            audioZoneManager.UpdateAudioZoneSetting(this);
            didGetAppliedOnce = true;
        }

        public void OnZonesChanged()
        {
            PrepareForSerialization();
            RequestSerializationRespectingCongestion();
            audioZoneManager.UpdateAudioZoneSetting(this);
        }

        private void RequestSerializationRespectingCongestion()
        {
            if (requestSerializationIsQueued) return;
            // Networking.IsClogged only becomes true once Suffering is greater (or equals?) 100, which is silly.
            float suffering = Stats.Suffering;
            if (suffering == 0f)
            {
                RequestSerialization();
                return;
            }
            requestSerializationIsQueued = true;
            // Exponential backoff, but only up to 16 seconds. Which it reaches at Suffering 45.
            // And a minimum of 1 second since once Suffering is non zero there's already problems.
            suffering = 1f + suffering / 15f;
            float delay = Mathf.Min(suffering * suffering, 16f);
            SendCustomEventDelayedSeconds(nameof(RequestSerializationDelayed), delay);
        }

        public void RequestSerializationDelayed()
        {
            requestSerializationIsQueued = false;
            RequestSerialization();
        }
    }
}
