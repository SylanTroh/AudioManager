
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Sylan.AudioManager
{
    public class AudioZone : UdonSharpBehaviour
    {
        const int DEFAULT_ZONE_PRIORITY = 128;
        const int NULL_ZONE_PRIORITY = 255;

        public int priority = DEFAULT_ZONE_PRIORITY;

        public float voiceGain = AudioManager.DEFAULT_VOICE_GAIN;
        public float voiceNear = AudioManager.DEFAULT_VOICE_RANGE_NEAR;
        public float voiceFar = AudioManager.DEFAULT_VOICE_RANGE_FAR;
        public float voiceVolumetricRadius = AudioManager.DEFAULT_VOICE_VOLUMETRIC_RADIUS;
        public bool voiceLowpass = AudioManager.DEFAULT_VOICE_LOWPASS;

        void Start()
        {

        }
    }
}
