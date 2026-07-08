using Sylan.AudioManager.EditorUtilities;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Sylan.AudioManager
{
    [InitializeOnLoad]
    public class AudioSettingManagerInitialize : IVRCSDKBuildRequestedCallback
    {
        private static bool SetSerializedProperties()
        {
            if (!SerializedPropertyUtils.TryFindSerializedObject(out AudioSettingManager manager, out SerializedObject managerSo, required: false)) return false;
            if (manager == null) return true;

            // Add VoiceApplicator to the same GameObject if it doesn't already exist
            if (manager.GetComponent<VoiceApplicator>() == null)
            {
                UdonSharpUndo.AddComponent<VoiceApplicator>(manager.gameObject);
                Debug.Log("[AudioManager] Automatically added VoiceApplicator to " + manager.gameObject.name, manager.gameObject);
            }

            if (!SerializedPropertyUtils.TryPopulateSerializedProperty<AudioZoneManager>(managerSo, AudioSettingManager.AudioZoneManagerPropertyName, required: false)) return false;
            if (!SerializedPropertyUtils.TryPopulateSerializedProperty<VoiceApplicator>(managerSo, AudioSettingManager.VoiceApplicatorPropertyName, required: true)) return false;
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
