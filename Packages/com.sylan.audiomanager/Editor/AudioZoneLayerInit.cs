using Sylan.AudioManager;
using UnityEditor;
using UnityEngine;

public class AudioZoneLayerInit : EditorWindow
{
    public const string LayerName = "AudioZones";
    private int layerIndex = -1;

    [MenuItem("Tools/Sylan/Initialize AudioZone Layer")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AudioZoneLayerInit));
    }

    private void OnGUI()
    {
        if (AudioZoneLayerExists())
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Found AudioZones layer at index " + GetAudioZoneLayerByName() + ".", EditorStyles.wordWrappedLabel);
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

    private static int GetAudioZoneLayerByName() => LayerMask.NameToLayer(LayerName);

    public static bool AudioZoneLayerExists() => GetAudioZoneLayerByName() != -1;

    public static bool TryGetAudioZoneLayer(out int audioZoneLayer, AudioZoneManager manager = null)
    {
        if (AudioZoneLayerExists())
        {
            audioZoneLayer = GetAudioZoneLayerByName();
            return true;
        }

        manager ??= FindFirstObjectByType<AudioZoneManager>();
        if (manager != null)
        {
            audioZoneLayer = manager.defaultLayerIndex;
            return true;
        }

        audioZoneLayer = -1;
        return false;
    }

    public static int GetAudioZoneLayer(AudioZoneManager manager = null)
    {
        if (TryGetAudioZoneLayer(out int audioZoneLayer, manager))
        {
            return audioZoneLayer;
        }
        throw new System.Exception("Impossible, manager is expected to be guaranteed to exist, "
            + "getting audio zone layer should always succeed.");
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
            layer.stringValue = LayerName; // Set the name of the new layer

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
