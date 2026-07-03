using UdonSharp;
using UnityEngine;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioSettingCollider : UdonSharpBehaviour
    {
        /// <summary>
        /// <para>Used by editor scripting for migration purposes.</para>
        /// <para>Components from before this field existed will have an initial value of <c>0u</c>.</para>
        /// </summary>
        [HideInInspector] public uint scriptVersion;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public const uint CurrentScriptVersion = 1u;
        private void Reset() => scriptVersion = CurrentScriptVersion; // Runs when the component gets created or reset.
#endif

        /// <summary>
        /// <para>Generated at build time. <see cref="AudioSettingCollider"/>s with perfectly matching
        /// settings use the same <see cref="settingIndex"/>.</para>
        /// </summary>
        [HideInInspector] public int settingIndex;

        [Header("Lower number means higher priority", order = 0)]
        [Space(-10, order = 1)]
        [Header("Audiozones have priority 1000", order = 2)]
        /// <summary>
        /// <para>Changing this value at runtime will not have any effect.</para>
        /// </summary>
        public int priority = AudioSettingManager.DEFAULT_PRIORITY;

        /// <inheritdoc cref="priority"/>
        public float voiceGain = AudioSettingManager.DEFAULT_VOICE_GAIN;
        /// <inheritdoc cref="priority"/>
        public float voiceNear = AudioSettingManager.DEFAULT_VOICE_RANGE_NEAR;
        /// <inheritdoc cref="priority"/>
        public float voiceFar = AudioSettingManager.DEFAULT_VOICE_RANGE_FAR;
        /// <inheritdoc cref="priority"/>
        public float volumetricRadius = AudioSettingManager.DEFAULT_VOICE_VOLUMETRIC_RADIUS;
        /// <inheritdoc cref="priority"/>
        public bool lowpassFilter = AudioSettingManager.DEFAULT_VOICE_LOWPASS;

        [Header("Voice Fade Settings")]
        [Tooltip("Enable smooth fading when entering/exiting this audio setting zone")]
        /// <inheritdoc cref="priority"/>
        public bool enableFade = false;
        [Tooltip("Duration of fade in seconds")]
        /// <inheritdoc cref="priority"/>
        public float fadeDuration = 1.0f;
    }
}
