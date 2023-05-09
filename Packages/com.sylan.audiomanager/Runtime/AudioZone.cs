
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.SDKBase.Midi;
using VRC.Udon;
using static VRC.Core.APIGroup;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioZone : UdonSharpBehaviour
    {
        [SerializeField] private AudioZoneManager _AudioZoneManager;
        public const string AudioZoneManagerPropertyName = nameof(_AudioZoneManager);

        [Header("Primary AudioZone ID")]
        public string zoneID = string.Empty;

        [Header("Additional AudioZones. Useful for transitions.", order = 0)]
        [Space(-10, order = 1)]
        [Header("To hear players who are not in a zone, set an empty string.", order = 2)]
        public string[] transitionZoneIDs;

        public override void OnPlayerTriggerEnter(VRCPlayerApi triggeringPlayer)
        {
            Debug.Log("[AudioManager] Entering Zone " + zoneID + gameObject.GetInstanceID());

            triggeringPlayer.EnterZone(_AudioZoneManager, zoneID);
            foreach (string id in transitionZoneIDs)
            {
                triggeringPlayer.EnterZone(_AudioZoneManager, id);
            }

            _AudioZoneManager.UpdateAudioSettings(triggeringPlayer);
        }
        public override void OnPlayerTriggerExit(VRCPlayerApi triggeringPlayer)
        {
            Debug.Log("[AudioManager] Exiting Zone " + zoneID + gameObject.GetInstanceID());

            triggeringPlayer.ExitZone(_AudioZoneManager, zoneID);
            foreach (string id in transitionZoneIDs)
            {
                triggeringPlayer.ExitZone(_AudioZoneManager, id);
            }

            _AudioZoneManager.UpdateAudioSettings(triggeringPlayer);
        }
    }
}
