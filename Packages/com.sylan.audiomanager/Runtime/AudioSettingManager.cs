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

        //Key:playerID -> DataList [ settingID[], settingPriority[], audioSettings[] ]
        private DataDictionary _AudioSettingDict = new DataDictionary();

        //
        // Manage _AudioSettingDict By player
        //
        /// <summary>
        /// </summary>
        /// <param name="player">Must be valid.</param>
        /// <param name="list"></param>
        /// <returns></returns>
        private bool TryGetPlayerAudioSettings(VRCPlayerApi player, out DataList list)
        {
            if (_AudioSettingDict.TryGetValue((DataToken)player.playerId, TokenType.DataList, out DataToken value))
            {
                list = value.DataList;
                return true;
            }
            Debug.LogError("[AudioManager] Failed to get AudioSettings for " + player.PrintName());
            list = null;
            return false;
        }
        private void InitPlayerAudioSettingDict(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;

            if (_AudioSettingDict.ContainsKey((DataToken)player.playerId))
            {
                Debug.Log("[AudioManager] AudioSettingDict already initialized for " + player.PrintName());
                return;
            }

            DataList DefaultAudioSettings = new DataList();
            DefaultAudioSettings.Add((DataToken)voiceGain);
            DefaultAudioSettings.Add((DataToken)voiceRangeNear);
            DefaultAudioSettings.Add((DataToken)voiceRangeFar);
            DefaultAudioSettings.Add((DataToken)volumetricRadius);
            DefaultAudioSettings.Add((DataToken)voiceLowpass);
            DataList DefaultDictEntry = new DataList();
            DefaultDictEntry.Add((DataToken)new DataList());
            DefaultDictEntry.Add((DataToken)new DataList());
            DefaultDictEntry.Add((DataToken)new DataList());
            DefaultDictEntry[SETTING_ID_INDEX].DataList.Add((DataToken)DefaultAudioSettingID);
            DefaultDictEntry[SETTING_PRIORITY_INDEX].DataList.Add((DataToken)DefaultAudioSettingPriority);
            DefaultDictEntry[SETTING_INDEX].DataList.Add((DataToken)DefaultAudioSettings);

            _AudioSettingDict.SetValue(key: (DataToken)player.playerId, value: (DataToken)DefaultDictEntry);
            Debug.Log("[AudioManager] Initialize AudioSettingDict for " + player.PrintName());
        }
        private DataList RemovePlayerAudioSettingDict(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return null;

            if (!_AudioSettingDict.Remove(key: (DataToken)player.playerId, out DataToken value))
            {
                Debug.LogError("[AudioManager] Failed to remove AudioSettingDict for " + player.PrintName());
                return null;
            }
            Debug.Log("[AudioManager] Removed AudioSettingDict for " + player.PrintName());
            return value.DataList;
        }
        public override void OnPlayerJoined(VRCPlayerApi joiningPlayer)
        {
            // No need to check if it is the local player which joined and loop through all players
            // as the joined event gets raised for everybody in the instance.
            InitPlayerAudioSettingDict(joiningPlayer);
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            RemovePlayerAudioSettingDict(player);
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

            if (!TryGetPlayerAudioSettings(player, out DataList list)) return;

            if (!list.TryGetValue(SETTING_ID_INDEX, TokenType.DataList, out DataToken token)) return;
            DataList settingIDList = token.DataList;
            if (!list.TryGetValue(SETTING_PRIORITY_INDEX, TokenType.DataList, out token)) return;
            DataList priorityList = token.DataList;
            if (!list.TryGetValue(SETTING_INDEX, TokenType.DataList, out token)) return;
            DataList SettingList = token.DataList;

            if (settingIDList.Contains((DataToken)settingID)) return;

            int index = -1;

            for (int i = 0; i < priorityList.Count; i++)
            {
                if (!priorityList.TryGetValue(i, TokenType.Int, out DataToken p)) continue;
                if (priority < p.Int)
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
            {
                list[SETTING_ID_INDEX].DataList.Add((DataToken)settingID);
                list[SETTING_PRIORITY_INDEX].DataList.Add((DataToken)priority);
                list[SETTING_INDEX].DataList.Add((DataToken)audioSetting);
            }
            else
            {
                list[SETTING_ID_INDEX].DataList.Insert(index, (DataToken)settingID);
                list[SETTING_PRIORITY_INDEX].DataList.Insert(index, (DataToken)priority);
                list[SETTING_INDEX].DataList.Insert(index, (DataToken)audioSetting);
            }
        }
        public bool RemoveAudioSetting(VRCPlayerApi player, string settingID)
        {
            if (!Utilities.IsValid(player)) return false;
            if (player == Networking.LocalPlayer) return false;

            if (!TryGetPlayerAudioSettings(player, out DataList list)) return false;

            if (!list.TryGetValue(SETTING_ID_INDEX, TokenType.DataList, out DataToken token)) return false;
            DataList settingIDList = token.DataList;
            if (!list.TryGetValue(SETTING_PRIORITY_INDEX, TokenType.DataList, out token)) return false;
            DataList priorityList = token.DataList;
            if (!list.TryGetValue(SETTING_INDEX, TokenType.DataList, out token)) return false;
            DataList SettingList = token.DataList;

            int index = settingIDList.IndexOf((DataToken)settingID);
            if (index == -1) return false;
            else
            {
                list[SETTING_ID_INDEX].DataList.RemoveAt(index);
                list[SETTING_PRIORITY_INDEX].DataList.RemoveAt(index);
                list[SETTING_INDEX].DataList.RemoveAt(index);
                return true;
            }
        }
        public void ClearAudioSettings(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            _AudioSettingDict.Remove((DataToken)player.playerId);
            InitPlayerAudioSettingDict(player);
        }
        //
        //Update Audio Settings
        //
        public void UpdateAudioSettings(VRCPlayerApi triggeringPlayer)
        {
            if (!Utilities.IsValid(triggeringPlayer)) return;
            if (triggeringPlayer == Networking.LocalPlayer) return;

            //If someone else caused the update, update triggering player
            ApplyAudioSetting(triggeringPlayer);
        }
        public void ApplyAudioSetting(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            if (player == Networking.LocalPlayer) return;

            if (!TryGetPlayerAudioSettings(player, out DataList list)) return;

            //VRCJson.TrySerializeToJson(list, JsonExportType.Minify, out DataToken result1);
            //Debug.Log(result1.ToString());

            if (!list.TryGetValue(SETTING_ID_INDEX, TokenType.DataList, out DataToken token)) return;
            DataList settingIDList = token.DataList;
            if (!list.TryGetValue(SETTING_PRIORITY_INDEX, TokenType.DataList, out token)) return;
            DataList priorityList = token.DataList;
            if (!list.TryGetValue(SETTING_INDEX, TokenType.DataList, out token)) return;
            DataList SettingList = token.DataList;

            //Get Highest Priority Setting
            if (!list[SETTING_INDEX].DataList.TryGetValue(0, TokenType.DataList, out token)) return;

            DataList audioSetting = token.DataList;
            if (!ValidateAudioSetting(audioSetting)) return;

            _VoiceApplicator.ApplyVoiceSetting(player, audioSetting);

            string debugString = "[AudioManager] Setting " + player.PrintName() + " Audio:";
            debugString += " SettingID:" + list[SETTING_ID_INDEX].DataList[0].String;
            debugString += ", VoiceGain:" + audioSetting[VOICE_GAIN_INDEX].Float.ToString();
            debugString += ", VoiceNear:" + audioSetting[RANGE_NEAR_INDEX].Float.ToString();
            debugString += ", VoiceFar:" + audioSetting[RANGE_FAR_INDEX].Float.ToString();
            debugString += ", VolumetricRadius:" + audioSetting[VOLUMETRIC_RADIUS_INDEX].Float.ToString();
            debugString += ", Lowpass:" + audioSetting[VOICE_LOWPASS_INDEX].Boolean.ToString();

            Debug.Log(debugString);
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
