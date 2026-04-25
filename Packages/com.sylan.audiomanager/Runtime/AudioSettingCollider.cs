using UdonSharp;
using UnityEngine;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioSettingCollider : UdonSharpBehaviour
    {
        [HideInInspector] public int SettingIndex;

        [Header("Lower number means higher priority", order = 0)]
        [Space(-10, order = 1)]
        [Header("Audiozones have priority 1000", order = 2)]
        public int priority = AudioSettingManager.DEFAULT_PRIORITY;

        public float voiceGain = AudioSettingManager.DEFAULT_VOICE_GAIN;
        public float voiceNear = AudioSettingManager.DEFAULT_VOICE_RANGE_NEAR;
        public float voiceFar = AudioSettingManager.DEFAULT_VOICE_RANGE_FAR;
        public float volumetricRadius = AudioSettingManager.DEFAULT_VOICE_VOLUMETRIC_RADIUS;
        public bool lowpassFilter = AudioSettingManager.DEFAULT_VOICE_LOWPASS;

        [Header("Voice Fade Settings")]
        [Tooltip("Enable smooth fading when entering/exiting this audio setting zone")]
        public bool enableFade = false;
        [Tooltip("Duration of fade in seconds")]
        public float fadeDuration = 1.0f;
    }
}
