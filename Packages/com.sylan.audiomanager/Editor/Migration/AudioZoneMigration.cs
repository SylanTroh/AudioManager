using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Sylan.AudioManager
{
    [InitializeOnLoad] // [DefaultExecutionOrder] has no effect on [InitializeOnLoad]
    public static class AudioZoneMigration
    {
        public static readonly Migrator[] allMigrators = new Migrator[]
        {
            AudioZoneManagerMigrator.SingletonInstance,
            AudioZoneColliderMigrator.SingletonInstance,
            AudioSettingColliderMigrator.SingletonInstance,
        };

        static AudioZoneMigration()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened; // Unknown if this is required.
            EditorSceneManager.sceneOpened += OnSceneOpened;
            // It would appear calling GetWindow to create an editor window does not work reliably or well
            // inside of initialize on load. It sometimes creates new windows, breaks existing ones, or they
            // all break. So delay by a frame.
            EditorApplication.update += EditorUpdate;
        }

        private static void EditorUpdate()
        {
            EditorApplication.update -= EditorUpdate;
            OnSceneLoaded();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            OnSceneLoaded();
        }

        private static void OnSceneLoaded()
        {
            bool doShowMigrationWindow = false;
            foreach (Migrator migrator in allMigrators)
            {
                Migrator.SceneLoadedResult result = migrator.OnSceneLoaded();
                if (result == Migrator.SceneLoadedResult.ShowMigrationWindow)
                {
                    doShowMigrationWindow = true;
                }
            }

            if (doShowMigrationWindow)
            {
                AudioZoneMigrationWindow.ShowAudioZoneMigrationWindow();
            }
        }
    }
}
