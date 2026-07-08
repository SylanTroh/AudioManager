using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sylan.AudioManager
{
    public abstract class Migrator
    {
        public enum SceneLoadedResult
        {
            None,
            ShowMigrationWindow,
        }
        public abstract SceneLoadedResult OnSceneLoaded();
        public virtual void CreateGUIInMigrationWindowInfoBox(VisualElement infoBox) { }
        public abstract IEnumerable<Component> GetAllComponentsRequiringMigration();
        public abstract void MarkAllMigrationsAsResolved();
        public abstract void DrawMigrationInfoInInspector(SerializedObject serializedObject, Object[] targets);
    }

    public abstract class Migrator<T> : Migrator
        where T : Component
    {
        public abstract uint GetCurrentScriptVersion();
        public abstract uint GetScriptVersion(T instance);
        public abstract string ScriptVersionPropertyName();

        protected T[] FindAll()
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        protected virtual bool ShouldShowMigrationWindow() => ShouldShowMigrationWindow(FindAll());

        protected bool ShouldShowMigrationWindow(T[] instances)
        {
            return instances.Any(inst => GetScriptVersion(inst) < GetCurrentScriptVersion());
        }

        protected SceneLoadedResult GetDefaultSceneLoadedResult(T[] instances)
        {
            return ShouldShowMigrationWindow(instances)
                ? SceneLoadedResult.ShowMigrationWindow
                : SceneLoadedResult.None;
        }

        public override SceneLoadedResult OnSceneLoaded() => GetDefaultSceneLoadedResult(FindAll());

        public override IEnumerable<Component> GetAllComponentsRequiringMigration()
        {
            return FindAll().Where(inst => GetScriptVersion(inst) < GetCurrentScriptVersion());
        }

        public override void MarkAllMigrationsAsResolved()
        {
            MarkMigrationsAsResolved(GetAllComponentsRequiringMigration().ToArray());
        }

        private void MarkMigrationsAsResolved(Component[] instances)
        {
            if (instances.Length == 0) return;
            SerializedObject so = new(instances);
            so.FindProperty(ScriptVersionPropertyName()).uintValue = GetCurrentScriptVersion();
            so.ApplyModifiedProperties();
        }

        public override void DrawMigrationInfoInInspector(SerializedObject serializedObject, Object[] targets)
        {
            T[] instances = targets.Cast<T>().ToArray(); // Doing '(T[])targets' is an invalid cast apparently.
            uint lowestScriptVersion = instances.Min(GetScriptVersion);
            if (lowestScriptVersion >= GetCurrentScriptVersion()) return;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Audio Zone Migration", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                }
                DrawMigrationInfoLabelsInInspector(lowestScriptVersion, serializedObject, instances);
                if (GUILayout.Button("Mark Migration As Resolved"))
                {
                    MarkMigrationsAsResolved(instances);
                    AudioZoneMigrationWindow.RefreshListIfWindowExists();
                }
            }
        }

        protected abstract void DrawMigrationInfoLabelsInInspector(
            uint lowestScriptVersion,
            SerializedObject serializedObject,
            T[] targets);
    }
}
