using Sylan.AudioManager.EditorUtilities;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Sylan.AudioManager
{
    public static class AudioZoneManagerInitialize
    {
        public static bool RunOnBuild()
        {
            if (!SerializedPropertyUtils.TryFindSerializedObject(out AudioZoneManager manager, out SerializedObject managerSo)) return false;
            if (manager == null) return true;

            if (!TryGetPlayerObject(out AudioZonePlayerObject playerObject))
            {
                playerObject = CreatePlayerObject(manager);
            }

            SerializedPropertyUtils.PopulateSerializedProperty<AudioSettingManager>(managerSo, AudioZoneManager.AudioSettingManagerPropertyName);

            RunOnPlayerObjectBuild(playerObject, manager);
            AudioZoneSyncCore correctPlayerSync = PickAppropriateSyncScript(playerObject);

            if (!EnsureNoUnknownScriptInstances(playerObject)
                || !EnsureNoUnknownScriptInstances(correctPlayerSync))
            {
                return false;
            }

            return true;
        }

        public static bool TryGetPlayerObject(out AudioZonePlayerObject playerObject)
        {
            playerObject = Object.FindAnyObjectByType<AudioZonePlayerObject>(FindObjectsInactive.Include);
            return playerObject != null;
        }

        public static AudioZonePlayerObject CreatePlayerObject(AudioZoneManager manager)
        {
            GameObject go = new(nameof(AudioZonePlayerObject));
            Undo.RegisterCreatedObjectUndo(go, $"Create {nameof(AudioZonePlayerObject)}");
            go.transform.SetParent(manager.transform, worldPositionStays: false);
            return UdonSharpUndo.AddComponent<AudioZonePlayerObject>(go);
        }

        private static void RunOnPlayerObjectBuild(AudioZonePlayerObject playerObject, AudioZoneManager manager)
        {
            SerializedObject playerObjectSo = new(playerObject);
            playerObjectSo.FindProperty(AudioZonePlayerObject.AudioZoneManagerPropertyName).objectReferenceValue = manager;
            int layer = AudioZoneLayerInit.GetAudioZoneLayer(manager);
            playerObjectSo.FindProperty(AudioZonePlayerObject.AudioZoneColliderLayerMaskPropertyName).intValue = 1 << layer;
            playerObjectSo.ApplyModifiedProperties();
        }

        private static AudioZoneSyncCore PickAppropriateSyncScript(AudioZonePlayerObject playerObject)
        {
            int requiredSettingZoneBits = 0;
            while ((1u << requiredSettingZoneBits) <= ((uint)AudioSettingInitialize.zoneIdCount + 1u))
            {
                requiredSettingZoneBits++;
            }
            int totalRequiredBits = AudioZoneInitialize.zoneIdCount + requiredSettingZoneBits;
            int totalIdCount = AudioZoneInitialize.zoneIdCount + AudioSettingInitialize.zoneIdCount;

            System.Type scriptType = totalRequiredBits switch
            {
                <= 64 => typeof(BitField64AudioZoneSync),
                <= 128 => typeof(BitField128AudioZoneSync),
                <= 192 => typeof(BitField192AudioZoneSync),
                _ => null,
            };
            scriptType ??= totalIdCount switch
            {
                <= ushort.MaxValue => typeof(ShortAudioZoneSync),
                _ => typeof(IntegerAudioZoneSync),
            };

            AudioZoneSyncCore existingScript = playerObject.GetComponentInChildren<AudioZoneSyncCore>();
            if (existingScript?.GetType() == scriptType) return existingScript;

            GameObject syncGo = existingScript?.gameObject;
            if (existingScript != null)
            {
                UdonSharpUndo.DestroyImmediate(existingScript);
            }

            if (syncGo == null)
            {
                syncGo = new("AudioZoneSync");
                syncGo.transform.SetParent(playerObject.transform, worldPositionStays: false);
                Undo.RegisterCreatedObjectUndo(syncGo, "Create AudioZoneSync");
            }

            return (AudioZoneSyncCore)UdonSharpUndo.AddComponent(syncGo, scriptType);
        }

        private static bool EnsureNoUnknownScriptInstances<T>(T validInstance)
            where T : UdonSharpBehaviour
        {
            bool valid = true;
            foreach (T script in SerializedPropertyUtils.FindAllObjects<T>())
            {
                if (script == validInstance) continue;

                Debug.LogError($"[AudioManager] Manually or unintentionally created {typeof(T).Name} script instance found. Should be deleted.", script);
                valid = false;
            }
            return valid;
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioZoneManager))]
    public class AudioZoneManagerEditor : Editor
    {
        private SerializedProperty fallbackLayerIndexProp;

        private void OnEnable()
        {
            fallbackLayerIndexProp = serializedObject.FindProperty(nameof(AudioZoneManager.fallbackLayerIndex));
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets))
                return;

            serializedObject.Update();
            AudioZoneManagerMigrator.SingletonInstance.DrawMigrationInfoInInspector(serializedObject, targets);
            DrawPropertiesExcluding(serializedObject, "m_Script", nameof(AudioZoneManager.fallbackLayerIndex));

            if (!AudioZoneLayerInit.AudioZoneLayerExists())
            {
                DrawDefaultLayerSettings();
            }
            serializedObject.ApplyModifiedProperties();

            // A button for the user to create the AudioZonePlayerObject immediately just for clarity that it is needed.
            if (!AudioZoneManagerInitialize.TryGetPlayerObject(out _))
            {
                EditorGUILayout.Space();
                if (GUILayout.Button(new GUIContent(
                    $"Create {nameof(AudioZonePlayerObject)}",
                    "This is required and will be created automatically upon entering play mode or publishing the world.")))
                {
                    AudioZoneManagerInitialize.CreatePlayerObject((AudioZoneManager)targets[0]);
                }
            }
        }

        private void DrawDefaultLayerSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"When no collision layer called '{AudioZoneLayerInit.LayerName}' "
                + $"is defined Audio Zone Colliders and Audio Setting Colliders will use the layer defined below.",
                MessageType.Info);

            // When changing the recommended layers here, also update the Reset function for the AudioZoneManager.
            SerializedPropertyUtils.DrawLayerField(fallbackLayerIndexProp, new GUIContent(
                "Audio Zones Layer",
                "It is best to use a layer which is hardly used by anything else. Out of VRChat's default layers "
                    + "several are internal to VRChat and it's likely best not to use them. "
                    + "The more fitting might be 'Ignore Raycast' or 'Environment', depending on which of "
                    + "those layers is used less in the current scene."));

            if (GUILayout.Button(new GUIContent(
                "Initialize AudioZones Layer",
                "When not all custom layers are used in a project, it is likely best to dedicate one of them to "
                    + "audio zones to keep the system's physics checks as performance light as they could be.")))
            {
                AudioZoneLayerInit.ShowWindow();
            }
        }
    }
}
