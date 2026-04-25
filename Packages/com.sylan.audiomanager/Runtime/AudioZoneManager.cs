using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioZoneManager : UdonSharpBehaviour
    {
        // ================================================================
        // References
        // ================================================================

        public AudioSettingManager AudioSettingManager { get => _AudioSettingManager; private set { _AudioSettingManager = value; } }
        [HideInInspector, SerializeField] private AudioSettingManager _AudioSettingManager;
        public const string AudioSettingManagerPropertyName = nameof(_AudioSettingManager);

        /// <summary>
        /// <para>Must be 0 as it must sort first, which is what <see cref="IntegerAudioZoneSync"/> relies on.</para>
        /// </summary>
        public const int EmptyZoneIdIndex = 0;
        public const ulong EmptyZoneIdBitFlag = 1uL << EmptyZoneIdIndex;
        [HideInInspector] public string[] ZoneIdMapping = Array.Empty<string>();

        // ================================================================
        // Audio Setting Zones
        // ================================================================

        // Yes audio zones, not setting zones. This is used as an offset to distinguish between the two.
        [HideInInspector] public int totalAudioZonesCount;
        [HideInInspector] public int audioSettingsIndexBitShift;
        [HideInInspector] public ulong audioSettingsIndexBitMask;

        [HideInInspector] public int[] allAudioSettingsPriority;
        [NonSerialized] public DataList[] allAudioSettings;
        public const string SETTING_ZONE_SETTING_ID = "SETTINGZONEVOICESETTING";

        // All these are purely used on Start to populate the allAudioSettings array.
        [HideInInspector] public float[] allAudioSettingsVoiceGain;
        [HideInInspector] public float[] allAudioSettingsVoiceNear;
        [HideInInspector] public float[] allAudioSettingsVoiceFar;
        [HideInInspector] public float[] allAudioSettingsVolumetricRadius;
        [HideInInspector] public bool[] allAudioSettingsLowpassFilter;
        [HideInInspector] public bool[] allAudioSettingsEnableFade;
        [HideInInspector] public float[] allAudioSettingsFadeDuration;

        // ================================================================
        // Audio Zone Configuration
        // ================================================================

        [Header("Set AudioSetting when in different audiozones")]
        [SerializeField] private float voiceGain = 7.0f;
        [SerializeField] private float voiceRangeNear = AudioSettingManager.DEFAULT_VOICE_RANGE_NEAR;
        [SerializeField] private float voiceRangeFar = 1.75f;
        [SerializeField] private float volumetricRadius = AudioSettingManager.DEFAULT_VOICE_VOLUMETRIC_RADIUS;
        [SerializeField] private bool voiceLowpass = AudioSettingManager.DEFAULT_VOICE_LOWPASS;

        [Header("Voice Fade Settings")]
        [Tooltip("Enable smooth fading when entering/exiting audio zones")]
        [SerializeField] private bool enableAudioZoneFade = true;
        [Tooltip("Duration of fade in seconds")]
        [SerializeField] private float audioZoneFadeDuration = 1.0f;

        [Header("Lower number means higher priority", order = 0)]
        [Space(-10, order = 1)]
        [Header("Audiozones have priority 1000 be default", order = 2)]
        public int audioZonePriority = 1000;

        // ================================================================
        // Data Structures
        // ================================================================

        public const string AUDIO_ZONE_SETTING_ID = "AUDIOZONEVOICESETTING";
        DataList AudioZoneAudioSettings = new DataList()
        {
            (DataToken)7.0f, //Voice Gain
            (DataToken)0.0f, //Voice Range Near
            (DataToken)2.0f, //Voice Range Far
            (DataToken)AudioSettingManager.DEFAULT_VOICE_VOLUMETRIC_RADIUS,
            (DataToken)AudioSettingManager.DEFAULT_VOICE_LOWPASS,
            (DataToken)false, //Fade Enabled
            (DataToken)1.0f   //Fade Duration
        };

        private AudioZoneSyncCore LocalPlayerSync;
        private readonly DataList RemotePlayerSyncs = new DataList();

        private void Start()
        {
            AudioZoneAudioSettings[AudioSettingManager.VOICE_GAIN_INDEX] = (DataToken)voiceGain;
            AudioZoneAudioSettings[AudioSettingManager.RANGE_NEAR_INDEX] = (DataToken)voiceRangeNear;
            AudioZoneAudioSettings[AudioSettingManager.RANGE_FAR_INDEX] = (DataToken)voiceRangeFar;
            AudioZoneAudioSettings[AudioSettingManager.VOLUMETRIC_RADIUS_INDEX] = (DataToken)volumetricRadius;
            AudioZoneAudioSettings[AudioSettingManager.VOICE_LOWPASS_INDEX] = (DataToken)voiceLowpass;
            AudioZoneAudioSettings[AudioSettingManager.FADE_ENABLED_INDEX] = (DataToken)enableAudioZoneFade;
            AudioZoneAudioSettings[AudioSettingManager.FADE_DURATION_INDEX] = (DataToken)audioZoneFadeDuration;
            BuildAllAudioZoneSettings();
        }

        private void BuildAllAudioZoneSettings()
        {
            int count = allAudioSettingsPriority.Length;
            allAudioSettings = new DataList[count];
            for (int i = 0; i < count; i++)
            {
                allAudioSettings[i] = new DataList(new DataToken[]
                {
                    allAudioSettingsVoiceGain[i],
                    allAudioSettingsVoiceNear[i],
                    allAudioSettingsVoiceFar[i],
                    allAudioSettingsVolumetricRadius[i],
                    allAudioSettingsLowpassFilter[i],
                    allAudioSettingsEnableFade[i],
                    allAudioSettingsFadeDuration[i],
                });
            }
            allAudioSettingsVoiceGain = null;
            allAudioSettingsVoiceNear = null;
            allAudioSettingsVoiceFar = null;
            allAudioSettingsVolumetricRadius = null;
            allAudioSettingsLowpassFilter = null;
            allAudioSettingsEnableFade = null;
            allAudioSettingsFadeDuration = null;
        }

        // ================================================================
        // Update Setting Zone Audio Settings
        // ================================================================

        public void UpdateSettingZoneAudioSetting(AudioZoneSyncCore playerObjectSync, int settingIndex, bool doApply)
        {
            if (LocalPlayerSync == playerObjectSync) return;

            if (settingIndex == -1)
            {
                _AudioSettingManager.RemoveAudioSetting(playerObjectSync.OwningPlayer, SETTING_ZONE_SETTING_ID);
            }
            else
            {
                int actualIndex = settingIndex + 1;
                int priority = allAudioSettingsPriority[actualIndex];
                DataList setting = allAudioSettings[actualIndex];
                _AudioSettingManager.AddAudioSetting(playerObjectSync.OwningPlayer, SETTING_ZONE_SETTING_ID, priority, setting);
            }

            if (doApply)
            {
                _AudioSettingManager.ApplyAudioSetting(playerObjectSync.OwningPlayer);
            }
        }

        // ================================================================
        // Update Audio Zone Audio Settings
        // ================================================================

        public void UpdateAudioZoneSetting(AudioZoneSyncCore playerObjectSync, bool doApply)
        {
            if (LocalPlayerSync != playerObjectSync)
            {
                //If someone else caused the update, update triggering player
                ApplyAudioZoneSetting(playerObjectSync, doApply);
            }
            else
            {
                //If the local player caused the update, update all players
                for (var i = 0; i < RemotePlayerSyncs.Count; i++)
                {
                    var remotePlayerSync = (AudioZoneSyncCore)RemotePlayerSyncs[i].Reference;
                    ApplyAudioZoneSetting(remotePlayerSync, doApply);
                }
            }
        }

        private void ApplyAudioZoneSetting(AudioZoneSyncCore remotePlayerObjectSync, bool doApply)
        {
            if (LocalPlayerSync.SharesAudioZoneWith(remotePlayerObjectSync))
            {
#if AUDIO_MANAGER_DEBUG
                Debug.Log("[AudioManager] Shares AudioZone with" + remotePlayerObjectSync.OwningPlayer.displayName + ".");
#endif
                _AudioSettingManager.RemoveAudioSetting(remotePlayerObjectSync.OwningPlayer, AUDIO_ZONE_SETTING_ID);
            }
            else
            {
#if AUDIO_MANAGER_DEBUG
                Debug.Log("[AudioManager] Does not share AudioZone with " + remotePlayerObjectSync.OwningPlayer.displayName + ".");
#endif
                _AudioSettingManager.AddAudioSetting(remotePlayerObjectSync.OwningPlayer, AUDIO_ZONE_SETTING_ID, audioZonePriority, AudioZoneAudioSettings);
            }

            if (doApply)
            {
                _AudioSettingManager.ApplyAudioSetting(remotePlayerObjectSync.OwningPlayer);
            }
        }

        public void Register(AudioZoneSyncCore playerObjectSync)
        {
            if (Networking.GetOwner(playerObjectSync.gameObject).isLocal)
            {
                LocalPlayerSync = playerObjectSync;
            }
            else
            {
                RemotePlayerSyncs.Add(playerObjectSync);
            }
        }

        public void Deregister(AudioZoneSyncCore playerObjectSync)
        {
            RemotePlayerSyncs.Remove(playerObjectSync);
        }
    }
}
