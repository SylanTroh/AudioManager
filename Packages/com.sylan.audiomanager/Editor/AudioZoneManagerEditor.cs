#if !COMPILER_UDONSHARP && UNITY_EDITOR
using Sylan.AudioManager.EditorUtilities;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Sylan.AudioManager
{
    [InitializeOnLoad]
    public class AudioZoneManagerInitialize : IVRCSDKBuildRequestedCallback
    {
        private static bool SetSerializedProperties()
        {
            //Object with Serialized Property(s)
            if (!SerializedPropertyUtils.GetSerializedObject<AudioZoneManager>(out SerializedObject serializedObject)) return false;

            // Get the AudioZoneManager instance
            AudioZoneManager manager = serializedObject?.targetObject as AudioZoneManager;
            if (manager != null && IsMissingPlayerObject())
            {
                CreatePlayerObject(manager);
            }

            //Set Serialized Property
            SerializedPropertyUtils.PopulateSerializedProperty<AudioSettingManager>(serializedObject, AudioZoneManager.AudioSettingManagerPropertyName);
            return true;
        }

        public static bool IsMissingPlayerObject()
        {
            return Object.FindAnyObjectByType<AudioZonePlayerObject>(FindObjectsInactive.Include) == null;
        }

        public static void CreatePlayerObject(AudioZoneManager manager)
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
        }

        //
        //Run On Play
        //
        static AudioZoneManagerInitialize()
        //Rename Static Constructor to match Class name
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            SetSerializedProperties();
        }
        //
        // Run On Build
        //
        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType != VRCSDKRequestedBuildType.Scene) return false;
            return SetSerializedProperties();
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
            if (AudioZoneManagerInitialize.IsMissingPlayerObject())
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