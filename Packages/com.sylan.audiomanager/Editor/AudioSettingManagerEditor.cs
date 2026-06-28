using Sylan.AudioManager.EditorUtilities;
using UnityEditor;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Sylan.AudioManager
{
    [InitializeOnLoad]
    public class AudioSettingManagerInitialize : IVRCSDKBuildRequestedCallback
    {
        private static bool SetSerializedProperties()
        {
            //Object with Serialized Property(s)
            if (!SerializedPropertyUtils.GetSerializedObject<AudioSettingManager>(out SerializedObject serializedObject)) return false;

            // Get the AudioSettingManager instance
            AudioSettingManager manager = serializedObject?.targetObject as AudioSettingManager;
            if (manager != null)
            {
                // Add VoiceApplicator to the same GameObject if it doesn't already exist
                VoiceApplicator voiceApplicator = manager.GetComponent<VoiceApplicator>();
                if (voiceApplicator == null)
                {
                    voiceApplicator = manager.gameObject.AddComponent<VoiceApplicator>();
                    UnityEngine.Debug.Log("[AudioManager] Automatically added VoiceApplicator to " + manager.gameObject.name);
                }
            }

            //Set Serialized Property
            SerializedPropertyUtils.PopulateSerializedProperty<AudioZoneManager>(serializedObject, AudioSettingManager.AudioZoneManagerPropertyName);
            SerializedPropertyUtils.PopulateSerializedProperty<VoiceApplicator>(serializedObject, AudioSettingManager.VoiceApplicatorPropertyName);
            return true;
        }
        //
        //Run On Play
        //
        static AudioSettingManagerInitialize()
        //Rename Static Constructor to match Class name
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            if (!SetSerializedProperties())
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
            return SetSerializedProperties();
        }
    }
}
