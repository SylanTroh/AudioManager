using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class IntegerAudioZonePlayerObjectSync : AbstractAudioZonePlayerObjectSync
    {
        [UdonSynced, SerializeField] private int[] AudioZones = Array.Empty<int>();

        protected override void InternalOnPreSerialization(int[] audioZonesIndexes, int[] audioSettingsIndexes)
        {
        }

        protected override bool SharesAudioZoneWith(AbstractAudioZonePlayerObjectSync other)
        {
            //TODO
            return true;
        }

        protected override void NotifyAudioManager(VRCPlayerApi player)
        {
            AudioZoneManager.ClearAudioZones(player);
            foreach (var audioZone in AudioZones)
            {
                AudioZoneManager.EnterAudioZone(player, audioZone, false);
            }

            AudioZoneManager.UpdateAudioZoneSetting(player);
        }

        public override void OnZoneChanged()
        {
            OnPreSerialization(); //TODO remove, just here for testing
            base.OnZoneChanged();
        }
    }
}