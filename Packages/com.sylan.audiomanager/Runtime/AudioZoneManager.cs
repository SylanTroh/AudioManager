
using System;
using UnityEditor;
using UnityEngine;
using UdonSharp;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioZoneManager : UdonSharpBehaviour
    {
        private AudioManager _AudioManager;
        
        [SerializeField] private AudioZone[] SerializedAudioZones;
        //Key:playerID -> DataDictionary Key:zoneID -> int numOccurences
        private DataDictionary _AudioZoneID = new DataDictionary();

        private void Start()
        {
            _AudioManager = AudioManager.GetAudioManager(transform);
            foreach (AudioZone zone in SerializedAudioZones) zone.Init(_AudioManager);
        }
        //
        // Manage AudioZoneDict By Player
        //
        public DataDictionary GetPlayerAudioZoneDict(VRCPlayerApi player)
        {
            if (!_AudioZoneID.TryGetValue((DataToken)player.playerId, TokenType.DataDictionary, out DataToken value))
            {
                Debug.LogError("[AudioManager] Failed to get AudioZoneDict for " + player.displayName);
                return null;
            }
            return value.DataDictionary;
        }
        public void InitPlayerAudioZoneDict(VRCPlayerApi player)
        {
            if (player == null) return;
            _AudioZoneID.Add(key: (DataToken)player.playerId, value: new DataDictionary());
            Debug.Log("[AudioManager] Initialize AudioZoneDict for " + player.displayName);
        }
        public DataDictionary RemovePlayerAudioZoneDict(VRCPlayerApi player)
        {
            if (player == null) return null;
            if(!_AudioZoneID.Remove(key: (DataToken)player.playerId, out DataToken value))
            {
                Debug.LogError("[AudioManager] Failed to remove AudioZoneDict for " + player.displayName);
            }
            Debug.Log("[AudioManager] Removed AudioZoneDict for " + player.displayName);
            return value.DataDictionary;
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            InitPlayerAudioZoneDict(player);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            RemovePlayerAudioZoneDict(player);
        }
        //
        //Manage AudioZoneDict[player] by ZoneID
        //
        public void EnterZone(VRCPlayerApi player, string zoneID)
        {
            DataDictionary dict = GetPlayerAudioZoneDict(player);
            if (!dict.TryGetValue((DataToken)zoneID, TokenType.Int, out DataToken value))
            {
                dict.Add((DataToken)zoneID, 1);
            }
            else
            {
                dict.SetValue((DataToken)zoneID, value.Int + 1);
            }
        }
        public bool ExitZone(VRCPlayerApi player, string zoneID)
        {
            DataDictionary dict = GetPlayerAudioZoneDict(player);
            if (!dict.TryGetValue((DataToken)zoneID, TokenType.Int, out DataToken value))
            {
                Debug.LogError("[AudioManager] Tried to exit AudioZone not in Dict");
                return false;
            }
            else if(value.Int <= 1)
            {
                return ExitAllZones(player, zoneID);
            }
            else
            {
                dict.SetValue((DataToken)zoneID,value.Int - 1);
                return true;
            }
        }
        public bool ExitAllZones(VRCPlayerApi player, string zoneID)
        {
            DataDictionary dict = GetPlayerAudioZoneDict(player);
            if (!dict.Remove((DataToken)zoneID, out DataToken value))
            {
                Debug.LogError("[AudioManager] Tried to remove AudioZone not in Dict");
                return false;
            }
            return true;
        }
        public void ClearZones(VRCPlayerApi player)
        {
            DataDictionary dict = GetPlayerAudioZoneDict(player);
            dict.Clear();
        }
        public bool InZone(VRCPlayerApi player,string zoneID)
        {
            DataDictionary dict = GetPlayerAudioZoneDict(player);
            if (!dict.TryGetValue((DataToken)zoneID, TokenType.Int, out DataToken value))
            {
                return false;
            }
            if(value.Int == 0) return false;
            return true;
        }
        public bool ShareZone(VRCPlayerApi player1, VRCPlayerApi player2)
        {
            DataDictionary dict1 = GetPlayerAudioZoneDict(player1);
            DataList list1 = dict1.GetKeys();
            DataDictionary dict2 = GetPlayerAudioZoneDict(player2);
            DataList list2 = dict2.GetKeys();

            bool defaultZone1 = InZone(player1,String.Empty) || list1.Count == 0;
            bool defaultZone2 = InZone(player2, String.Empty) || list2.Count == 0;
            if (defaultZone1 && defaultZone2) return true;

            foreach (DataToken token in list1.ToArray())
            {
                if(token.TokenType != TokenType.String) continue;
                string id = token.String;
                if(InZone(player2,id)) return true;
            }
            return false;
        }
        //
        //
        //
    }
    public static class AudioZoneManagerExtensions
    {
        //
        //Extensions for VRCPlayerAPI
        //
        public static void EnterZone(this VRCPlayerApi player, AudioZoneManager zoneManager, string zoneID)
        {
            zoneManager.EnterZone(player, zoneID);
        }
        public static bool ExitZone(this VRCPlayerApi player, AudioZoneManager zoneManager, string zoneID)
        {
            return zoneManager.ExitZone(player, zoneID);
        }
        public static bool ExitAllZones(this VRCPlayerApi player, AudioZoneManager zoneManager, string zoneID)
        {
            return zoneManager.ExitAllZones(player, zoneID);
        }
        public static void ClearZones(this VRCPlayerApi player, AudioZoneManager zoneManager)
        {
            zoneManager.ClearZones(player);
        }
        public static bool InZone(this VRCPlayerApi player, AudioZoneManager zoneManager, string zoneID)
        {
            return zoneManager.InZone(player, zoneID);
        }
        public static bool SharesZoneWith(this VRCPlayerApi player1, VRCPlayerApi player2, AudioZoneManager zoneManager)
        {
            return zoneManager.ShareZone(player1, player2);
        }
        //
        //
        //
    }
}
