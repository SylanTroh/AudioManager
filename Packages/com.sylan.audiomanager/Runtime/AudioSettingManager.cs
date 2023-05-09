
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioSettingManager : UdonSharpBehaviour
    {
        public const float DEFAULT_VOICE_GAIN = 15.0f;
        public const float DEFAULT_VOICE_RANGE_NEAR = 0.0f;
        public const float DEFAULT_VOICE_RANGE_FAR = 15.0f;
        public const float DEFAULT_VOICE_VOLUMETRIC_RADIUS = 0.0f;
        public const bool DEFAULT_VOICE_LOWPASS = true;

        public AudioZoneManager AudioZoneManager { get => _AudioZoneManager; private set { _AudioZoneManager = value; } }
        [SerializeField] private AudioZoneManager _AudioZoneManager;
        public const string AudioZoneManagerPropertyName = nameof(_AudioZoneManager);

        public static void SetPlayerVoice(VRCPlayerApi player, float voiceGain, float voiceNear, float voiceFar, float voiceVolumetricRadius, bool voiceLowpass)
        {
            if (!player.IsValid()) return;

            player.SetVoiceGain(voiceGain);
            player.SetVoiceDistanceNear(voiceNear);
            player.SetVoiceDistanceFar(voiceFar);
            player.SetVoiceVolumetricRadius(voiceVolumetricRadius);
            player.SetVoiceLowpass(voiceLowpass);
        }
        public static void SetPlayerVoiceDefault(VRCPlayerApi player)
        {
            SetPlayerVoice(player, DEFAULT_VOICE_GAIN, DEFAULT_VOICE_RANGE_NEAR, DEFAULT_VOICE_RANGE_FAR, DEFAULT_VOICE_VOLUMETRIC_RADIUS, DEFAULT_VOICE_LOWPASS);
        }
        public static void SetPlayerVoiceOff(VRCPlayerApi player)
        {
            SetPlayerVoice(player, DEFAULT_VOICE_GAIN, DEFAULT_VOICE_RANGE_NEAR, 0, DEFAULT_VOICE_VOLUMETRIC_RADIUS, DEFAULT_VOICE_LOWPASS);
        }
        public void UpdateAudioSettings(VRCPlayerApi triggeringPlayer)
        {
            if (triggeringPlayer == null) return;
            if(!triggeringPlayer.IsValid()) return;

            if (triggeringPlayer != Networking.LocalPlayer)
            {
                //If someone else caused the update, update triggering player
                ApplyAudioZoneSetting(triggeringPlayer);
            }
            else
            {
                //If the local player caused the update, update all players
                VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
                VRCPlayerApi.GetPlayers(players);
                foreach (VRCPlayerApi player in players) ApplyAudioZoneSetting(player);
            }
        }
        public void ApplyAudioZoneSetting(VRCPlayerApi player)
        {
            if (!player.IsValid()) return;
            if (player == Networking.LocalPlayer) return;
            if (Networking.LocalPlayer.SharesZoneWith(player, AudioZoneManager))
            {
                Debug.Log("[AudioManager] Set " + player.displayName + "audio on.");
                SetPlayerVoiceDefault(player);
            }
            else
            {
                Debug.Log("[AudioManager] Set " + player.displayName + "audio off.");
                SetPlayerVoiceOff(player);
            }
        }
    }
}
