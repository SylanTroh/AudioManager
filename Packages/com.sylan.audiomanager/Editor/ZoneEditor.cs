using System.Collections.Generic;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Sylan.AudioManager
{
    public abstract class ZoneEditor : Editor
    {
        // These are static to be remembered throughout this unity session.
        private static float shrinkGrowthAmount = 0.25f;
        private static bool showFoldout = true;

        private readonly List<ColliderHandles> allColliderHandles = new();

        /// <summary>
        /// <para>Call <see cref="TryFindAudioZoneManager"/> before using this field.</para>
        /// </summary>
        private AudioZoneManager manager;
        private void TryFindAudioZoneManager() => manager ??= FindAnyObjectByType<AudioZoneManager>(FindObjectsInactive.Include);

        private void OnEnable()
        {
            GetCollidersAndCreateHandles();
            SceneView.duringSceneGui += OnSceneGUIUdonSharpFree;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUIUdonSharpFree;
        }

        private void GetCollidersAndCreateHandles()
        {
            allColliderHandles.Clear();
            foreach (Component target in targets.Cast<Component>())
            {
                foreach (Collider collider in target.GetComponents<Collider>())
                {
                    if (ColliderHandles.TryCreate(collider, out ColliderHandles handles))
                    {
                        allColliderHandles.Add(handles);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets))
                return;

            serializedObject.Update();
            GetMigrator().DrawMigrationInfoInInspector(serializedObject, targets);
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawAudioZoneEditorSettings();
        }

        protected abstract Migrator GetMigrator();

        private void DrawAudioZoneEditorSettings()
        {
            showFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(showFoldout, "Audiozone Editor Settings", EditorStyles.foldoutHeader);
            if (showFoldout)
            {
                DrawAudioZoneEditorSettingsContent();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawAudioZoneEditorSettingsContent()
        {
            if (!allColliderHandles.Any(h => h.IsRelevantToAudioZones()))
            {
                EditorGUILayout.HelpBox("Add a Collider component to enable resizing handles in the scene view.", MessageType.Info);

                if (GUILayout.Button("Add BoxCollider"))
                {
                    AddColliders<BoxCollider>();
                }
                if (GUILayout.Button("Add SphereCollider"))
                {
                    AddColliders<SphereCollider>();
                }
            }

            if (allColliderHandles.Any(h => h.CanGrowCollider()))
            {
                TryFindAudioZoneManager();
                EditorGUILayout.HelpBox("Depending on Head Check Radius defined on the Audio Zone Manager:\n"
                    + "- when 0 (default), zones should likely match the size of interiors/areas exactly\n"
                    + "- when larger, shrinking zones can help with undesired clipping into zones"
                    + (manager == null ? "" : $"\n(Current Head Check Radius: {manager.HeadCheckRadius})"),
                    MessageType.None);

                shrinkGrowthAmount = Mathf.Max(0f, EditorGUILayout.FloatField("Shrink/Growth Amount", shrinkGrowthAmount));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Shrink")) GrowColliders(-shrinkGrowthAmount);
                if (GUILayout.Button("Grow")) GrowColliders(shrinkGrowthAmount);
                GUILayout.EndHorizontal();
            }

            if (allColliderHandles.Any(h => h.CanResetLocation()))
                if (GUILayout.Button("Reset Audiozone Location"))
                    foreach (ColliderHandles handles in allColliderHandles.Where(h => h.CanResetLocation()))
                        handles.ResetLocation();

            if (allColliderHandles.Any(h => h.CanResetCollider()))
                if (GUILayout.Button(new GUIContent(
                    "Reset Audiozone Size",
                    "If there is a Mesh Filter on the parent of this object, "
                        + "its bounds will be used as a reference to set the size of this Audiozone.")))
                {
                    foreach (ColliderHandles handles in allColliderHandles.Where(h => h.CanResetCollider()))
                        handles.ResetCollider();
                }
        }

        private void AddColliders<T>()
            where T : Collider
        {
            foreach (Component target in targets.Cast<Component>())
            {
                T collider = Undo.AddComponent<T>(target.gameObject);
                if (!ColliderHandles.TryCreate(collider, out ColliderHandles handles)) continue;
                if (handles.CanResetCollider()) handles.ResetCollider();
                allColliderHandles.Add(handles);
            }
        }

        private void GrowColliders(float delta)
        {
            foreach (ColliderHandles handles in allColliderHandles.Where(h => h.CanGrowCollider()))
            {
                handles.GrowCollider(delta);
            }
        }

        /// <summary>
        /// <para>Cannot use normal <c>OnSceneGUI</c> because that would end up lagging more and more the more
        /// objects are multi-selected. Not because of any of the logic in here, however.</para>
        /// <para>The cause would be in the <c>UdonSharpBehaviourOverrideEditor</c>, which does a call to
        /// <see cref="SerializedObject.Update"/> before calling our function.</para>
        /// </summary>
        private void OnSceneGUIUdonSharpFree(SceneView view)
        {
            foreach (ColliderHandles handles in allColliderHandles)
            {
                handles.DrawHandles();
            }
        }

        public static bool IsRelevantMeshCollider(MeshCollider meshCollider)
        {
            return meshCollider != null && meshCollider.isTrigger;
        }

        protected static void DrawColliderGizmos(Component audioZone, Color color)
        {
            Gizmos.color = color;
            Gizmos.matrix = audioZone.transform.localToWorldMatrix;

            foreach (Collider collider in audioZone.GetComponents<Collider>())
            {
                switch (collider)
                {
                    case BoxCollider boxCollider:
                        Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                        break;
                    case SphereCollider sphereCollider:
                        Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
                        break;
                    case MeshCollider meshCollider:
                        if (IsRelevantMeshCollider(meshCollider))
                        {
                            Gizmos.DrawWireMesh(meshCollider.sharedMesh);
                        }
                        break;
                    case CapsuleCollider capsuleCollider:
                        DrawWireCapsule(capsuleCollider.center, capsuleCollider.height, capsuleCollider.radius, capsuleCollider.direction);
                        break;
                }
            }
        }

        public static void DrawWireCapsule(Vector3 center, float height, float radius, int direction)
        {
            // Inspired by: https://discussions.unity.com/t/drawing-capsule-gizmo/597344/13
            Vector3 offset = Vector3.zero;
            offset[direction] = Mathf.Max(0f, height * 0.5f - radius);
            DrawWireCapsule(center - offset, center + offset, radius);
        }

        public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius)
        {
            // Inspired by: https://discussions.unity.com/t/drawing-capsule-gizmo/597344/13
            if (point1 == point2)
            {
                Gizmos.DrawWireSphere(point1, radius);
                return;
            }
            using (new Handles.DrawingScope(Gizmos.color, Gizmos.matrix))
            {
                Quaternion rotation = Quaternion.LookRotation(point1 - point2);

                Handles.DrawWireArc(point1, rotation * Vector3.left, rotation * Vector3.down, 180f, radius);
                Handles.DrawWireArc(point1, rotation * Vector3.up, rotation * Vector3.left, 180f, radius);
                Handles.DrawWireDisc(point1, rotation * Vector3.forward, radius);

                Handles.DrawWireArc(point2, rotation * Vector3.left, rotation * Vector3.down, -180f, radius);
                Handles.DrawWireArc(point2, rotation * Vector3.up, rotation * Vector3.left, -180f, radius);
                Handles.DrawWireDisc(point2, rotation * Vector3.forward, radius);

                Handles.DrawLine(point1 + rotation * Vector3.down * radius, point2 + rotation * Vector3.down * radius);
                Handles.DrawLine(point1 + rotation * Vector3.left * radius, point2 + rotation * Vector3.left * radius);
                Handles.DrawLine(point1 + rotation * Vector3.up * radius, point2 + rotation * Vector3.up * radius);
                Handles.DrawLine(point1 + rotation * Vector3.right * radius, point2 + rotation * Vector3.right * radius);
            }
        }
    }
}
