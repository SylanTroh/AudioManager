using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioSettingManager : UdonSharpBehaviour
    {
        public const int DEFAULT_PRIORITY = 0;
        public const float DEFAULT_VOICE_GAIN = 15.0f;
        public const float DEFAULT_VOICE_RANGE_NEAR = 0.0f;
        public const float DEFAULT_VOICE_RANGE_FAR = 15.0f;
        public const float DEFAULT_VOICE_VOLUMETRIC_RADIUS = 0.0f;
        public const bool DEFAULT_VOICE_LOWPASS = true;

        public const int VOICE_GAIN_INDEX = 0;
        public const int RANGE_NEAR_INDEX = 1;
        public const int RANGE_FAR_INDEX = 2;
        public const int VOLUMETRIC_RADIUS_INDEX = 3;
        public const int VOICE_LOWPASS_INDEX = 4;
        public const int FADE_ENABLED_INDEX = 5;
        public const int FADE_DURATION_INDEX = 6;

        public const int SETTING_ID_INDEX = 0;
        public const int SETTING_PRIORITY_INDEX = 1;
        public const int SETTING_INDEX = 2;

        private const string DefaultAudioSettingID = "";
        private const int DefaultAudioSettingPriority = int.MaxValue;

        [Header("Set default AudioSetting")]
        [SerializeField] private float voiceGain = DEFAULT_VOICE_GAIN;
        [SerializeField] private float voiceRangeNear = DEFAULT_VOICE_RANGE_NEAR;
        [SerializeField] private float voiceRangeFar = DEFAULT_VOICE_RANGE_FAR;
        [SerializeField] private float volumetricRadius = DEFAULT_VOICE_VOLUMETRIC_RADIUS;
        [SerializeField] private bool voiceLowpass = DEFAULT_VOICE_LOWPASS;

        public AudioZoneManager AudioZoneManager { get => _AudioZoneManager; private set { _AudioZoneManager = value; } }
        [HideInInspector, SerializeField] private AudioZoneManager _AudioZoneManager;
        public const string AudioZoneManagerPropertyName = nameof(_AudioZoneManager);

        public VoiceApplicator VoiceApplicator { get => _VoiceApplicator; private set { _VoiceApplicator = value; } }
        [HideInInspector, SerializeField] private VoiceApplicator _VoiceApplicator;
        public const string VoiceApplicatorPropertyName = nameof(_VoiceApplicator);

        /// <summary>
        /// <para>Key: playerID -> DataList [ settingID[], settingPriority[], audioSettings[] ]</para>
        /// </summary>
        private DataDictionary allPlayerSettings = new DataDictionary();
        /// <summary>
        /// <para><see cref="int"/> playerId -> <see cref="VRCPlayerApi"/> player</para>
        /// <para>Contains all players for which we have already received the
        /// <see cref="OnPlayerLeft(VRCPlayerApi)"/> event.</para>
        /// <para>As soon as the <see cref="VRCPlayerApi"/> turns invalid it will get removed from this
        /// dictionary. Therefore this dictionary will be empty 99% of the time.</para>
        /// </summary>
        private DataDictionary recentlyLeftPlayers = new DataDictionary();
        private bool trackingRecentlyLeftPlayers = false;

        //
        // Manage _AudioSettingDict By player
        //
        /// <summary>
        /// </summary>
        /// <param name="player">Must be valid.</param>
        /// <param name="list"></param>
        /// <returns></returns>
        private bool TryGetOrInitPlayerAudioSettings(VRCPlayerApi player, out DataList list)
        {
            if (allPlayerSettings.TryGetValue((DataToken)player.playerId, out DataToken value))
            {
                list = value.DataList;
                return true;
            }
            if (!recentlyLeftPlayers.ContainsKey((DataToken)player.playerId))
            {
                // Having the getter function also call init removes any dependency on order of operations
                // around scripts receiving the OnPlayerJoined event.
                // Other scripts can interact with the AudioManager for players for whom the manager has not
                // received the OnPlayerJoined event yet.
                list = InitPlayerAudioSettings(player);
                return true;
            }
            // Player already left, the player reference is about to turn invalid,
            // no logic shall interact with that player anymore, just ignore attempts doing so.
            list = null;
            return false;
        }
        private DataList InitPlayerAudioSettings(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return null;

            if (allPlayerSettings.TryGetValue((DataToken)player.playerId, out DataToken value))
            {
                Debug.Log("[AudioManager] AudioSettingDict already initialized for " + player.PrintName());
                return value.DataList;
            }

            DataList defaultAudioSettings = new DataList();
            defaultAudioSettings.Add((DataToken)voiceGain);
            defaultAudioSettings.Add((DataToken)voiceRangeNear);
            defaultAudioSettings.Add((DataToken)voiceRangeFar);
            defaultAudioSettings.Add((DataToken)volumetricRadius);
            defaultAudioSettings.Add((DataToken)voiceLowpass);
            DataList defaultPlayerAudioSettings = new DataList();
            defaultPlayerAudioSettings.Add((DataToken)new DataList());
            defaultPlayerAudioSettings.Add((DataToken)new DataList());
            defaultPlayerAudioSettings.Add((DataToken)new DataList());
            defaultPlayerAudioSettings[SETTING_ID_INDEX].DataList.Add((DataToken)DefaultAudioSettingID);
            defaultPlayerAudioSettings[SETTING_PRIORITY_INDEX].DataList.Add((DataToken)DefaultAudioSettingPriority);
            defaultPlayerAudioSettings[SETTING_INDEX].DataList.Add((DataToken)defaultAudioSettings);

            allPlayerSettings.SetValue(key: (DataToken)player.playerId, value: (DataToken)defaultPlayerAudioSettings);
            Debug.Log("[AudioManager] Initialize PlayerAudioSettings for " + player.PrintName());
            return defaultPlayerAudioSettings;
        }
        private void RemovePlayerAudioSettings(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;

            if (!allPlayerSettings.Remove((DataToken)player.playerId))
            {
                // This is just an info, the goal of RemovePlayerAudioSettings is removing the settings,
                // if they already didn't exist then its goal has already been achieved.
                Debug.Log("[AudioManager] No PlayerAudioSettings present to remove for " + player.PrintName());
                return;
            }
            Debug.Log("[AudioManager] Removed PlayerAudioSettings for " + player.PrintName());
        }
        public override void OnPlayerJoined(VRCPlayerApi joiningPlayer)
        {
            // No need to check if it is the local player which joined and loop through all players
            // as the joined event gets raised for everybody in the instance.
            InitPlayerAudioSettings(joiningPlayer);
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            RemovePlayerAudioSettings(player);
            recentlyLeftPlayers.Add((DataToken)player.playerId, new DataToken(player));
            StartLeftPlayerTrackingLoop();
        }
        private void StartLeftPlayerTrackingLoop()
        {
            if (trackingRecentlyLeftPlayers) return;
            trackingRecentlyLeftPlayers = true;
            SendCustomEventDelayedFrames(nameof(InternalLeftPlayerTrackingLoop), 1);
        }
        public void InternalLeftPlayerTrackingLoop()
        {
            DataList playerIds = recentlyLeftPlayers.GetKeys();
            DataList players = recentlyLeftPlayers.GetValues();
            int count = playerIds.Count;
            for (int i = 0; i < count; i++)
            {
                VRCPlayerApi player = (VRCPlayerApi)players[i].Reference;
                if (Utilities.IsValid(player)) continue; // Still valid, keep.
                recentlyLeftPlayers.Remove(playerIds[i]);
            }
            if (recentlyLeftPlayers.Count == 0)
            {
                trackingRecentlyLeftPlayers = false;
                return;
            }
            SendCustomEventDelayedFrames(nameof(InternalLeftPlayerTrackingLoop), 1);
        }
        //
        //Manage _AudioSettingDict[player] by settingID
        //
        private bool ValidateAudioSetting(DataList audioSetting)
        {
            if (audioSetting == null)
            {
                Debug.LogError("[AudioManager] Invalid Audio Setting - null");
                return false;
            }

            if (audioSetting.Count != 5 && audioSetting.Count != 7)
            {
                Debug.LogError("[AudioManager] Invalid Audio Setting - expected 5 or 7 elements, got " + audioSetting.Count);
                return false;
            }

            // Validate required voice parameters
            bool isValid =
                (audioSetting.TryGetValue(VOICE_GAIN_INDEX, TokenType.Float, out DataToken discard)) &&
                (audioSetting.TryGetValue(RANGE_NEAR_INDEX, TokenType.Float, out discard)) &&
                (audioSetting.TryGetValue(RANGE_FAR_INDEX, TokenType.Float, out discard)) &&
                (audioSetting.TryGetValue(VOLUMETRIC_RADIUS_INDEX, TokenType.Float, out discard)) &&
                (audioSetting.TryGetValue(VOICE_LOWPASS_INDEX, TokenType.Boolean, out discard));

            if (!isValid)
            {
                Debug.LogError("[AudioManager] Invalid Audio Setting - missing or wrong type for voice parameters");
                return false;
            }

            // Validate fade parameters if present
            if (audioSetting.Count == 7)
            {
                isValid = isValid &&
                    (audioSetting.TryGetValue(FADE_ENABLED_INDEX, TokenType.Boolean, out discard)) &&
                    (audioSetting.TryGetValue(FADE_DURATION_INDEX, TokenType.Float, out discard));

                if (!isValid)
                {
                    Debug.LogError("[AudioManager] Invalid Audio Setting - wrong type for fade parameters");
                    return false;
                }
            }

            return true;
        }
        public void AddAudioSetting(VRCPlayerApi player, string settingID, int priority, DataList audioSetting)
        {
            if (!Utilities.IsValid(player)) return;
            if (player == Networking.LocalPlayer) return;

            if (!ValidateAudioSetting(audioSetting)) return;
            _AddAudioSetting(player, settingID, priority, audioSetting);
        }
        private void _AddAudioSetting(VRCPlayerApi player, string settingID, int priority, DataList audioSetting)
        {
            if (audioSetting == null) return;

            if (!TryGetOrInitPlayerAudioSettings(player, out DataList list)) return;

            DataList settingIDList = list[SETTING_ID_INDEX].DataList;
            DataList priorityList = list[SETTING_PRIORITY_INDEX].DataList;
            DataList settingList = list[SETTING_INDEX].DataList;

            if (settingIDList.Contains((DataToken)settingID)) return;

            int index = priorityList.Count; // Insert at "count" adds to the end of the list.

            for (int i = 0; i < priorityList.Count; i++)
            {
                if (priority < priorityList[i].Int)
                {
                    index = i;
                    break;
                }
            }

            settingIDList.Insert(index, (DataToken)settingID);
            priorityList.Insert(index, (DataToken)priority);
            settingList.Insert(index, (DataToken)audioSetting);
        }
        public bool RemoveAudioSetting(VRCPlayerApi player, string settingID)
        {
            if (!Utilities.IsValid(player)) return false;
            if (player == Networking.LocalPlayer) return false;

            if (!TryGetOrInitPlayerAudioSettings(player, out DataList list)) return false;

            DataList settingIDList = list[SETTING_ID_INDEX].DataList;
            int index = settingIDList.IndexOf((DataToken)settingID);
            if (index == -1) return false;
            settingIDList.RemoveAt(index);
            list[SETTING_PRIORITY_INDEX].DataList.RemoveAt(index);
            list[SETTING_INDEX].DataList.RemoveAt(index);
            return true;
        }
        public void ClearAudioSettings(VRCPlayerApi player)
        {
            RemovePlayerAudioSettings(player);
            InitPlayerAudioSettings(player);
        }
        //
        //Update Audio Settings
        //
        public void UpdateAudioSettings(VRCPlayerApi triggeringPlayer) => ApplyAudioSetting(triggeringPlayer); // Backwards compatibility.
        public void ApplyAudioSetting(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            if (player == Networking.LocalPlayer) return;

            if (!TryGetOrInitPlayerAudioSettings(player, out DataList list)) return;

            //VRCJson.TrySerializeToJson(list, JsonExportType.Minify, out DataToken result1);
            //Debug.Log(result1.ToString());

            //Get Highest Priority Setting
            if (!list[SETTING_INDEX].DataList.TryGetValue(0, TokenType.DataList, out DataToken settingToken)) return;

            DataList audioSetting = settingToken.DataList;
            if (!ValidateAudioSetting(audioSetting)) return;

            _VoiceApplicator.ApplyVoiceSetting(player, audioSetting);

            Debug.Log("[AudioManager] Setting " + player.PrintName() + " Audio:"
                + " SettingID:" + list[SETTING_ID_INDEX].DataList[0].String
                + ", VoiceGain:" + audioSetting[VOICE_GAIN_INDEX].Float.ToString()
                + ", VoiceNear:" + audioSetting[RANGE_NEAR_INDEX].Float.ToString()
                + ", VoiceFar:" + audioSetting[RANGE_FAR_INDEX].Float.ToString()
                + ", VolumetricRadius:" + audioSetting[VOLUMETRIC_RADIUS_INDEX].Float.ToString()
                + ", Lowpass:" + audioSetting[VOICE_LOWPASS_INDEX].Boolean.ToString());
        }
    }
    public static class AudioSettingManagerExtensions
    {
        //
        //Extensions for VRCPlayerAPI
        //
        public static void AddAudioSetting(this VRCPlayerApi player, AudioSettingManager settingManager, string settingID, int priority, DataList audioSetting)
        {
            settingManager.AddAudioSetting(player, settingID, priority, audioSetting);
        }
        public static bool RemoveAudioSetting(this VRCPlayerApi player, AudioSettingManager settingManager, string settingID)
        {
            return settingManager.RemoveAudioSetting(player, settingID);
        }
        public static void ClearAudioSettings(this VRCPlayerApi player, AudioSettingManager settingManager)
        {
            settingManager.ClearAudioSettings(player);
        }
        //
        //
        //
    }
}
