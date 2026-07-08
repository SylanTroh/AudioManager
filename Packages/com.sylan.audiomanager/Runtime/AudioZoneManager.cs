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

        public AudioSettingManager AudioSettingManager => audioSettingManager;
        [HideInInspector, SerializeField] private AudioSettingManager audioSettingManager;
        public const string AudioSettingManagerPropertyName = nameof(audioSettingManager);

        // ================================================================
        // General Settings
        // ================================================================

        /// <summary>
        /// <para>Must be 0 as it must sort first, which is what <see cref="IntegerAudioZoneSync"/> relies on.</para>
        /// </summary>
        public const int EmptyZoneIdIndex = 0;
        public const ulong EmptyZoneIdBitFlag = 1uL << EmptyZoneIdIndex;
        [HideInInspector] public string[] zoneIdMapping = Array.Empty<string>();
        public int fallbackLayerIndex;

        /// <summary>
        /// <para>Used by editor scripting for migration purposes.</para>
        /// <para>Components from before this field existed will have an initial value of <c>0u</c>.</para>
        /// </summary>
        [HideInInspector] public uint scriptVersion;
#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public const uint CurrentScriptVersion = 2u;
        private void Reset() // Runs when the component gets created or reset.
        {
            scriptVersion = CurrentScriptVersion;

            // When changing these layers here, also update the tooltip for the custom inspector.
            fallbackLayerIndex = LayerMask.NameToLayer("Ignore Raycast");
            if (fallbackLayerIndex != -1) return;
            // If "Ignore Raycast" doesn't exist, "Environment" probably doesn't either.
            fallbackLayerIndex = LayerMask.NameToLayer("Environment");
            if (fallbackLayerIndex != -1) return;
            fallbackLayerIndex = 0;
        }
#endif

        [Header("General")]
        [Tooltip("The size of the sphere used to check for which zones a player is in.\n"
            + "The local player capsule has a radius of 0.2 for reference. "
            + "As a further note, that capsule's size is constant, regardless of player/avatar size.\n"
            + "0 is a good value, it allows setting up zones exactly matching the sizes of interiors/areas, "
            + "no larger, no smaller.")]
        [Range(0f, 1f)]
        [SerializeField] private float headCheckRadius = 0f;
        public float HeadCheckRadius => headCheckRadius;
        public const string HeadCheckRadiusPropertyName = nameof(headCheckRadius);

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
            if (!Utilities.IsValid(playerObjectSync.owningPlayer)) return; // Major trust issues.
            if (LocalPlayerSync == playerObjectSync) return;

            if (settingIndex == -1)
            {
#if SYLAN_AUDIOMANAGER_DEBUG
                Debug.Log("[AudioManager] No Setting Zone for " + playerObjectSync.owningPlayer.PrintName() + ".");
#endif
                if (audioSettingManager.RemoveAudioSetting(playerObjectSync.owningPlayer, SETTING_ZONE_SETTING_ID))
                {
                    audioSettingManager.ApplyAudioSetting(playerObjectSync.owningPlayer);
                }
            }
            else
            {
                int priority = allAudioSettingsPriority[settingIndex];
                DataList setting = allAudioSettings[settingIndex];
#if SYLAN_AUDIOMANAGER_DEBUG
                Debug.Log("[AudioManager] Using Setting Zone " + settingIndex + " for " + playerObjectSync.owningPlayer.PrintName() + ".");
#endif
                audioSettingManager.RemoveAudioSetting(playerObjectSync.owningPlayer, SETTING_ZONE_SETTING_ID);
                audioSettingManager.AddAudioSetting(playerObjectSync.owningPlayer, SETTING_ZONE_SETTING_ID, priority, setting);
                audioSettingManager.ApplyAudioSetting(playerObjectSync.owningPlayer);
            }
        }

        // ================================================================
        // Update Audio Zone Audio Settings
        // ================================================================

        public void UpdateAudioZoneSetting(AudioZoneSyncCore playerObjectSync)
        {
            if (killed) return;
            if (LocalPlayerSync == null) return; // The order in which player objects get created is undefined behavior.
            if (!Utilities.IsValid(playerObjectSync.owningPlayer)) return; // Major trust issues.
            if (LocalPlayerSync != playerObjectSync)
            {
                //If someone else caused the update, update triggering player
                ApplyAudioZoneSetting(playerObjectSync);
            }
            else
            {
                //If the local player caused the update, update all players
                for (int i = 0; i < RemotePlayerSyncs.Count; i++)
                {
                    AudioZoneSyncCore remotePlayerSync = (AudioZoneSyncCore)RemotePlayerSyncs[i].Reference;
                    // Deletion order of remote VRCPlayerApis and their player objects is undefined behavior.
                    if (!Utilities.IsValid(remotePlayerSync.owningPlayer)) continue;
                    ApplyAudioZoneSetting(remotePlayerSync);
                }
            }
        }

        private void ApplyAudioZoneSetting(AudioZoneSyncCore remotePlayerObjectSync)
        {
            if (LocalPlayerSync.SharesAudioZoneWith(remotePlayerObjectSync))
            {
#if SYLAN_AUDIOMANAGER_DEBUG
                Debug.Log("[AudioManager] Shares AudioZone with " + remotePlayerObjectSync.owningPlayer.PrintName() + ".");
#endif
                if (audioSettingManager.RemoveAudioSetting(remotePlayerObjectSync.owningPlayer, AUDIO_ZONE_SETTING_ID))
                {
                    audioSettingManager.ApplyAudioSetting(remotePlayerObjectSync.owningPlayer);
                }
            }
            else
            {
#if SYLAN_AUDIOMANAGER_DEBUG
                Debug.Log("[AudioManager] Does not share AudioZone with " + remotePlayerObjectSync.owningPlayer.PrintName() + ".");
#endif
                if (audioSettingManager.AddAudioSetting(remotePlayerObjectSync.owningPlayer, AUDIO_ZONE_SETTING_ID, audioZonePriority, AudioZoneAudioSettings))
                {
                    audioSettingManager.ApplyAudioSetting(remotePlayerObjectSync.owningPlayer);
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
                AudioZoneSyncCore remotePlayerSync = (AudioZoneSyncCore)RemotePlayerSyncs[i].Reference;
                if (!Utilities.IsValid(remotePlayerSync.owningPlayer)) continue; // Major trust issues.
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
