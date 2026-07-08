using Sylan.AudioManager.EditorUtilities;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Sylan.AudioManager
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AudioZoneManagerKillSwitch))]
    public class AudioZoneManagerKillSwitchEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets))
                return;
            EditorGUILayout.Space();

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(new GUIContent("These Custom Events can be sent to this script:\n"
                + "- EnableKillSwitch\n"
                + "- DisableKillSwitch\n"
                + "- ToggleKillSwitch\n"
                + "Like with a Local Event On Interact script,\n"
                + "or a UI Toggle Send Local Event script, "
                + "likely in combination with a UI Toggle Sync script plus enabling the "
                + "Syncing Is Handled Externally checkbox on this script.\n"
                + "Those mentioned scripts are from the JanSharp Common package, other packages may provide equivalents."));
        }
    }

    public static class AudioZoneManagerKillSwitchInitialize
    {
        public static bool RunOnBuild()
        {
            if (!SerializedPropertyUtils.TryFindSerializedObject<AudioZoneManagerKillSwitch>(out _, out SerializedObject so, required: false)) return false;
            if (!SerializedPropertyUtils.TryPopulateSerializedProperty<AudioZoneManager>(so, AudioZoneManagerKillSwitch.AudioZoneManagerPropertyName, required: true)) return false;
            if (!SerializedPropertyUtils.TryPopulateSerializedProperty<AudioSettingManager>(so, AudioZoneManagerKillSwitch.AudioSettingManagerPropertyName, required: true)) return false;
            return true;
        }
    }
}
