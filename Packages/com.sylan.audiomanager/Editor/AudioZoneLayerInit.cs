using Sylan.AudioManager;
using UnityEditor;
using UnityEngine;

public class AudioZoneLayerInit : EditorWindow
{
    public const string layerName = "AudioZones";
    private int layerIndex = -1;

    [MenuItem("Tools/Sylan/Initialize AudioZone Layer")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AudioZoneLayerInit));
    }

    private void OnGUI()
    {
        if (TryFindAudioZoneLayer(out var existingLayerIndex))
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Found AudioZones layer at index " + existingLayerIndex + ".", EditorStyles.wordWrappedLabel);
            }
            return;
        }
        layerIndex = EditorGUILayout.IntField("Layer Index", layerIndex);

        if (GUILayout.Button("Find Empty Layer"))
        {
            FindEmptyLayer();
        }

        if (GUILayout.Button("Initialize Layer"))
        {
            if (layerIndex == -1)
            {
                Debug.LogError("[AudioManager] Please enter a valid layer index.");
                return;
            }

            if (LayerMask.LayerToName(layerIndex) != "")
            {
                if (EditorUtility.DisplayDialog("Layer Already Exists",
                    "The layer already exists. Are you sure you want to overwrite its settings?", "Yes", "No"))
                {
                    Initialize();
                }
            }
            else
            {
                Initialize();
            }
        }
    }
    private void FindEmptyLayer()
    {
        for (int i = 22; i < 32; i++)
        {
            if (LayerMask.LayerToName(i) == "")
            {
                layerIndex = i;
                Debug.Log("[AudioManager] Found empty layer at index " + i + ".");
                return;
            }
        }

        Debug.LogWarning("[AudioManager] No empty layer found after index 21.");
    }

    public static bool TryFindAudioZoneLayer(out int layerIndex, object source = null)
    {
        var manager = source switch
        {
            SerializedObject serializedObject => serializedObject.targetObject as AudioZoneManager,
            AudioZoneManager audioZoneManager => audioZoneManager,
            _ => null
        };
        manager = manager != null ? manager : FindFirstObjectByType<AudioZoneManager>();

        layerIndex = LayerMask.NameToLayer(layerName);
        var success = layerIndex != -1;
        if (!success)
        {
            layerIndex = manager != null ? manager.defaultLayerIndex : -1;
        }
        return success;
    }

    private void Initialize()
    {
        // Create the layer if it doesn't exist
        if (LayerMask.LayerToName(layerIndex) == "")
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            layers.GetArrayElementAtIndex(layerIndex).stringValue = "Layer " + layerIndex;
            SerializedProperty layer = layers.GetArrayElementAtIndex(layerIndex);
            layer.stringValue = layerName; // Set the name of the new layer

            tagManager.ApplyModifiedProperties();

            IgnoreAllLayerCollision(layerIndex);
        }
    }

    public static void IgnoreAllLayerCollision(int layerIndex)
    {
        // Set the collision matrix for the layer
        for (int i = 0; i < 32; i++)
        {
            Physics.IgnoreLayerCollision(layerIndex, i, true);
        }
    }
}
