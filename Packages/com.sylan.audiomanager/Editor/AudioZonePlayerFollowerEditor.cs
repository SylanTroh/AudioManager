#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Sylan.AudioManager
{
    [CustomEditor(typeof(AudioZonePlayerFollower))]
    public class AudioZonePlayerFollowerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Keep the Collider of this game object very small to not trigger any Audio Zones through the wall!",
                MessageType.Warning
            );
            EditorGUILayout.HelpBox(
                "Usage of Stations break the AudioManager. This is a workaround to prevent this.\n"
                + "If you dont expect anyone from using stations, delete this game object. It will save u performance.",
                MessageType.Info
            );
            base.OnInspectorGUI();
        }
    }

    [InitializeOnLoad]
    public class AudioZonePlayerFollowerInitialize : IVRCSDKBuildRequestedCallback
    {
        private static int FindAudioZoneLayer()
        {
            int layerIndex = -1;
            for (int i = 22; i < 32; i++)
            {
                if (LayerMask.LayerToName(i) == "AudioZones")
                {
                    layerIndex = i;
                    Debug.Log("Found AudioZones layer at index " + i + ".");
                }
            }

            Debug.LogWarning("No AudioZones layer found after index 21.");
            return layerIndex;
        }

        private static bool RunOnBuild()
        {
            //Object with Serialized Property(s)
            var audioZonePlayerFollowers = Object.FindObjectsOfType<AudioZonePlayerFollower>(true);
            if (audioZonePlayerFollowers.Length == 0) return true;
            if (audioZonePlayerFollowers.Length > 1)
            {
                Debug.LogError($"Build blocked: There are multiple {nameof(AudioZonePlayerFollower)} scripts!");
                return false;
            }

            audioZonePlayerFollowers[0].enabled = true;
            audioZonePlayerFollowers[0].gameObject.layer = 0;
            audioZonePlayerFollowers[0].gameObject.SetActive(true);

            var audioZoneLayer = FindAudioZoneLayer();

            var collisionLayerMask = 1 << audioZoneLayer; // mask for audio zone layer
            foreach (var collider in audioZonePlayerFollowers[0].GetComponents<Collider>())
            {
                collider.isTrigger = true;
                if (audioZoneLayer == -1) continue;
                collider.includeLayers = collisionLayerMask; //only include audio zone layer
                collider.excludeLayers = ~collisionLayerMask; //exclude everything but the audio zone layer
            }

            foreach (var rigidbody in audioZonePlayerFollowers[0].GetComponents<Rigidbody>())
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                if (audioZoneLayer == -1) continue;
                rigidbody.includeLayers = collisionLayerMask; //only include audio zone layer
                rigidbody.excludeLayers = ~collisionLayerMask; //exclude everything but the audio zone layer
            }

            return true;
        }

        //
        //Run On Play
        //
        static AudioZonePlayerFollowerInitialize()
            //Rename Static Constructor to match Class name
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            RunOnBuild();
        }

        //
        // Run On Build
        //
        public int callbackOrder => 0;

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            if (requestedBuildType != VRCSDKRequestedBuildType.Scene) return false;
            return RunOnBuild();
        }
    }
}
#endif