using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    /// <summary>
    /// VoiceApplicator - Applies of voice settings with support for fading between settings
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VoiceApplicator : UdonSharpBehaviour
    {
        
        // Fade data indices
        private const int FADE_START_TIME_INDEX = 0;
        private const int FADE_DURATION_INDEX = 1;
        private const int FADE_FROM_GAIN_INDEX = 2;
        private const int FADE_FROM_NEAR_INDEX = 3;
        private const int FADE_FROM_FAR_INDEX = 4;
        private const int FADE_FROM_VOLUMETRIC_INDEX = 5;
        private const int FADE_FROM_LOWPASS_INDEX = 6;  // Store as float rather than bool
        private const int FADE_TO_GAIN_INDEX = 7;
        private const int FADE_TO_NEAR_INDEX = 8;
        private const int FADE_TO_FAR_INDEX = 9;
        private const int FADE_TO_VOLUMETRIC_INDEX = 10;
        private const int FADE_TO_LOWPASS_INDEX = 11;  // Store as float rather than bool
        
        // Current voice setting per player
        // We need this because VRCPlayerApi doesn't provide get methods for voice settings.
        private DataDictionary _playerAudioSetting = new DataDictionary();

        // Active voice fades that need iterating through
        private DataDictionary _activePlayerFade = new DataDictionary();
        
        // =============================================================================
        // Public Methods
        // =============================================================================
        
        /// <summary>
        /// Apply voice settings to a player, with optional fade
        /// Supports old AudioSetting format for backwards compatibility
        /// </summary>
        /// <param name="player">Target player (must not be local player)</param>
        /// <param name="audioSetting">Audio setting DataList</param>
        public void ApplyVoiceSetting(VRCPlayerApi player, DataList audioSetting)
        {
            if (!Utilities.IsValid(player)) return;
            if (player == Networking.LocalPlayer) return;
            
            if (audioSetting == null || audioSetting.Count < 5)
            {
                Debug.LogError("[AudioManager] VoiceApplicator: Invalid audioSetting - must have at least 5 elements");
                return;
            }
            
            DataList voiceParams = ExtractVoiceParams(audioSetting);
            
            // Default to instant change
            bool fadeEnabled = false;
            float fadeDuration = 0.0f;
            
            if (audioSetting.Count >= 7)
            {
                if (audioSetting.TryGetValue(AudioSettingManager.FADE_ENABLED_INDEX, TokenType.Boolean, out DataToken fadeToken) &&
                    audioSetting.TryGetValue(AudioSettingManager.FADE_DURATION_INDEX, TokenType.Float, out DataToken durationToken))
                {
                    fadeEnabled = fadeToken.Boolean;
                    fadeDuration = durationToken.Float;
                }
            }
            
            if (fadeEnabled && fadeDuration > 0.0f)
            {
                DataList currentSettings = GetCurrentSettings(player);
                
                if (currentSettings == null)
                {
                    // If there is no previous value, apply settings instantly
                    ApplyImmediate(player, voiceParams);
                    StoreCurrentSettings(player, voiceParams);
                }
                else
                {
                    StartFade(player, currentSettings, voiceParams, fadeDuration);
                }
            }
            else
            {
                ApplyImmediate(player, voiceParams);
                StoreCurrentSettings(player, voiceParams);
                CancelFade(player);
            }
        }
        
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            int playerId = player.playerId;
            
            // Remove current settings tracking
            if (_playerAudioSetting.ContainsKey((DataToken)playerId))
            {
                _playerAudioSetting.Remove((DataToken)playerId);
                Debug.Log("[AudioManager] VoiceApplicator: " +  player.PrintName() + " left, removing current audio settings" + player.PrintName());
            }
            
            // Remove active fade if present
            if (_activePlayerFade.ContainsKey((DataToken)playerId))
            {
                _activePlayerFade.Remove((DataToken)playerId);
                Debug.Log("[AudioManager] VoiceApplicator: " +  player.PrintName() + " left, cancelled fades for" );
            }
        }
        
        // =============================================================================
        // Private METHODS
        // =============================================================================
        
        /// <summary>
        /// Apply voice settings immediately without fade
        /// </summary>
        /// <param name="player">Target player</param>
        /// <param name="voiceParams">DataList[5] - [gain, near, far, volumetric, lowpass as float]</param>
        private void ApplyImmediate(VRCPlayerApi player, DataList voiceParams)
        {
            if (!Utilities.IsValid(player)) return;
            if (voiceParams == null || voiceParams.Count < 5) return;
            
            float gain = voiceParams[AudioSettingManager.VOICE_GAIN_INDEX].Float;
            float near = voiceParams[AudioSettingManager.RANGE_NEAR_INDEX].Float;
            float far = voiceParams[AudioSettingManager.RANGE_FAR_INDEX].Float;
            float volumetric = voiceParams[AudioSettingManager.VOLUMETRIC_RADIUS_INDEX].Float;
            bool lowpass = voiceParams[AudioSettingManager.VOICE_LOWPASS_INDEX].Float > 0.5f;
            
            player.SetVoiceGain(gain);
            player.SetVoiceDistanceNear(near);
            player.SetVoiceDistanceFar(far);
            player.SetVoiceVolumetricRadius(volumetric);
            player.SetVoiceLowpass(lowpass);
            
            Debug.Log("[AudioManager] VoiceApplicator: Applied voice settings to " + player.PrintName() + 
                      " (gain:" + gain + ", near:" + near + ", far:" + far + ", volumetric:" + volumetric + ", lowpass:" + lowpass + ")");
        }
        
        /// <summary>
        /// Start a fade from current settings to target settings
        /// </summary>
        /// <param name="player">Target player</param>
        /// <param name="fromSettings">Source voice settings DataList[5]</param>
        /// <param name="toSettings">Target voice settings DataList[5]</param>
        /// <param name="duration">Fade duration in seconds</param>
        private void StartFade(VRCPlayerApi player, DataList fromSettings, DataList toSettings, float duration)
        {
            if (!Utilities.IsValid(player)) return;
            if (fromSettings == null || toSettings == null) return;
            if (fromSettings.Count < 5 || toSettings.Count < 5) return;
            
            int playerId = player.playerId;
            
            // Create fade data datalist
            DataList fadeData = new DataList();
            fadeData.Add((DataToken)Time.realtimeSinceStartup);  // startTime
            fadeData.Add((DataToken)duration);   // duration
            
            // From settings
            fadeData.Add((DataToken)fromSettings[AudioSettingManager.VOICE_GAIN_INDEX].Float);
            fadeData.Add((DataToken)fromSettings[AudioSettingManager.RANGE_NEAR_INDEX].Float);
            fadeData.Add((DataToken)fromSettings[AudioSettingManager.RANGE_FAR_INDEX].Float);
            fadeData.Add((DataToken)fromSettings[AudioSettingManager.VOLUMETRIC_RADIUS_INDEX].Float);
            fadeData.Add((DataToken)fromSettings[AudioSettingManager.VOICE_LOWPASS_INDEX].Float);
            
            // To settings
            fadeData.Add((DataToken)toSettings[AudioSettingManager.VOICE_GAIN_INDEX].Float);
            fadeData.Add((DataToken)toSettings[AudioSettingManager.RANGE_NEAR_INDEX].Float);
            fadeData.Add((DataToken)toSettings[AudioSettingManager.RANGE_FAR_INDEX].Float);
            fadeData.Add((DataToken)toSettings[AudioSettingManager.VOLUMETRIC_RADIUS_INDEX].Float);
            fadeData.Add((DataToken)toSettings[AudioSettingManager.VOICE_LOWPASS_INDEX].Float);
            
            _activePlayerFade.SetValue((DataToken)playerId, (DataToken)fadeData);
            
            Debug.Log("[AudioManager] VoiceApplicator: Started fade for " + player.PrintName() +
                      " (duration:" + duration + "s, from gain:" + fromSettings[AudioSettingManager.VOICE_GAIN_INDEX].Float +
                      " to gain:" + toSettings[AudioSettingManager.VOICE_GAIN_INDEX].Float + ")");
        }
        
        /// <summary>
        /// Cancel any active fade for a player
        /// </summary>
        /// <param name="player">Target player</param>
        private void CancelFade(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            
            int playerId = player.playerId;
            
            if (_activePlayerFade.ContainsKey((DataToken)playerId))
            {
                _activePlayerFade.Remove((DataToken)playerId);
                Debug.Log("[AudioManager] VoiceApplicator: Cancelled fade for " + player.PrintName());
            }
        }
        
        private void Update()
        {
            if (_activePlayerFade.Count == 0) return;
            
            float currentTime = Time.realtimeSinceStartup;
            DataList playersToRemove = new DataList();
            
            DataList keys = _activePlayerFade.GetKeys();
            for (int i = 0; i < keys.Count; i++)
            {
                int playerId = keys[i].Int;
                
                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerId);
                if (!Utilities.IsValid(player))
                {
                    playersToRemove.Add((DataToken)playerId);
                    continue;
                }
                
                if (!_activePlayerFade.TryGetValue((DataToken)playerId, TokenType.DataList, out DataToken fadeDataToken))
                {
                    continue;
                }
                
                DataList fadeData = fadeDataToken.DataList;
                
                float startTime = fadeData[FADE_START_TIME_INDEX].Float;
                float duration = fadeData[FADE_DURATION_INDEX].Float;
                float elapsed = currentTime - startTime;
                float t = Mathf.Clamp01(elapsed / duration);
                
                float gain = Mathf.Lerp(
                    fadeData[FADE_FROM_GAIN_INDEX].Float,
                    fadeData[FADE_TO_GAIN_INDEX].Float,
                    t
                );
                float near = Mathf.Lerp(
                    fadeData[FADE_FROM_NEAR_INDEX].Float,
                    fadeData[FADE_TO_NEAR_INDEX].Float,
                    t
                );
                float far = Mathf.Lerp(
                    fadeData[FADE_FROM_FAR_INDEX].Float,
                    fadeData[FADE_TO_FAR_INDEX].Float,
                    t
                );
                float volumetric = Mathf.Lerp(
                    fadeData[FADE_FROM_VOLUMETRIC_INDEX].Float,
                    fadeData[FADE_TO_VOLUMETRIC_INDEX].Float,
                    t
                );
                
                float lowpassFloat = Mathf.Lerp(
                    fadeData[FADE_FROM_LOWPASS_INDEX].Float,
                    fadeData[FADE_TO_LOWPASS_INDEX].Float,
                    t
                );
                
                bool lowpass = lowpassFloat > 0.5f;
                
                player.SetVoiceGain(gain);
                player.SetVoiceDistanceNear(near);
                player.SetVoiceDistanceFar(far);
                player.SetVoiceVolumetricRadius(volumetric);
                player.SetVoiceLowpass(lowpass);
                
                if (t >= 1.0f)
                {
                    // If fade complete
                    playersToRemove.Add((DataToken)playerId);
                    
                    // Store final values as current settings
                    DataList finalSettings = new DataList();
                    finalSettings.Add((DataToken)fadeData[FADE_TO_GAIN_INDEX].Float);
                    finalSettings.Add((DataToken)fadeData[FADE_TO_NEAR_INDEX].Float);
                    finalSettings.Add((DataToken)fadeData[FADE_TO_FAR_INDEX].Float);
                    finalSettings.Add((DataToken)fadeData[FADE_TO_VOLUMETRIC_INDEX].Float);
                    finalSettings.Add((DataToken)fadeData[FADE_TO_LOWPASS_INDEX].Float);
                    
                    _playerAudioSetting.SetValue((DataToken)playerId, (DataToken)finalSettings);
                    
                    Debug.Log("[AudioManager] VoiceApplicator: Completed fade for " + player.PrintName());
                }
                else
                {
                    // Update current settings to interpolated values (for smooth interruption)
                    DataList currentSettings = new DataList();
                    currentSettings.Add((DataToken)gain);
                    currentSettings.Add((DataToken)near);
                    currentSettings.Add((DataToken)far);
                    currentSettings.Add((DataToken)volumetric);
                    currentSettings.Add((DataToken)(lowpass ? 1.0f : 0.0f));
                    
                    _playerAudioSetting.SetValue((DataToken)playerId, (DataToken)currentSettings);
                }
            }
            
            // Clean up completed fades
            for (int i = 0; i < playersToRemove.Count; i++)
            {
                _activePlayerFade.Remove(playersToRemove[i]);
            }
        }
        
        // =============================================================================
        // HELPER METHODS
        // =============================================================================
        
        /// <summary>
        /// Extract voice parameters from full audio setting
        /// </summary>
        /// <param name="audioSetting">Complete audio setting DataList</param>
        /// <returns>DataList[5] with voice params, or null if invalid</returns>
        private DataList ExtractVoiceParams(DataList audioSetting)
        {
            if (audioSetting == null || audioSetting.Count < 5) return null;
            
            DataList voiceParams = new DataList();
            voiceParams.Add((DataToken)audioSetting[AudioSettingManager.VOICE_GAIN_INDEX].Float);
            voiceParams.Add((DataToken)audioSetting[AudioSettingManager.RANGE_NEAR_INDEX].Float);
            voiceParams.Add((DataToken)audioSetting[AudioSettingManager.RANGE_FAR_INDEX].Float);
            voiceParams.Add((DataToken)audioSetting[AudioSettingManager.VOLUMETRIC_RADIUS_INDEX].Float);
            
            // Convert bool to float for consistent storage
            bool lowpass = audioSetting[AudioSettingManager.VOICE_LOWPASS_INDEX].Boolean;
            voiceParams.Add((DataToken)(lowpass ? 1.0f : 0.0f));
            
            return voiceParams;
        }
        
        /// <summary>
        /// Get currently active voice settings for a player
        /// </summary>
        /// <param name="player">Target player</param>
        /// <returns>DataList[5] with current settings, or null if not found</returns>
        private DataList GetCurrentSettings(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return null;
            
            int playerId = player.playerId;
            
            if (_playerAudioSetting.TryGetValue((DataToken)playerId, TokenType.DataList, out DataToken value))
            {
                return value.DataList;
            }
            
            return null;
        }
        
        /// <summary>
        /// Store current voice settings for a player
        /// </summary>
        /// <param name="player">Target player</param>
        /// <param name="voiceParams">DataList[5] with voice settings</param>
        private void StoreCurrentSettings(VRCPlayerApi player, DataList voiceParams)
        {
            if (!Utilities.IsValid(player)) return;
            if (voiceParams == null || voiceParams.Count < 5) return;
            
            int playerId = player.playerId;
            _playerAudioSetting.SetValue((DataToken)playerId, (DataToken)voiceParams);
        }
    }
    
    public static class AudioZoneManagerExtensions
    {
        // ================================================================
        // Extensions for VRCPlayerAPI
        // ================================================================
        public static string PrintName(this VRCPlayerApi player)
        {
            return player.displayName + "-" + player.playerId.ToString();
        }
    }
}