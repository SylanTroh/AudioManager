using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sylan.AudioManager
{
    public class AudioZoneMigrationWindow : EditorWindow
    {
        private static AudioZoneMigrationWindow MainInstance;

        private VisualElement root;
        private ListView listView;
        private readonly List<Component> componentsRequiringMigration = new();

        [MenuItem("Tools/Sylan/Audio Zones Migration")]
        public static void ShowAudioZoneMigrationWindow()
        {
            MainInstance = GetWindow<AudioZoneMigrationWindow>();
            MainInstance.minSize = new Vector2(500f, 600f);
            MainInstance.titleContent = new GUIContent("Audio Zones Migration");
            MainInstance.RefreshList();
            MainInstance.Focus();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        // Unconditionally running refresh in both of these cases likely isn't the most performant method of
        // handling this. Though in the case of hierarchy change I legitimately do not know if there is a
        // cleaner way of doing it. Should hopefully still be fine, it's not that incredibly expensive to
        // find all the scrips and rebuild the list, and it only matters during migration.
        private void OnUndoRedo() => RefreshList();
        private void OnHierarchyChanged() => RefreshList();

        private void CreateGUI()
        {
            root = rootVisualElement;
            root.Add(new Label("Audio Zones Migration")
            {
                style = {
                    alignSelf = Align.Center,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 16f,
                    marginTop = 2f,
                    marginBottom = 2f,
                },
            });

            CreateHeader();
            CreateCommonButtons();
            // Not fetching components requiring migration here, the show window function does it.
            CreateList();
        }

        private void FetchAllComponentsRequiringMigration()
        {
            componentsRequiringMigration.Clear();
            foreach (Migrator migrator in AudioZoneMigration.allMigrators)
            {
                componentsRequiringMigration.AddRange(migrator.GetAllComponentsRequiringMigration());
            }
        }

        private void CreateHeader()
        {
            Box headerBox = new() { style = { flexShrink = 0f, marginBottom = 4f } };

            foreach (Migrator migrator in AudioZoneMigration.allMigrators)
            {
                migrator.CreateGUIInMigrationWindowInfoBox(headerBox);
            }

            if (headerBox.childCount != 0)
                root.Add(headerBox);
        }

        private void CreateCommonButtons()
        {
            Box box = new() { style = { flexShrink = 0f, marginBottom = 4f } };

            box.Add(new Button(MarkAllAsResolved) { text = "Mark All As Resolved" });
            box.Add(new Button(RefreshList) { text = "Refresh List" });

            root.Add(box);
        }

        private void CreateList()
        {
            Box listBox = new() { style = { flexGrow = 1f } };
            listBox.Add(
                new Label("Objects Requiring Migration")
                { style = { alignSelf = Align.Center, unityFontStyleAndWeight = FontStyle.Bold } });
            listBox.Add(
                new Label("Click on objects and see their inspector for instructions")
                { style = { alignSelf = Align.Center, whiteSpace = WhiteSpace.Normal, marginBottom = 4f } });

            listView = new ListView(componentsRequiringMigration, 16, MakeListIem, BindListItem)
            {
                style = { flexGrow = 1f },
                selectionType = SelectionType.Multiple,
            };
            listView.selectionChanged += OnListSelectionChanged;
            listBox.Add(listView);
            root.Add(listBox);
        }

        private VisualElement MakeListIem()
        {
            VisualElement row = new() { style = { flexDirection = FlexDirection.Row } };
            row.Add(new Image() { style = { width = 18f, flexShrink = 0f } });
            row.Add(new Label());
            return row;
        }

        private void BindListItem(VisualElement element, int index)
        {
            Component component = componentsRequiringMigration[index];
            Image image = (Image)element[0];
            Label label = (Label)element[1];
            image.image = AssetPreview.GetMiniThumbnail(component.gameObject);
            label.text = component.name;
        }

        private void OnListSelectionChanged(IEnumerable<object> selected)
        {
            Selection.objects = selected.Cast<Component>()
                .Where(c => c != null)
                .Select(c => c.gameObject)
                .ToArray();
        }

        private void MarkAllAsResolved()
        {
            foreach (Migrator migrator in AudioZoneMigration.allMigrators)
            {
                migrator.MarkAllMigrationsAsResolved();
            }
            RefreshList();
        }

        private void RefreshList()
        {
            FetchAllComponentsRequiringMigration();
            if (listView == null) return; // Could run before CreateGUI.
            listView.Rebuild();
        }

        public static void RefreshListIfWindowExists()
        {
            if (MainInstance == null) return;
            MainInstance.RefreshList();
        }
    }
}
