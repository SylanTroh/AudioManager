
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Sylan.AudioManager
{
    public class AudioZoneManager : UdonSharpBehaviour
    {
        VRCPlayerApi[] PlayerList;
        int[] PlayerIDList;
        AudioZone[][] activeZone = new AudioZone[0][];

        void Start()
        {
            PlayerList = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(PlayerList);
            QuickSortPlayers(PlayerList, 0, PlayerList.Length - 1);

            PlayerIDList = new int[PlayerList.Length];
            for (int i = 0; i < PlayerList.Length; i++)
            {
                PlayerIDList[i] = PlayerList[i].playerId;
            }

            activeZone = null;
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            Append(ref PlayerList, player);
            Append(ref PlayerIDList, player.playerId);
            Append(ref activeZone, null);
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            int index = Array.BinarySearch(PlayerIDList, player.playerId);
            var newPlayerArray = new VRCPlayerApi[PlayerIDList.Length-1];
            var newIDArray = new int[PlayerIDList.Length - 1];
            var newActiveZoneArray = new AudioZone[PlayerIDList.Length - 1][];

            for(int i = 0; i < index; i++) 
            {
                newPlayerArray[i] = PlayerList[i];
                newIDArray[i] = PlayerIDList[i];
                newActiveZoneArray[i] = activeZone[i];
            }
            for (int i = index + 1; i < PlayerIDList.Length; i++)
            {
                newPlayerArray[i-1] = PlayerList[i];
                newIDArray[i-1] = PlayerIDList[i];
                newActiveZoneArray[i-1] = activeZone[i];
            }
        }
        //public static AudioZone GetHighestPriority(AudioZone[] zones)
        //{
        //    AudioZone highestPriority;
        //}
        //public int ActiveZonesIndex(VRCPlayerApi player)
        //{
        //    return Array.BinarySearch(PlayerIDList, player.playerId);
        //}
        //public AudioZone[] ActiveZones(VRCPlayerApi player)
        //{
        //    return activeZone[ActiveZonesIndex(player)];
        //}
        //public void SetActiveZone(AudioZone zone, VRCPlayerApi player)
        //{
        //    var index = ActiveZonesIndex(player);
        //    if (zone.priority < activeZone[index].priority) return;
        //    activeZone[index] = zone;
        //}
        //public void RemoveActiveZone(AudioZone zone, VRCPlayerApi player)
        //{
        //    var index = ActiveZonesIndex(player);
        //    if (zone != activeZone[index]) return;
        //    activeZone[index] = null;
        //}
        public static void SetPlayerAudio(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            player.SetVoiceGain(AudioManager.DEFAULT_VOICE_GAIN);
            player.SetVoiceDistanceFar(AudioManager.DEFAULT_VOICE_RANGE_FAR);
            player.SetVoiceDistanceNear(AudioManager.DEFAULT_VOICE_RANGE_NEAR);
            player.SetVoiceVolumetricRadius(AudioManager.DEFAULT_VOICE_VOLUMETRIC_RADIUS);
            player.SetVoiceLowpass(AudioManager.DEFAULT_VOICE_LOWPASS);
        }
        public static void MutePlayer(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) return;
            player.SetVoiceGain(0);
        }
        public static void SetPlayerAudio(VRCPlayerApi player, AudioZone zone)
        {
            if (!Utilities.IsValid(player)) return;
            if (!Utilities.IsValid(zone))
            {
                SetPlayerAudio(player);
                return;
            }
            player.SetVoiceGain(zone.voiceGain);
            player.SetVoiceDistanceFar(zone.voiceFar);
            player.SetVoiceDistanceNear(zone.voiceNear);
            player.SetVoiceVolumetricRadius(zone.voiceVolumetricRadius);
            player.SetVoiceLowpass(zone.voiceLowpass);
        }
        public static void Append<T>(ref T[] array, T newElement)
        {
            if (!Utilities.IsValid(array))
            {
                array = new T[] { newElement };
                return;
            }

            var newArray = new T[array.Length + 1];
            array.CopyTo(newArray, 0);
            newArray[newArray.Length - 1] = newElement;

            array = newArray;
        }
        int PlayerIDComparer(VRCPlayerApi player1, VRCPlayerApi player2)
        {
            if (player1.playerId == player2.playerId) return 0;
            if (player1.playerId < player2.playerId) return -1;
            return 1;
        }
        private void QuickSortPlayers(VRCPlayerApi[] arr, int start, int end)
        {
            if (!Utilities.IsValid(arr)) return;

            int i = 0;
            if (start < end)
            {
                i = PartitionPlayers(arr, start, end);

                QuickSortPlayers(arr, start, i - 1);
                QuickSortPlayers(arr, i + 1, end);
            }
        }
        private int PartitionPlayers(VRCPlayerApi[] arr, int start, int end)
        {
            VRCPlayerApi temp;
            VRCPlayerApi p = arr[end];
            int i = start - 1;

            for (int j = start; j <= end - 1; j++)
            {
                if (PlayerIDComparer(arr[j], p) == -1)
                {
                    i++;
                    temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                }
            }

            temp = arr[i + 1];
            arr[i + 1] = arr[end];
            arr[end] = temp;
            return i + 1;
        }
    }
}
