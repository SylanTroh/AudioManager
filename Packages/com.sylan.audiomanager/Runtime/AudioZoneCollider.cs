
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [AddComponentMenu("Scrips/Audio Zone Collider")]
    public class AudioZoneCollider : UdonSharpBehaviour
    {
        [Header("Primary AudioZone ID")]
        public string zoneID = string.Empty;

        [Header("Additional AudioZones. Useful for transitions.", order = 0)]
        [Space(-10, order = 1)]
        [Header("To match players who are not in a zone, set an empty string.", order = 2)]
        public string[] transitionZoneIDs;

        [HideInInspector, SerializeField] private AudioZoneManager _AudioZoneManager;
        public const string AudioZoneManagerPropertyName = nameof(_AudioZoneManager);

        private bool hasAudioSettingComponent = false;

        public bool isNegativeZone = false;

        private int localPlayerEnterCounter = 0;

        private void Start()
        {
            hasAudioSettingComponent = (GetComponent<AudioSettingCollider>() != null);
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi triggeringPlayer)
        {
            Debug.Log("[AudioManager] " + triggeringPlayer.displayName + "-" + triggeringPlayer.playerId.ToString() + " Entering Zone " + zoneID + "-" + gameObject.GetInstanceID());

            if (triggeringPlayer.isLocal)
            {
                localPlayerEnterCounter++;
            }
            
            triggeringPlayer.EnterAudioZone(_AudioZoneManager, zoneID, isNegativeZone);
            foreach (string id in transitionZoneIDs)
            {
                triggeringPlayer.EnterAudioZone(_AudioZoneManager, id, isNegativeZone);
            }

            _AudioZoneManager.UpdateAudioZoneSetting(triggeringPlayer, hasAudioSettingComponent);
        }
        public override void OnPlayerTriggerExit(VRCPlayerApi triggeringPlayer)
        {
            _HandlePlayerExiting(triggeringPlayer, 1);
        }

        private void _HandlePlayerExiting(VRCPlayerApi triggeringPlayer, int exitCount)
        {
            //When leaving a station it reactivates the Collider, which will also trigger OnPlayerTriggerEnter.
            //But while entering a station does deactivate the collider, it does not trigger OnPlayerTriggerExit.
            //if you enter & exit a station inside the AudioZoneCollider multiple times => you also enter the AudioZone multiple times
            // => we also have to exit it multiple times => amount > 1
            Debug.Log($"[AudioManager] {triggeringPlayer.displayName}-{triggeringPlayer.playerId} Exiting Zone {zoneID}-{gameObject.GetInstanceID()} with {nameof(exitCount)}:{exitCount}");

            if (triggeringPlayer.isLocal)
            {
                localPlayerEnterCounter = Mathf.Max(0, localPlayerEnterCounter - exitCount);
            }
            
            triggeringPlayer.ExitAudioZone(_AudioZoneManager, zoneID, isNegativeZone, exitCount);
            foreach (string id in transitionZoneIDs)
            {
                triggeringPlayer.ExitAudioZone(_AudioZoneManager, id, isNegativeZone, exitCount);
            }

            _AudioZoneManager.UpdateAudioZoneSetting(triggeringPlayer, hasAudioSettingComponent);
        }

        [NetworkCallable]
        public void NetworkCallableOnPlayerTriggerEnter(int playerId)
        {
            var player = VRCPlayerApi.GetPlayerById(playerId);
            if (!Utilities.IsValid(player) || !player.IsValid()) return;
            OnPlayerTriggerEnter(player);
        }

        [NetworkCallable]
        public void NetworkCallableOnPlayerTriggerExit(int playerId, int exitCount)
        {
            var player = VRCPlayerApi.GetPlayerById(playerId);
            if (!Utilities.IsValid(player) || !player.IsValid()) return;
            _HandlePlayerExiting(player, exitCount); 
        }

        public void OnLocalPlayerEnterStationFix()
        {
            if (localPlayerEnterCounter > 0)
            {
                //We have already entered the zone normally via OnPlayerTriggerEnter, which means we are in no station. Nothing to do here
                Debug.Log($"[AudioManager] Local Player is already in Zone {zoneID}, no need to send Network Event for {nameof(NetworkCallableOnPlayerTriggerEnter)}");
                return;
            }
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(NetworkCallableOnPlayerTriggerEnter), Networking.LocalPlayer.playerId);
        }

        public void OnLocalPlayerExitStationFix()
        {
            if (localPlayerEnterCounter <= 0)
            {
                //We already exited the zone as often as we entered it - nothing to do here.
                Debug.Log($"[AudioManager] Local Player is already out of Zone {zoneID}, no need to send Network Event for {nameof(NetworkCallableOnPlayerTriggerExit)}");
                return;
            }
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(NetworkCallableOnPlayerTriggerExit), Networking.LocalPlayer.playerId, localPlayerEnterCounter);
        }
    }
}
