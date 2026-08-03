using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AudioZoneManagerKillSwitch : UdonSharpBehaviour
    {
        [HideInInspector, SerializeField] private AudioZoneManager audioZoneManager;
        public const string AudioZoneManagerPropertyName = nameof(audioZoneManager);
        [HideInInspector, SerializeField] private AudioSettingManager audioSettingManager;
        public const string AudioSettingManagerPropertyName = nameof(audioSettingManager);

        [Tooltip("When using a different script to sync the on off state of the kill switch, enable this. "
            + "For example when using a UI Toggle Sync script.")]
        [SerializeField] private bool syncingIsHandledExternally = false;

        [UdonSynced] private bool killed;
        public bool Killed
        {
            get => killed;
            set
            {
                if (killed == value) return;
                killed = value;
                if (!syncingIsHandledExternally)
                {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                    RequestSerialization();
                }
                ApplyToManager();
            }
        }

        public void EnableKillSwitch()
        {
            Killed = true;
        }

        public void DisableKillSwitch()
        {
            Killed = false;
        }

        public void ToggleKillSwitch()
        {
            Killed = !Killed;
        }

        public override void OnDeserialization()
        {
            if (syncingIsHandledExternally) return; // Other scripts on the same object may RequestSerialization.
            ApplyToManager();
        }

        private void ApplyToManager()
        {
            if (!Killed)
            {
                audioZoneManager.Revive();
                return;
            }
            audioZoneManager.Kill();

            VRCPlayerApi[] players = VRCPlayerApi.GetPlayers();
            foreach (VRCPlayerApi player in players)
            {
                audioSettingManager.RemoveAudioSetting(player, AudioZoneManager.SETTING_ZONE_SETTING_ID);
                audioSettingManager.RemoveAudioSetting(player, AudioZoneManager.AUDIO_ZONE_SETTING_ID);
                audioSettingManager.ApplyAudioSetting(player);
            }
        }
    }
}
