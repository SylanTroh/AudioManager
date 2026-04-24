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

        public abstract bool SharesAudioZoneWith(AudioZoneSyncCore other);

        private void Start()
        {
            AudioZonePlayerObject = transform.parent.GetComponent<AudioZonePlayerObject>();
            AudioZoneManager = AudioZonePlayerObject.AudioZoneManager;
            if (AudioZoneManager == null)
            {
                Debug.Log($"{nameof(AudioZoneSyncCore)} has no {nameof(AudioZoneManager)}.");
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

        protected void LogAudioZones(int[] audioZoneIndexes)
        {
            var zoneNames = new string[audioZoneIndexes.Length];
            for (var index = 0; index < audioZoneIndexes.Length; index++)
            {
                var audioZoneIndex = audioZoneIndexes[index];
                zoneNames[index] = AudioZoneManager.ZoneIdMapping[audioZoneIndex];
            }

            Debug.Log($"Player {OwningPlayer.PrintName()} entered Zones: '{string.Join("', '", zoneNames)}'");
        }

        public abstract void OnValidateAudioZonesStart();

        public abstract void NotifyAudioSettingCollider(AudioSettingCollider audioSettingCollider);

        public abstract void NotifyHitAudioZoneCollider(AudioZoneCollider audioZoneCollider);

        public override void OnDeserialization()
        {
            AudioZoneManager.UpdateAudioZoneSetting(this);
        }

        public override void OnPreSerialization() { } // TODO remove, just here for testing

        public void OnZoneChanged()
        {
            OnPreSerialization(); // TODO remove, just here for testing
            RequestSerialization();
            AudioZoneManager.UpdateAudioZoneSetting(this);
        }

        public abstract bool HasZoneChanged();
    }
}
