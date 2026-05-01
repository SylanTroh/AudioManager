using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class AudioZoneSyncCore : UdonSharpBehaviour
    {
        public VRCPlayerApi OwningPlayer;

        protected AudioZoneManager AudioZoneManager;
        protected AudioZonePlayerObject AudioZonePlayerObject;

        // TODO: remove SerializeField, just used for testing
        /// <summary>
        /// <para><c>-1</c> indicates not being in any setting zone.</para>
        /// <para>Not marked with <see cref="UdonSyncedAttribute"/>, deriving classes must sync this value.</para>
        /// </summary>
        [SerializeField] protected int syncedAudioSettingIndex;

        private AudioSettingCollider activeSettingZone;
        private AudioSettingCollider oldActiveSettingZone;

        public abstract bool SharesAudioZoneWith(AudioZoneSyncCore other);

        protected abstract string SyncScriptName { get; }

        private void Start()
        {
            Debug.Log($"[AudioManager] Using the {SyncScriptName} script for syncing audio and setting zones.");

            AudioZonePlayerObject = transform.parent.GetComponent<AudioZonePlayerObject>();
            AudioZoneManager = AudioZonePlayerObject.AudioZoneManager;
            if (AudioZoneManager == null)
            {
                Debug.Log($"[AudioManager] {nameof(AudioZoneSyncCore)} has no {nameof(AudioZoneManager)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            AudioZoneManager.Register(this);
            OwningPlayer = Networking.GetOwner(gameObject);
        }

        private void OnDestroy()
        {
            AudioZoneManager.Deregister(this);
        }

#if AUDIO_MANAGER_DEBUG
        protected void LogAudioZones(int[] audioZoneIndexes)
        {
            var zoneNames = new string[audioZoneIndexes.Length];
            for (var index = 0; index < audioZoneIndexes.Length; index++)
            {
                var audioZoneIndex = audioZoneIndexes[index];
                zoneNames[index] = $"\"{AudioZoneManager.ZoneIdMapping[audioZoneIndex]}\"";
            }

            Debug.Log($"[AudioManager] Player: {OwningPlayer.PrintName()}, Setting: {syncedAudioSettingIndex}, "
                + $"Zones ({zoneNames.Length}): {string.Join(", ", zoneNames)}");
        }
#endif

        public virtual void OnValidateAudioZonesStart()
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
                    && audioSettingCollider.SettingIndex < activeSettingZone.SettingIndex))
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
        public virtual bool HasZoneChanged()
        {
            bool hasSettingZoneChanged = activeSettingZone != oldActiveSettingZone;
            oldActiveSettingZone = activeSettingZone;
            return hasSettingZoneChanged;
        }

        /// <summary>
        /// </summary>
        /// <param name="audioSettingIndex"><c>-1</c> when not in any <see cref="AudioSettingCollider"/>.</param>
        protected abstract void InternalOnPreSerialization(int audioSettingIndex);

        public override void OnPreSerialization()
        {
            // Saving which setting zone the local player is in this "synced" variable too purely for cleanliness.
            // The local player does not actually care about which setting zones they themselves are in,
            // so this is not actually used locally.
            syncedAudioSettingIndex = oldActiveSettingZone == null ? -1 : oldActiveSettingZone.SettingIndex;
            InternalOnPreSerialization(syncedAudioSettingIndex);
        }

        public override void OnDeserialization()
        {
            ApplySettingAndAudioZoneSetting();
        }

        public void ApplySettingAndAudioZoneSetting()
        {
            AudioZoneManager.UpdateSettingZoneAudioSetting(this, syncedAudioSettingIndex, doApply: false);
            AudioZoneManager.UpdateAudioZoneSetting(this, doApply: true);
        }

        public void OnZoneChanged()
        {
            OnPreSerialization(); // TODO remove, just here for testing
            RequestSerialization();
            AudioZoneManager.UpdateAudioZoneSetting(this, doApply: true);
        }
    }
}
