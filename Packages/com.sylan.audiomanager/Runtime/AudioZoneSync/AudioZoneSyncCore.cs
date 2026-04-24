using UdonSharp;
using VRC.SDK3.Data;
using VRC.SDKBase;
using Debug = UnityEngine.Debug;

namespace Sylan.AudioManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class AudioZoneSyncCore : UdonSharpBehaviour
    {
        public VRCPlayerApi OwningPlayer;

        protected AudioZoneManager AudioZoneManager;
        protected AudioZonePlayerObject AudioZonePlayerObject;

        private DataDictionary audioSettingIds = new DataDictionary();
        private DataDictionary oldAudioSettingIds = new DataDictionary();

        private DataDictionary audioZoneIds = new DataDictionary();
        private DataDictionary oldAudioZoneIds = new DataDictionary();

        private DataDictionary negativeZoneIds = new DataDictionary();
        private DataDictionary oldNegativeZoneIds = new DataDictionary();

        private DataDictionary finalAudioZoneIds = new DataDictionary();
        private DataDictionary oldFinalAudioZoneIds = new DataDictionary();

        private bool hasZonesChanged;

        protected abstract void InternalOnPreSerialization(int[] audioZonesIndexes, int[] audioSettingsIndexes);

        public abstract bool SharesAudioZoneWith(AudioZoneSyncCore other);

        private void Start()
        {
            AudioZonePlayerObject = transform.parent.GetComponent<AudioZonePlayerObject>();
            AudioZoneManager = AudioZonePlayerObject.AudioZoneManager;
            if (AudioZoneManager == null)
            {
                Debug.Log($"{nameof(AudioZoneSyncCore)} has no {nameof(AudioZoneManager)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            AudioZoneManager.Register(this);
            OwningPlayer = Networking.GetOwner(gameObject);
        }

        private void OnDestroy()
        {
            AudioZoneManager.Deregister(this);
        }

        public void OnValidateAudioZonesStart()
        {
            hasZonesChanged = false;
            negativeZoneIds.Clear();
        }

        public void NotifyHitAudioZoneCollider(AudioZoneCollider audioZoneCollider)
        {
            if (audioZoneCollider.isNegativeZone)
            {
                AddZoneIds(audioZoneCollider, oldNegativeZoneIds, negativeZoneIds);
            }
            else
            {
                AddZoneIds(audioZoneCollider, oldAudioZoneIds, audioZoneIds);
            }
        }

        public void NotifyAudioSettingCollider(AudioSettingCollider audioSettingCollider)
        {
            AddZoneId(audioSettingCollider.SettingIndex, oldAudioSettingIds, audioSettingIds);
        }

        public override void OnPreSerialization()
        {
            InternalOnPreSerialization(GetAllKeysArray(oldFinalAudioZoneIds, true), GetAllKeysArray(oldAudioZoneIds));
        }

        public override void OnDeserialization()
        {
            AudioZoneManager.UpdateAudioZoneSetting(this);
        }

        public virtual void OnZoneChanged()
        {
            OnPreSerialization(); // TODO remove, just here for testing
            RequestSerialization();
            AudioZoneManager.UpdateAudioZoneSetting(this);
        }

        public bool HasZoneChanged()
        {
            hasZonesChanged = hasZonesChanged || oldAudioZoneIds.Count != audioZoneIds.Count
                                              || oldAudioSettingIds.Count != audioSettingIds.Count
                                              || oldNegativeZoneIds.Count != negativeZoneIds.Count;

            if (hasZonesChanged)
            {
                hasZonesChanged = false;
                var keys = audioZoneIds.GetKeys();
                for (var i = 0; i < keys.Count; i++)
                {
                    if (!negativeZoneIds.ContainsKey(keys[i].Int))
                    {
                        AddZoneId(keys[i].Int, oldFinalAudioZoneIds, finalAudioZoneIds);
                    }
                }

                hasZonesChanged = hasZonesChanged || oldFinalAudioZoneIds.Count != finalAudioZoneIds.Count;
                SwapDictionaries(ref finalAudioZoneIds, ref oldFinalAudioZoneIds);
            }

            SwapDictionaries(ref audioZoneIds, ref oldAudioZoneIds);
            SwapDictionaries(ref audioSettingIds, ref oldAudioSettingIds);
            SwapDictionaries(ref negativeZoneIds, ref oldNegativeZoneIds);

            return hasZonesChanged;
        }

        private void SwapDictionaries(ref DataDictionary newDict, ref DataDictionary oldDict)
        {
            var tmpSwappingDict = oldDict;
            oldDict = newDict;
            newDict = tmpSwappingDict;
            tmpSwappingDict.Clear();
        }

        private void AddZoneIds(AudioZoneCollider audioZoneCollider, DataDictionary oldDict, DataDictionary newDict)
        {
            AddZoneId(audioZoneCollider.zoneIdIndex, oldDict, newDict);
            foreach (var zoneId in audioZoneCollider.transitionZoneIdIndexes)
            {
                AddZoneId(zoneId, oldDict, newDict);
            }
        }

        private void AddZoneId(int zoneId, DataDictionary oldDict, DataDictionary newDict)
        {
            if (!oldDict.ContainsKey(zoneId))
            {
                hasZonesChanged = true;
            }

            newDict.SetValue(zoneId, true);
        }

        private int[] GetAllKeysArray(DataDictionary dict, bool sort = false)
        {
            var keys = new int[dict.Count];
            var list = dict.GetKeys();
            if (sort)
            {
                list.Sort();
            }

            for (var i = 0; i < list.Count; i++)
            {
                keys[i] = list[i].Int;
            }

            return keys;
        }
    }
}