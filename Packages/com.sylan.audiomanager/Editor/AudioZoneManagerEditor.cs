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
            IgnoreAllLayerCollisionForAudioZoneLayer();

            //Object with Serialized Property(s)
            if (!SerializedPropertyUtils.GetSerializedObject<AudioZoneManager>(out SerializedObject serializedObject)) return false;
            if (serializedObject == null) return true;

            // Get the AudioZoneManager instance
            AudioZoneManager manager = (AudioZoneManager)serializedObject.targetObject;
            if (!TryGetPlayerObject(out var playerObject))
            {
                playerObject = CreatePlayerObject(manager);
            }

            //Set Serialized Property
            SerializedPropertyUtils.PopulateSerializedProperty<AudioSettingManager>(serializedObject, AudioZoneManager.AudioSettingManagerPropertyName);

            RunOnPlayerObjectBuild(playerObject);
            AudioZoneSyncCore correctPlayerSync = PickAppropriateSyncScript(playerObject);

            if (!EnsureNoUnknownScriptInstances(playerObject)
                || !EnsureNoUnknownScriptInstances(correctPlayerSync))
            {
                return false;
            }

            return true;
        }

        private static void IgnoreAllLayerCollisionForAudioZoneLayer()
        {
            int layerIndex = AudioZoneLayerInit.FindAudioZoneLayer(doLogWarning: false);
            if (layerIndex == -1) return;

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

            AudioZonePlayerObject playerObject = UdonSharpUndo.AddComponent<AudioZonePlayerObject>(go);

            // Immediately populate the manager both for clarity for the user
            // as well as not having to rely on order of operations during on build.
            SerializedObject playerObjectSo = new(playerObject);
            playerObjectSo.FindProperty(AudioZonePlayerObject.AudioZoneManagerPropertyName).objectReferenceValue = manager;
            playerObjectSo.ApplyModifiedProperties();
            return playerObject;
        }

        private static void RunOnPlayerObjectBuild(AudioZonePlayerObject playerObject)
        {
            SerializedPropertyUtils.PopulateSerializedProperty<AudioZoneManager>(new(playerObject), AudioZonePlayerObject.AudioZoneManagerPropertyName);
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

                Debug.LogError($"Manually or unintentionally created {typeof(T).Name} script instance found. Should be deleted.", script);
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
    }
}
#endif