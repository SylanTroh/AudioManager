using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sylan.AudioManager
{
    // Must also add migrators to the list of allMigrators manually.
    // And custom inspectors must also manually make calls to draw the migration info box, if needed.
    public class AudioZoneManagerMigrator : Migrator<AudioZoneManager>
    {
        public static readonly AudioZoneManagerMigrator SingletonInstance = new();

        // Script version 0 -> 1:
        // Automatically set headCheckRadius to a non 0 value for backwards compatibility.
        // This way users can dismiss the migration window entirely, mark all as resolved, and the scene
        // should continue to function comparatively to how it did before.
        // If the AudioZones layer exists, automatically update the collision matrix for it, ignoring all collisions.
        //
        // Script version 1 -> 2:
        // User acknowledged the change for how zones are checked for and may or may not have adjusted the zone.

        public override uint GetCurrentScriptVersion() => AudioZoneManager.CurrentScriptVersion;
        public override uint GetScriptVersion(AudioZoneManager instance) => instance.scriptVersion;
        public override string ScriptVersionPropertyName() => nameof(AudioZoneManager.scriptVersion);

        private const float BackwardsCompatibleHeadCheckRadius = 0.25f;

        public override SceneLoadedResult OnSceneLoaded()
        {
            AudioZoneManager[] managers = FindAll();

            AudioZoneManager[] versionZeroManagers = managers.Where(m => m.scriptVersion == 0u).ToArray();
            if (versionZeroManagers.Length != 0)
            {
                SerializedObject so = new(versionZeroManagers);
                so.FindProperty(nameof(AudioZoneManager.scriptVersion)).uintValue = 1u;
                so.FindProperty(AudioZoneManager.HeadCheckRadiusPropertyName).floatValue = BackwardsCompatibleHeadCheckRadius;
                so.ApplyModifiedPropertiesWithoutUndo();

                if (AudioZoneLayerInit.AudioZoneLayerExists())
                {
                    AudioZoneLayerInit.IgnoreAllLayerCollision(AudioZoneLayerInit.GetAudioZoneLayer());
                }
            }

            return GetDefaultSceneLoadedResult(managers);
        }

        public override void CreateGUIInMigrationWindowInfoBox(VisualElement infoBox)
        {
            AudioZoneManager[] managers = FindAll();
            if (managers.Length == 0) return;
            uint lowestScriptVersion = managers.Min(m => m.scriptVersion);
            if (lowestScriptVersion <= 1u)
            {
                infoBox.Add(new Label($"The way audio zones get detected has been changed fundamentally. "
                    + $"To determine which zones a player is in, rather than using the player's capsule collider "
                    + $"the system now uses the player's head position.")
                { style = { whiteSpace = WhiteSpace.Normal, marginBottom = 4f } });

                infoBox.Add(new Label($"For backwards compatibility there is an option to increase the radius "
                    + $"of those head based checks. In new scenes it defaults to 0, however it has been set to "
                    + $"{BackwardsCompatibleHeadCheckRadius} in this scene.")
                { style = { whiteSpace = WhiteSpace.Normal, marginBottom = 4f } });

                infoBox.Add(new Label($"For cleanliness and maintainability of the scene, it might be good "
                    + $"to make audio zones match the size of interiors/areas exactly - if they aren't already - "
                    + $"and then change Head Check Radius to 0 on the Audio Zone Manager.")
                { style = { whiteSpace = WhiteSpace.Normal } });
            }
        }

        protected override void DrawMigrationInfoLabelsInInspector(
            uint lowestScriptVersion,
            SerializedObject serializedObject,
            AudioZoneManager[] targets)
        {
            if (lowestScriptVersion <= 1u)
            {
                DrawLabelExplainingChangeToHeadBasedChecks();
            }
        }

        public static void DrawLabelExplainingChangeToHeadBasedChecks()
        {
            GUILayout.Label("With the change to how audio zones are checked for it'd be clean "
                + "for zones to match the size of interiors/areas exactly, and the manager be configured to have a "
                + "Head Check Radius of 0.", EditorStyles.wordWrappedLabel);
        }
    }
}
