
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.SDKBase.Midi;
using VRC.Udon;
using static VRC.Core.APIGroup;

namespace Sylan.AudioManager
{
    public class AudioZone : UdonSharpBehaviour
    {
        private AudioManager _AudioManager;

        [Header("Primary AudioZone ID")]
        public string zoneID = string.Empty;

        [Header("Additional AudioZones. Useful for transitions.", order = 0)]
        [Space(-10, order = 1)]
        [Header("To hear players who are not in a zone, set an empty string.", order = 2)]
        public string[] transitionZoneIDs;

        public void Init(AudioManager audioManager)
        {
            _AudioManager = audioManager;
        }
        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            Debug.Log("[AudioZone] Entering Zone " + zoneID + gameObject.GetInstanceID());

            player.EnterZone(_AudioManager.AudioZoneManager, zoneID);
            foreach (string id in transitionZoneIDs)
            {
                player.EnterZone(_AudioManager.AudioZoneManager, id);
            }

            UpdateAudio(player);
        }
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            Debug.Log("[AudioZone] Exiting Zone " + zoneID + gameObject.GetInstanceID());

            player.ExitZone(_AudioManager.AudioZoneManager, zoneID);
            foreach (string id in transitionZoneIDs)
            {
                player.ExitZone(_AudioManager.AudioZoneManager, id);
            }

            UpdateAudio(player);
        }
        public void UpdateAudio(VRCPlayerApi triggeringPlayer)
        {
            if(triggeringPlayer == Networking.LocalPlayer) _AudioManager.SetAllAudio();
            else _AudioManager.SetPlayerAudio(triggeringPlayer);
        }
    }
}
