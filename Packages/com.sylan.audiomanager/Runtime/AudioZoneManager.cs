
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
        public AudioSettingManager AudioSettingManager { get => _AudioSettingManager; private set { _AudioSettingManager = value; } }
        [HideInInspector, SerializeField] private AudioSettingManager _AudioSettingManager;
        public const string AudioSettingManagerPropertyName = nameof(_AudioSettingManager);

        [HideInInspector, SerializeField] private AudioZone[] AudioZones;
        public const string AudioZonesPropertyName = nameof(AudioZones);

        [Header("Set AudioSetting when in different audiozones")]
        [SerializeField] private float voiceGain = 7.0f;
        [SerializeField] private float voiceRangeNear = AudioSettingManager.DEFAULT_VOICE_RANGE_NEAR;
        [SerializeField] private float voiceRangeFar = 1.75f;
        [SerializeField] private float volumetricRadius = AudioSettingManager.DEFAULT_VOICE_VOLUMETRIC_RADIUS;
        [SerializeField] private bool voiceLowpass = AudioSettingManager.DEFAULT_VOICE_LOWPASS;


        //Key:playerID -> DataDictionary Key:zoneID -> int numOccurences
        private DataDictionary _AudioZoneDict = new DataDictionary();

        public const int AUDIO_ZONE_PRIORITY = 1000;
        public const string AUDIO_ZONE_SETTING_ID = "AUDIOZONEVOICESETTING";
        DataList AudioZoneAudioSettings = new DataList()
        {
            (DataToken)7.0f, //Voice Gain
            (DataToken)0.0f, //Voice Range Near
            (DataToken)2.0f, //Voice Range Far
            (DataToken)AudioSettingManager.DEFAULT_VOICE_VOLUMETRIC_RADIUS,
            (DataToken)AudioSettingManager.DEFAULT_VOICE_LOWPASS
        };
        private void Start()
        {
            AudioZoneAudioSettings[AudioSettingManager.VOICE_GAIN_INDEX] = (DataToken)voiceGain;
            AudioZoneAudioSettings[AudioSettingManager.RANGE_NEAR_INDEX] = (DataToken)voiceRangeNear;
            AudioZoneAudioSettings[AudioSettingManager.RANGE_FAR_INDEX] = (DataToken)voiceRangeFar;
            AudioZoneAudioSettings[AudioSettingManager.VOLUMETRIC_RADIUS_INDEX] = (DataToken)volumetricRadius;
            AudioZoneAudioSettings[AudioSettingManager.VOICE_LOWPASS_INDEX] = (DataToken)voiceLowpass;
        }

        //
        // Manage AudioZoneDict By Player
        //
        public DataDictionary GetPlayerAudioZoneDict(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return null;
            if (!player.IsValid()) return null;

            if (!_AudioZoneDict.TryGetValue((DataToken)player.playerId, TokenType.DataDictionary, out DataToken value))
            {
                Debug.LogError("[AudioManager] Failed to get AudioZoneDict for " + player.displayName + "-" + player.playerId.ToString());
                return null;
            }
            return value.DataDictionary;
        }
        public void InitPlayerAudioZoneDict(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            if (!player.IsValid()) return;

            if (_AudioZoneDict.TryGetValue((DataToken)player.playerId, TokenType.DataDictionary, out DataToken value))
            {
                Debug.Log("[AudioManager] AudioZoneDict already initialized for " + player.displayName + "-" + player.playerId.ToString());
                return;
            }
            _AudioZoneDict.SetValue(key: (DataToken)player.playerId, value: (DataToken)(new DataDictionary()));
            Debug.Log("[AudioManager] Initialize AudioZoneDict for " + player.displayName + "-" + player.playerId.ToString());
        }
        public DataDictionary RemovePlayerAudioZoneDict(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return null;
            if (!player.IsValid()) return null;

            if (!_AudioZoneDict.Remove(key: (DataToken)player.playerId, out DataToken value))
            {
                Debug.LogError("[AudioManager] Failed to remove AudioZoneDict for " + player.displayName + "-" + player.playerId.ToString());
            }
            Debug.Log("[AudioManager] Removed AudioZoneDict for " + player.displayName + "-" + player.playerId.ToString());
            return value.DataDictionary;
        }
        public override void OnPlayerJoined(VRCPlayerApi joiningPlayer)
        {
            if (joiningPlayer == Networking.LocalPlayer)
            {
                VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
                VRCPlayerApi.GetPlayers(players);
                foreach (var player in players)
                {
                    InitPlayerAudioZoneDict(player);
                }
            }
            else
            {
                InitPlayerAudioZoneDict(joiningPlayer);
            }
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            RemovePlayerAudioZoneDict(player);
        }
        //
        //Manage AudioZoneDict[player]
        //
        public void EnterAudioZone(VRCPlayerApi player, string zoneID)
        {
            DataDictionary dict = GetPlayerAudioZoneDict(player);
            if(!Utilities.IsValid(dict)) return;
            if (!dict.TryGetValue((DataToken)zoneID, TokenType.Int, out DataToken value))
            {
                dict.Add((DataToken)zoneID, (DataToken)1);
            }
            else
            {
                dict.SetValue((DataToken)zoneID, (DataToken)(value.Int + 1));
            }
        }
        public bool ExitAudioZone(VRCPlayerApi player, string zoneID)
        {
            DataDictionary dict = GetPlayerAudioZoneDict(player);
            if (!Utilities.IsValid(dict)) return false;
            if (!dict.TryGetValue((DataToken)zoneID, TokenType.Int, out DataToken value))
            {
                Debug.LogError("[AudioManager] Tried to exit AudioZone not in Dict");
                return false;
            }
            else if (value.Int <= 1)
            {
                return ExitAllAudioZones(player, zoneID);
            }
            else
            {
                dict.SetValue((DataToken)zoneID, (DataToken)(value.Int - 1));
                return true;
            }
        }
        public bool ExitAllAudioZones(VRCPlayerApi player, string zoneID)
        {
            DataDictionary dict = GetPlayerAudioZoneDict(player);
            if (!dict.Remove((DataToken)zoneID, out DataToken value))
            {
                Debug.LogError("[AudioManager] Tried to remove AudioZone not in Dict");
                return false;
            }
            return true;
        }
        public void ClearAudioZones(VRCPlayerApi player)
        {
            DataDictionary dict = GetPlayerAudioZoneDict(player);
            if (!Utilities.IsValid(dict)) return;
            dict.Clear();
        }
        public bool InAudioZone(VRCPlayerApi player, string zoneID)
        {
            DataDictionary dict = GetPlayerAudioZoneDict(player);
            if (!Utilities.IsValid(dict)) return false;
            if (!dict.TryGetValue((DataToken)zoneID, TokenType.Int, out DataToken value))
            {
                return false;
            }
            if (value.Int == 0) return false;
            return true;
        }
        public bool ShareAudioZone(VRCPlayerApi player1, VRCPlayerApi player2)
        {
            DataDictionary dict1 = GetPlayerAudioZoneDict(player1);
            DataDictionary dict2 = GetPlayerAudioZoneDict(player2);
            if(!Utilities.IsValid(dict1) || !Utilities.IsValid(dict2)) return false;
            DataList list1 = dict1.GetKeys();
            DataList list2 = dict2.GetKeys();

            //VRCJson.TrySerializeToJson(dict1, JsonExportType.Minify, out DataToken result1);
            //VRCJson.TrySerializeToJson(dict2, JsonExportType.Minify, out DataToken result2);
            //Debug.Log(result1.ToString());
            //Debug.Log(result2.ToString());

            //Transition Zones only match no zone, not other transition zones
            if (list1.Count == 0 && InAudioZone(player2, String.Empty)) return true;
            if (list2.Count == 0 && InAudioZone(player1, String.Empty)) return true;
            if (list1.Count == 0 && list2.Count == 0) return true;
            if (list1.Count == 0 || list2.Count == 0) return false;

            foreach (DataToken token in list1.ToArray())
            {
                if (token.TokenType != TokenType.String) continue;
                string id = token.String;
                if(id == string.Empty) continue;
                if (InAudioZone(player2, id)) return true;
            }
            return false;
        }
        //
        //Update Audio Settings
        //
        public void UpdateAudioZoneSetting(VRCPlayerApi triggeringPlayer, bool hasAudioSettingComponent = false)
        {
            if (triggeringPlayer == null) return;
            if (!triggeringPlayer.IsValid()) return;

            if (triggeringPlayer != Networking.LocalPlayer)
            {
                //If someone else caused the update, update triggering player
                ApplyAudioZoneSetting(triggeringPlayer);
                if(!hasAudioSettingComponent) _AudioSettingManager.ApplyAudioSetting(triggeringPlayer);
            }
            else
            {
                //If the local player caused the update, update all players
                VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
                VRCPlayerApi.GetPlayers(players);
                foreach (VRCPlayerApi player in players)
                {
                    ApplyAudioZoneSetting(player);
                    if (!hasAudioSettingComponent) _AudioSettingManager.ApplyAudioSetting(player);
                }
            }
        }
        public void ApplyAudioZoneSetting(VRCPlayerApi player)
        {
            if (!player.IsValid()) return;
            if (player == Networking.LocalPlayer) return;
            if (Networking.LocalPlayer.SharesAudioZoneWith(player, this))
            {
                //Debug.Log("[AudioManager] Shares AudioZone with" + player.displayName + ".");
                _AudioSettingManager.RemoveAudioSetting(player, AUDIO_ZONE_SETTING_ID);
            }
            else
            {
                //Debug.Log("[AudioManager] Does not share AudioZone with " + player.displayName + ".");
                _AudioSettingManager.AddAudioSetting(player, AUDIO_ZONE_SETTING_ID, AUDIO_ZONE_PRIORITY, AudioZoneAudioSettings);
            }
        }
    }
    public static class AudioZoneManagerExtensions
    {
        //
        //Extensions for VRCPlayerAPI
        //
        public static void EnterAudioZone(this VRCPlayerApi player, AudioZoneManager zoneManager, string zoneID)
        {
            zoneManager.EnterAudioZone(player, zoneID);
        }
        public static bool ExitAudioZone(this VRCPlayerApi player, AudioZoneManager zoneManager, string zoneID)
        {
            return zoneManager.ExitAudioZone(player, zoneID);
        }
        public static bool ExitAllAudioZones(this VRCPlayerApi player, AudioZoneManager zoneManager, string zoneID)
        {
            return zoneManager.ExitAllAudioZones(player, zoneID);
        }
        public static void ClearAudioZones(this VRCPlayerApi player, AudioZoneManager zoneManager)
        {
            zoneManager.ClearAudioZones(player);
        }
        public static bool InAudioZone(this VRCPlayerApi player, AudioZoneManager zoneManager, string zoneID)
        {
            return zoneManager.InAudioZone(player, zoneID);
        }
        public static bool SharesAudioZoneWith(this VRCPlayerApi player1, VRCPlayerApi player2, AudioZoneManager zoneManager)
        {
            return zoneManager.ShareAudioZone(player1, player2);
        }
        //
        //
        //
    }
}
