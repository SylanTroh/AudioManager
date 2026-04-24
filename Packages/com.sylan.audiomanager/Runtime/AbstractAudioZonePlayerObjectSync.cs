using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using Debug = UnityEngine.Debug;

namespace Sylan.AudioManager
{
    [RequireComponent(typeof(AudioZonePlayerObject))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class AbstractAudioZonePlayerObjectSync : UdonSharpBehaviour
    {
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

        protected VRCPlayerApi localPlayer;
        private bool hasZonesChanged;

        protected abstract void NotifyAudioManager(VRCPlayerApi player);
        protected abstract void InternalOnPreSerialization(int[] audioZonesIndexes, int[] audioSettingsIndexes);
        
        protected abstract bool SharesAudioZoneWith(AbstractAudioZonePlayerObjectSync other);

        private void Start()
        {
            AudioZonePlayerObject = GetComponent<AudioZonePlayerObject>();
            AudioZoneManager = GetComponent<AudioZonePlayerObject>().AudioZoneManager;
            if (AudioZoneManager == null)
            {
                Debug.Log($" has no {nameof(AudioZoneManager)}.");
                enabled = false;
                gameObject.SetActive(false);
                return;
            }

            if (!Networking.IsOwner(gameObject)) return;

            localPlayer = Networking.LocalPlayer;
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
            InternalOnPreSerialization(GetAllKeysArray(oldFinalAudioZoneIds), GetAllKeysArray(oldAudioZoneIds));
        }

        public override void OnDeserialization()
        {
            if (Networking.IsOwner(gameObject)) return;
            var owner = Networking.GetOwner(gameObject);
            NotifyAudioManager(owner);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal) return;
            RequestSerialization();
        }

        public virtual void OnZoneChanged()
        {
            RequestSerialization();
            NotifyAudioManager(localPlayer);
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

        private int[] GetAllKeysArray(DataDictionary dict)
        {
            var keys = new int[dict.Count];
            var list = dict.GetKeys();
            for (var i = 0; i < list.Count; i++)
            {
                keys[i] = list[i].Int;
            }

            return keys;
        }
    }
}