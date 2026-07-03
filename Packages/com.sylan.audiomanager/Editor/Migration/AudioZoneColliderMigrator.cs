using UnityEditor;

namespace Sylan.AudioManager
{
    // Must also add migrators to the list of allMigrators manually.
    // And custom inspectors must also manually make calls to draw the migration info box, if needed.
    public class AudioZoneColliderMigrator : Migrator<AudioZoneCollider>
    {
        public static readonly AudioZoneColliderMigrator SingletonInstance = new();

        // Script version 0 -> 1:
        // User acknowledged the change for how zones are checked for and may or may not have adjusted the zone.

        public override uint GetCurrentScriptVersion() => AudioZoneCollider.CurrentScriptVersion;
        public override uint GetScriptVersion(AudioZoneCollider instance) => instance.scriptVersion;
        public override string ScriptVersionPropertyName() => nameof(AudioZoneCollider.scriptVersion);

        protected override void DrawMigrationInfoLabelsInInspector(
            uint lowestScriptVersion,
            SerializedObject serializedObject,
            AudioZoneCollider[] targets)
        {
            if (lowestScriptVersion <= 0u)
            {
                AudioZoneManagerMigrator.DrawLabelExplainingChangeToHeadBasedChecks();
            }
        }
    }
}
