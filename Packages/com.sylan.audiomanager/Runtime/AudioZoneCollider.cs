
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [AddComponentMenu("Scrips/Audio Zone Collider")]
    public class AudioZoneCollider : UdonSharpBehaviour
    {
        [Header("Primary AudioZone ID")]
        /// <summary>
        /// Dont use this zoneId on runTime. It will be cleared.
        /// Use <c>zoneIdIndex</c> instead.
        /// Iu need the name, use <c>_AudioZoneManager.ZoneIdMapping[zoneIdIndex]</c>.
        /// </summary>
        public string zoneID = string.Empty;
        [HideInInspector] public int zoneIdIndex;

        [Header("Additional AudioZones. Useful for transitions.", order = 0)]
        [Space(-10, order = 1)]
        [Header("To match players who are not in a zone, set an empty string.", order = 2)]
        /// <summary>
        /// Do not use these zone IDs at runtime. They will be cleared.
        /// Use <c>transitionZoneIdIndexes</c> instead.
        /// If you need the name, use <c>_AudioZoneManager.ZoneIdMapping[index]</c>.
        /// </summary>
        public string[] transitionZoneIDs;
        [HideInInspector] public int[] transitionZoneIdIndexes;

        [HideInInspector, SerializeField] private AudioZoneManager _AudioZoneManager;
        public const string AudioZoneManagerPropertyName = nameof(_AudioZoneManager);

        private bool hasAudioSettingComponent = false;

        public bool isNegativeZone = false;

        private void Start()
        {
            hasAudioSettingComponent = (GetComponent<AudioSettingCollider>() != null);
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi triggeringPlayer)
        {
            Debug.Log("[AudioManager] " + triggeringPlayer.displayName + "-" + triggeringPlayer.playerId.ToString() + " Entering Zone " + _AudioZoneManager.ZoneIdMapping[zoneIdIndex] + "-" + gameObject.GetInstanceID());

            triggeringPlayer.EnterAudioZone(_AudioZoneManager, zoneIdIndex, isNegativeZone);
            foreach (var id in transitionZoneIdIndexes)
            {
                triggeringPlayer.EnterAudioZone(_AudioZoneManager, id, isNegativeZone);
            }

            _AudioZoneManager.UpdateAudioZoneSetting(triggeringPlayer, hasAudioSettingComponent);
        }
        
        public override void OnPlayerTriggerExit(VRCPlayerApi triggeringPlayer)
        {
            Debug.Log("[AudioManager] " + triggeringPlayer.displayName + "-" + triggeringPlayer.playerId.ToString() + " Exiting Zone " + _AudioZoneManager.ZoneIdMapping[zoneIdIndex] + "-" + gameObject.GetInstanceID());

            triggeringPlayer.ExitAudioZone(_AudioZoneManager, zoneIdIndex, isNegativeZone);
            foreach (var id in transitionZoneIdIndexes)
            {
                triggeringPlayer.ExitAudioZone(_AudioZoneManager, id, isNegativeZone);
            }

            _AudioZoneManager.UpdateAudioZoneSetting(triggeringPlayer, hasAudioSettingComponent);
        }
    }
}
