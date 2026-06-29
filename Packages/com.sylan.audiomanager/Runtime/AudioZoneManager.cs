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
        [HideInInspector] public string[] zoneIdMapping = Array.Empty<string>();
        [HideInInspector] public int defaultLayerIndex = 0;

        // ================================================================
        // Audio Setting Zones
        // ================================================================

        /// <summary>
        /// <para>This is used as an offset to distinguish between audio zone and setting zone indexes when
        /// using using <see cref="AudioZoneSyncArrayCore"/>.</para>
        /// <para>Setting zone indexes get "shifted" up by this value for syncing purposes. In other words the
        /// id range from <c>0</c> (inclusive) to <see cref="totalAudioZonesCount"/> (exclusive) is used for
        /// <see cref="AudioZoneCollider"/>s, the id range starting at <see cref="totalAudioZonesCount"/>
        /// (inclusive) is used for <see cref="AudioSettingCollider"/>s.</para>
        /// </summary>
        [HideInInspector] public int totalAudioZonesCount;
        /// <summary>
        /// <para>When using <see cref="BitFieldAudioZoneSync"/> this defines how many of the lower bits of
        /// the <see cref="BitFieldAudioZoneSync.highestSyncedAudioZonesField"/> are used for audio zones. The
        /// remaining higher bits are used as an id/index for a setting zone.</para>
        /// </summary>
        [HideInInspector] public int audioSettingsIndexBitShift;
        /// <summary>
        /// <para>When using <see cref="BitFieldAudioZoneSync"/> this is the mask for all the bits in the
        /// <see cref="BitFieldAudioZoneSync.highestSyncedAudioZonesField"/> which are used as an id/index for
        /// a setting zone.</para>
        /// </summary>
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
        /// <summary>
        /// <para>List of <see cref="AudioZoneSyncCore"/>.</para>
        /// </summary>
        private readonly DataList RemotePlayerSyncs = new DataList();

        private bool killed;

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

        public void UpdateSettingZoneAudioSetting(AudioZoneSyncCore playerObjectSync, int settingIndex)
        {
            if (killed) return;
            if (LocalPlayerSync == null) return; // The order in which player objects get created is undefined behavior.
            if (!Utilities.IsValid(playerObjectSync.OwningPlayer)) return; // Major trust issues.
            if (LocalPlayerSync == playerObjectSync) return;

            if (settingIndex == -1)
            {
#if AUDIO_MANAGER_DEBUG
                Debug.Log("[AudioManager] No Setting Zone for " + playerObjectSync.OwningPlayer.PrintName() + ".");
#endif
                if (_AudioSettingManager.RemoveAudioSetting(playerObjectSync.OwningPlayer, SETTING_ZONE_SETTING_ID))
                {
                    _AudioSettingManager.ApplyAudioSetting(playerObjectSync.OwningPlayer);
                }
            }
            else
            {
                int priority = allAudioSettingsPriority[settingIndex];
                DataList setting = allAudioSettings[settingIndex];
#if AUDIO_MANAGER_DEBUG
                Debug.Log("[AudioManager] Using Setting Zone " + settingIndex + " for " + playerObjectSync.OwningPlayer.PrintName() + ".");
#endif
                _AudioSettingManager.RemoveAudioSetting(playerObjectSync.OwningPlayer, SETTING_ZONE_SETTING_ID);
                _AudioSettingManager.AddAudioSetting(playerObjectSync.OwningPlayer, SETTING_ZONE_SETTING_ID, priority, setting);
                _AudioSettingManager.ApplyAudioSetting(playerObjectSync.OwningPlayer);
            }
        }

        // ================================================================
        // Update Audio Zone Audio Settings
        // ================================================================

        public void UpdateAudioZoneSetting(AudioZoneSyncCore playerObjectSync)
        {
            if (killed) return;
            if (LocalPlayerSync == null) return; // The order in which player objects get created is undefined behavior.
            if (!Utilities.IsValid(playerObjectSync.OwningPlayer)) return; // Major trust issues.
            if (LocalPlayerSync != playerObjectSync)
            {
                //If someone else caused the update, update triggering player
                ApplyAudioZoneSetting(playerObjectSync);
            }
            else
            {
                //If the local player caused the update, update all players
                for (var i = 0; i < RemotePlayerSyncs.Count; i++)
                {
                    var remotePlayerSync = (AudioZoneSyncCore)RemotePlayerSyncs[i].Reference;
                    // Deletion order of remote VRCPlayerApis and their player objects is undefined behavior.
                    if (!Utilities.IsValid(remotePlayerSync.OwningPlayer)) continue;
                    ApplyAudioZoneSetting(remotePlayerSync);
                }
            }
        }

        private void ApplyAudioZoneSetting(AudioZoneSyncCore remotePlayerObjectSync)
        {
            if (LocalPlayerSync.SharesAudioZoneWith(remotePlayerObjectSync))
            {
#if AUDIO_MANAGER_DEBUG
                Debug.Log("[AudioManager] Shares AudioZone with " + remotePlayerObjectSync.OwningPlayer.PrintName() + ".");
#endif
                if (_AudioSettingManager.RemoveAudioSetting(remotePlayerObjectSync.OwningPlayer, AUDIO_ZONE_SETTING_ID))
                {
                    _AudioSettingManager.ApplyAudioSetting(remotePlayerObjectSync.OwningPlayer);
                }
            }
            else
            {
#if AUDIO_MANAGER_DEBUG
                Debug.Log("[AudioManager] Does not share AudioZone with " + remotePlayerObjectSync.OwningPlayer.PrintName() + ".");
#endif
                if (_AudioSettingManager.AddAudioSetting(remotePlayerObjectSync.OwningPlayer, AUDIO_ZONE_SETTING_ID, audioZonePriority, AudioZoneAudioSettings))
                {
                    _AudioSettingManager.ApplyAudioSetting(remotePlayerObjectSync.OwningPlayer);
                }
            }
        }

        public void Register(AudioZoneSyncCore playerObjectSync)
        {
            if (Networking.GetOwner(playerObjectSync.gameObject).isLocal)
            {
                LocalPlayerSync = playerObjectSync;
                // Apply any or all zone settings which may have been ignored previously
                // due to LocalPlayerSync being null at the time since the
                // creation order of player objects and each other's deserialization is undefined behavior.
                ApplyAllRemotePlayerSettingAndAudioZoneSetting();
            }
            else
            {
                RemotePlayerSyncs.Add(playerObjectSync);
            }
        }

        private void ApplyAllRemotePlayerSettingAndAudioZoneSetting()
        {
            for (int i = 0; i < RemotePlayerSyncs.Count; i++)
            {
                var remotePlayerSync = (AudioZoneSyncCore)RemotePlayerSyncs[i].Reference;
                if (!Utilities.IsValid(remotePlayerSync.OwningPlayer)) continue; // Major trust issues.
                remotePlayerSync.ApplySettingAndAudioZoneSetting();
            }
        }

        public void Deregister(AudioZoneSyncCore playerObjectSync)
        {
            RemotePlayerSyncs.Remove(playerObjectSync);
        }

        // ================================================================
        // Kill Switch
        // ================================================================

        public void Kill()
        {
            killed = true;
        }

        public void Revive()
        {
            killed = false;
            ApplyAllRemotePlayerSettingAndAudioZoneSetting();
        }
    }
}
