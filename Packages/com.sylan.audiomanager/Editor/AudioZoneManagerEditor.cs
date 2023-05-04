#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Sylan.AudioManager
{
    [InitializeOnLoad]
    public class AudioZoneManagerEditor : Editor, IVRCSDKBuildRequestedCallback
    {
        static void PopulateAudioZones(SerializedObject serializedObject, AudioZoneManager audioZoneManager)
        {
            SerializedProperty audioZonesProp;

            audioZonesProp = serializedObject.FindProperty("SerializedAudioZones");

            // Get all the AudioZone components in the scene
            AudioZone[] audioZones = Object.FindObjectsOfType<AudioZone>();

            // Assign the serialized AudioZone references to the AudioZoneManager's audioZones array
            audioZonesProp.ClearArray();
            audioZonesProp.arraySize = audioZones.Length;
            for (int i = 0; i < audioZones.Length; i++)
            {
                audioZonesProp.GetArrayElementAtIndex(i).objectReferenceValue = audioZones[i];
            }

            // Apply the changes to the AudioZoneManager component
            serializedObject.ApplyModifiedProperties();
        }
        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType != VRCSDKRequestedBuildType.Scene) return false;
            if (!GetAudioZoneManager(out AudioZoneManager audioZoneManager)) return false;
            SerializedObject serializedObject = new SerializedObject(audioZoneManager);

            PopulateAudioZones(serializedObject, audioZoneManager);
            return true;
        }
        static AudioZoneManagerEditor()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            if(!GetAudioZoneManager(out AudioZoneManager audioZoneManager))
            {
                EditorApplication.isPlaying = false;
                return;
            }
            SerializedObject serializedObject = new SerializedObject(audioZoneManager);
            PopulateAudioZones(serializedObject, audioZoneManager);
        }
        private static bool GetAudioZoneManager(out AudioZoneManager audioZoneManager)
        {
            AudioZoneManager[] audioZoneManagers = Object.FindObjectsOfType<AudioZoneManager>();
            if (audioZoneManagers.Length > 1)
            {
                Debug.LogError("[AudioZoneManagerEditor] More than one AudioZoneManager");
                audioZoneManager = null;
                return false;
            }
            audioZoneManager = audioZoneManagers[0];
            return true;
        }
    }
}
#endif