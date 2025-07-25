﻿#if !COMPILER_UDONSHARP && UNITY_EDITOR
using Sylan.AudioManager.EditorUtilities;
using UnityEditor;
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

            //Set Serialized Property
            SerializedPropertyUtils.PopulateSerializedArray<AudioZoneCollider>(serializedObject, AudioZoneManager.AudioZoneCollidersPropertyName);
            SerializedPropertyUtils.PopulateSerializedProperty<AudioSettingManager>(serializedObject, AudioZoneManager.AudioSettingManagerPropertyName);
            return true;
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
}
#endif