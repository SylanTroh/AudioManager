using System.Collections.Generic;
using System.Linq;
using Sylan.AudioManager.EditorUtilities;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Sylan.AudioManager
{
    [CustomEditor(typeof(AudioZoneCollider))]
    public class AudioZoneEditor : ZoneEditor
    {
        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Pickable)]
        private static void DrawGizmos(AudioZoneCollider audioZone, GizmoType gizmoType)
        {
            DrawColliderGizmos(audioZone, Color.cyan);
        }
    }

    [InitializeOnLoad]
    public class AudioZoneInitialize : IVRCSDKBuildRequestedCallback
    {
        public static int zoneIdCount;

        private static bool RunAllOnBuild()
        {
            return RunOnBuild()
                && AudioSettingInitialize.RunOnBuild()
                && AudioZoneManagerInitialize.RunOnBuild()
                && AudioZoneManagerKillSwitchInitialize.RunOnBuild();
        }

        /// <summary>
        /// <para>Affects all but <see cref="MeshCollider"/>s, in case a zone component is on the same object
        /// as a visible mesh.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="components"></param>
        public static void MakeAllAttachedCollidersTriggers<T>(T[] components)
            where T : Component
        {
            SerializedObject collidersSo = new(components
                .SelectMany(z => z.GetComponents<Collider>())
                .Where(c => c is not MeshCollider)
                .ToArray());
            collidersSo.FindProperty("m_IsTrigger").boolValue = true;
            collidersSo.ApplyModifiedProperties();
        }

        private static bool RunOnBuild()
        {
            if (!SerializedPropertyUtils.GetObjects<AudioZoneCollider>(out AudioZoneCollider[] audioZones)) return false;
            if (audioZones.Length == 0) return true;
            if (!SerializedPropertyUtils.GetObject<AudioZoneManager>(out var audioZoneManager)) return false;

            var zoneIdDict = new Dictionary<string, int> { { string.Empty, AudioZoneManager.EmptyZoneIdIndex } };

            AudioZoneLayerInit.TryFindAudioZoneLayer(out var collisionLayer, audioZoneManager);

            foreach (var audioZone in audioZones)
            {
                audioZone.gameObject.layer = collisionLayer;
                PopulateGeneratedIds(zoneIdDict, audioZone);
            }

            MakeAllAttachedCollidersTriggers(audioZones);

            zoneIdCount = zoneIdDict.Count;

            if (audioZoneManager != null)
            {
                audioZoneManager.totalAudioZonesCount = zoneIdCount;
                var shift = zoneIdCount % 64;
                audioZoneManager.audioSettingsIndexBitShift = shift;
                audioZoneManager.audioSettingsIndexBitMask = ulong.MaxValue << shift;

                audioZoneManager.ZoneIdMapping = new string[zoneIdDict.Count];
                foreach (var keyValuePair in zoneIdDict)
                {
                    audioZoneManager.ZoneIdMapping[keyValuePair.Value] = keyValuePair.Key;
                }
            }

            return true;
        }

        private static void PopulateGeneratedIds(Dictionary<string, int> zoneIdDict, AudioZoneCollider audioZone)
        {
            // TODO: Use SerializedObject

            ulong field1 = 0uL;
            ulong field2 = 0uL;
            ulong field3 = 0uL;

            audioZone.zoneIdIndex = GetOrAdd(zoneIdDict, audioZone.zoneID);
            AddIdAsFlag(ref field1, ref field2, ref field3, audioZone.zoneIdIndex);

            audioZone.transitionZoneIdIndexes = new int[audioZone.transitionZoneIDs.Length];
            for (var i = 0; i < audioZone.transitionZoneIDs.Length; i++)
            {
                int zoneIdIndex = GetOrAdd(zoneIdDict, audioZone.transitionZoneIDs[i]);
                audioZone.transitionZoneIdIndexes[i] = zoneIdIndex;
                AddIdAsFlag(ref field1, ref field2, ref field3, zoneIdIndex);
            }

            audioZone.combinedZoneIdsField1 = field1;
            audioZone.combinedZoneIdsField2 = field2;
            audioZone.combinedZoneIdsField3 = field3;
        }

        private static void AddIdAsFlag(ref ulong field1, ref ulong field2, ref ulong field3, int zoneId)
        {
            AddIdAsFlag(ref field1, 0, zoneId);
            AddIdAsFlag(ref field2, 64, zoneId);
            AddIdAsFlag(ref field3, 128, zoneId);
        }

        private static void AddIdAsFlag(ref ulong field, int baseShift, int zoneId)
        {
            if (zoneId < baseShift || baseShift + 64 <= zoneId)
                return;
            field |= 1uL << (zoneId - baseShift);
        }

        private static int GetOrAdd(Dictionary<string, int> zoneIdDict, string zoneId)
        {
            if (zoneIdDict.TryGetValue(zoneId, out var value)) return value;

            value = zoneIdDict.Count;
            zoneIdDict.Add(zoneId, value);
            return value;
        }

        //
        //Run On Play
        //
        static AudioZoneInitialize()
        //Rename Static Constructor to match Class name
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            if (!RunAllOnBuild())
            {
                EditorApplication.isPlaying = false;
            }
        }
        //
        // Run On Build
        //
        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType != VRCSDKRequestedBuildType.Scene) return false;
            return RunAllOnBuild();
        }
    }

    public class AudioZoneColliderProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            //This will only temporary remove the string ZoneIds before PlayMode & upload. We dont need them anymore and can save some memory
            foreach (var audioZoneCollider in Object.FindObjectsByType<AudioZoneCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                audioZoneCollider.zoneID = string.Empty;
                audioZoneCollider.transitionZoneIDs = System.Array.Empty<string>();
            }
        }
    }
}
