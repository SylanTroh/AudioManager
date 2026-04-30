#if !COMPILER_UDONSHARP && UNITY_EDITOR
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

            //Object with Serialized Property(s)
            if (!SerializedPropertyUtils.GetSerializedObject<AudioZoneManager>(out SerializedObject serializedObject)) return false;
            if (serializedObject == null) return true;
            IgnoreAllLayerCollisionForAudioZoneLayer(serializedObject);

            // Get the AudioZoneManager instance
            AudioZoneManager manager = (AudioZoneManager)serializedObject.targetObject;
            if (!TryGetPlayerObject(out var playerObject))
            {
                playerObject = CreatePlayerObject(manager);
            }

            //Set Serialized Property
            SerializedPropertyUtils.PopulateSerializedProperty<AudioSettingManager>(serializedObject, AudioZoneManager.AudioSettingManagerPropertyName);

            RunOnPlayerObjectBuild(playerObject, manager);
            AudioZoneSyncCore correctPlayerSync = PickAppropriateSyncScript(playerObject);

            if (!EnsureNoUnknownScriptInstances(playerObject)
                || !EnsureNoUnknownScriptInstances(correctPlayerSync))
            {
                return false;
            }

            return true;
        }

        private static void IgnoreAllLayerCollisionForAudioZoneLayer(SerializedObject serializedObject)
        {
            if (!AudioZoneLayerInit.TryFindAudioZoneLayer(out var layerIndex, serializedObject)) return;

            AudioZoneLayerInit.IgnoreAllLayerCollision(layerIndex);
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
            AudioZoneLayerInit.TryFindAudioZoneLayer(out var layer, manager);
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
            foreach (T script in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets))
                return;
            EditorGUILayout.Space();

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            
            if (!AudioZoneLayerInit.TryFindAudioZoneLayer(out var layerIndex, serializedObject))
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
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);  
            EditorGUILayout.HelpBox(
                "AudioZones Layer could not be found. Either select a Layer to use or Init AudioZones layer",
                MessageType.Info
            );
                
            EditorGUI.BeginChangeCheck();
            
            var defaultLayerIndexProp = serializedObject.FindProperty(nameof(AudioZoneManager.DefaultLayerIndex));
            var newLayer = EditorGUILayout.LayerField(
                "Default Layer",
                defaultLayerIndexProp.intValue
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                defaultLayerIndexProp.intValue = newLayer;
            }
            
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent(
                    "Init AudioZones Layer",
                    "This is an optional but highly recommended step to improve performance by preventing unnecessary collisions.")))
            {
                AudioZoneLayerInit.ShowWindow();
            }
                
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
    }
}
#endif