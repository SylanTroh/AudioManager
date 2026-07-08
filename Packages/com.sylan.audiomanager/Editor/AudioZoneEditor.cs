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
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioZoneCollider))]
    public class AudioZoneEditor : ZoneEditor
    {
        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Pickable)]
        private static void DrawGizmos(AudioZoneCollider audioZone, GizmoType gizmoType)
        {
            DrawColliderGizmos(audioZone, Color.cyan);
        }

        protected override Migrator GetMigrator() => AudioZoneColliderMigrator.SingletonInstance;
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
        public static void MakeAllAttachedPrimitiveCollidersTriggers<T>(T[] components)
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
            AudioZoneCollider[] audioZones = SerializedPropertyUtils.FindAllObjects<AudioZoneCollider>();
            if (audioZones.Length == 0) return true; // TODO: Reset zoneIdCount.
            if (!SerializedPropertyUtils.TryFindObject(out AudioZoneManager audioZoneManager)) return false;

            if (AudioZoneLayerInit.TryGetAudioZoneLayer(out int audioZoneLayer, audioZoneManager))
            {
                SerializedPropertyUtils.SetLayerAndApply(audioZones.Select(z => z.gameObject).ToArray(), audioZoneLayer);
            }

            MakeAllAttachedPrimitiveCollidersTriggers(audioZones);

            Dictionary<string, int> zoneIdDict = new() { { string.Empty, AudioZoneManager.EmptyZoneIdIndex } };
            foreach (AudioZoneCollider audioZone in audioZones)
            {
                PopulateGeneratedIds(zoneIdDict, audioZone);
            }

            zoneIdCount = zoneIdDict.Count;

            if (audioZoneManager != null)
            {
                SerializedObject so = new(audioZoneManager);

                so.FindProperty(nameof(AudioZoneManager.totalAudioZonesCount)).intValue = zoneIdCount;
                int shift = zoneIdCount % 64;
                so.FindProperty(nameof(AudioZoneManager.audioSettingsIndexBitShift)).intValue = shift;
                so.FindProperty(nameof(AudioZoneManager.audioSettingsIndexBitMask)).ulongValue = ulong.MaxValue << shift;

                string[] zoneIdMapping = new string[zoneIdDict.Count];
                foreach (var kvp in zoneIdDict)
                {
                    zoneIdMapping[kvp.Value] = kvp.Key;
                }
                SerializedPropertyUtils.SetArrayProperty(
                    so.FindProperty(nameof(audioZoneManager.zoneIdMapping)),
                    zoneIdMapping,
                    (p, v) => p.stringValue = v);

                so.ApplyModifiedProperties();
            }

            return true;
        }

        private static void PopulateGeneratedIds(Dictionary<string, int> zoneIdDict, AudioZoneCollider audioZone)
        {
            SerializedObject so = new(audioZone);
            ulong field1 = 0uL;
            ulong field2 = 0uL;
            ulong field3 = 0uL;

            int zoneIdIndex = GetOrAdd(zoneIdDict, audioZone.zoneID);
            so.FindProperty(nameof(AudioZoneCollider.zoneIdIndex)).intValue = zoneIdIndex;
            AddIdAsFlag(ref field1, ref field2, ref field3, zoneIdIndex);

            int[] transitionZoneIdIndexes = new int[audioZone.transitionZoneIDs.Length];
            for (int i = 0; i < audioZone.transitionZoneIDs.Length; i++)
            {
                zoneIdIndex = GetOrAdd(zoneIdDict, audioZone.transitionZoneIDs[i]);
                transitionZoneIdIndexes[i] = zoneIdIndex;
                AddIdAsFlag(ref field1, ref field2, ref field3, zoneIdIndex);
            }
            SerializedPropertyUtils.SetArrayProperty(
                so.FindProperty(nameof(AudioZoneCollider.transitionZoneIdIndexes)),
                transitionZoneIdIndexes,
                (p, v) => p.intValue = v);

            so.FindProperty(nameof(AudioZoneCollider.combinedZoneIdsField1)).ulongValue = field1;
            so.FindProperty(nameof(AudioZoneCollider.combinedZoneIdsField2)).ulongValue = field2;
            so.FindProperty(nameof(AudioZoneCollider.combinedZoneIdsField3)).ulongValue = field3;

            so.ApplyModifiedProperties();
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
            if (zoneIdDict.TryGetValue(zoneId, out int value)) return value;

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
            foreach (AudioZoneCollider audioZoneCollider in SerializedPropertyUtils.FindAllObjects<AudioZoneCollider>())
            {
                audioZoneCollider.zoneID = string.Empty;
                audioZoneCollider.transitionZoneIDs = System.Array.Empty<string>();
            }
        }
    }
}
