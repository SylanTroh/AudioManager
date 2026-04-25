using UdonSharp;
using VRC.SDK3.Data;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class AudioZoneSyncArrayCore : AudioZoneSyncCore
    {
        private DataDictionary audioZoneIds = new DataDictionary();
        private DataDictionary oldAudioZoneIds = new DataDictionary();

        private DataDictionary negativeZoneIds = new DataDictionary();
        private DataDictionary oldNegativeZoneIds = new DataDictionary();

        private DataDictionary finalAudioZoneIds = new DataDictionary();
        private DataDictionary oldFinalAudioZoneIds = new DataDictionary();

        private bool hasZonesChanged;

        public override void OnValidateAudioZonesStart()
        {
            base.OnValidateAudioZonesStart();
            hasZonesChanged = false;
        }

        public override void NotifyHitAudioZoneCollider(AudioZoneCollider audioZoneCollider)
        {
            if (audioZoneCollider.isNegativeZone)
            {
                AddZoneIds(audioZoneCollider, oldNegativeZoneIds, negativeZoneIds, ref hasZonesChanged);
            }
            else
            {
                AddZoneIds(audioZoneCollider, oldAudioZoneIds, audioZoneIds, ref hasZonesChanged);
            }
        }

        protected abstract void InternalOnPreSerialization(int[] audioZonesIndexes, int audioSettingIndex);

        protected override void InternalOnPreSerialization(int audioSettingIndex)
        {
            InternalOnPreSerialization(GetAllKeysArray(oldFinalAudioZoneIds), audioSettingIndex);
        }

        public override bool HasZoneChanged()
        {
            hasZonesChanged = hasZonesChanged || oldAudioZoneIds.Count != audioZoneIds.Count
                                              || oldNegativeZoneIds.Count != negativeZoneIds.Count;

            if (hasZonesChanged)
            {
                hasZonesChanged = false;
                var keys = audioZoneIds.GetKeys();
                for (var i = 0; i < keys.Count; i++)
                {
                    if (!negativeZoneIds.ContainsKey(keys[i].Int))
                    {
                        AddZoneId(keys[i].Int, oldFinalAudioZoneIds, finalAudioZoneIds, ref hasZonesChanged);
                    }
                }

                hasZonesChanged = hasZonesChanged || oldFinalAudioZoneIds.Count != finalAudioZoneIds.Count;
                SwapDictionaries(ref finalAudioZoneIds, ref oldFinalAudioZoneIds);
            }

            SwapDictionaries(ref audioZoneIds, ref oldAudioZoneIds);
            SwapDictionaries(ref negativeZoneIds, ref oldNegativeZoneIds);

            return hasZonesChanged || base.HasZoneChanged();
        }

        private void SwapDictionaries(ref DataDictionary newDict, ref DataDictionary oldDict)
        {
            var tmpSwappingDict = oldDict;
            oldDict = newDict;
            newDict = tmpSwappingDict;
            newDict.Clear();
        }

        private void AddZoneIds(AudioZoneCollider audioZoneCollider, DataDictionary oldDict, DataDictionary newDict, ref bool hasChanged)
        {
            AddZoneId(audioZoneCollider.zoneIdIndex, oldDict, newDict, ref hasChanged);
            foreach (var zoneId in audioZoneCollider.transitionZoneIdIndexes)
            {
                AddZoneId(zoneId, oldDict, newDict, ref hasChanged);
            }
        }

        private void AddZoneId(int zoneId, DataDictionary oldDict, DataDictionary newDict, ref bool hasChanged)
        {
            if (!oldDict.ContainsKey(zoneId))
            {
                hasChanged = true;
            }

            newDict.SetValue(zoneId, true);
        }

        private int[] GetAllKeysArray(DataDictionary dict)
        {
            var keys = new int[dict.Count];
            var list = dict.GetKeys();
            list.Sort(); // Sorted such that 1) empty id goes first and 2) binary searches can be used.

            for (var i = 0; i < list.Count; i++)
            {
                keys[i] = list[i].Int;
            }

            return keys;
        }
    }
}