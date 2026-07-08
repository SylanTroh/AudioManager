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
                    foreach (var handles in allColliderHandles.Where(h => h.CanResetLocation()))
                        handles.ResetLocation();

            if (allColliderHandles.Any(h => h.CanResetCollider()))
                if (GUILayout.Button("Reset Audiozone Size"))
                    foreach (var handles in allColliderHandles.Where(h => h.CanResetCollider()))
                        handles.ResetCollider();
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

        /// <summary>
        /// <para>https://discussions.unity.com/t/drawing-capsule-gizmo/597344/13</para>
        /// </summary>
        /// <param name="center"></param>
        /// <param name="height"></param>
        /// <param name="radius"></param>
        /// <param name="direction"></param>
        public static void DrawWireCapsule(Vector3 center, float height, float radius, int direction)
        {
            Vector3 offset = Vector3.zero;
            offset[direction] = height * 0.5f - radius;
            DrawWireCapsule(center + offset, center - offset, radius);
        }

        /// <summary>
        /// <para>https://discussions.unity.com/t/drawing-capsule-gizmo/597344/13</para>
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="radius"></param>
        public static void DrawWireCapsule(Vector3 p1, Vector3 p2, float radius)
        {
            // Special case when both points are in the same position
            if (p1 == p2)
            {
                // DrawWireSphere works only in gizmo methods
                Gizmos.DrawWireSphere(p1, radius);
                return;
            }
            using (new Handles.DrawingScope(Gizmos.color, Gizmos.matrix))
            {
                Quaternion p1Rotation = Quaternion.LookRotation(p1 - p2);
                Quaternion p2Rotation = Quaternion.LookRotation(p2 - p1);
                // Check if capsule direction is collinear to Vector.up
                float c = Vector3.Dot((p1 - p2).normalized, Vector3.up);
                if (c == 1f || c == -1f)
                {
                    // Fix rotation
                    p2Rotation = Quaternion.Euler(p2Rotation.eulerAngles.x, p2Rotation.eulerAngles.y + 180f, p2Rotation.eulerAngles.z);
                }
                // First side
                Handles.DrawWireArc(p1, p1Rotation * Vector3.left, p1Rotation * Vector3.down, 180f, radius);
                Handles.DrawWireArc(p1, p1Rotation * Vector3.up, p1Rotation * Vector3.left, 180f, radius);
                Handles.DrawWireDisc(p1, (p2 - p1).normalized, radius);
                // Second side
                Handles.DrawWireArc(p2, p2Rotation * Vector3.left, p2Rotation * Vector3.down, 180f, radius);
                Handles.DrawWireArc(p2, p2Rotation * Vector3.up, p2Rotation * Vector3.left, 180f, radius);
                Handles.DrawWireDisc(p2, (p1 - p2).normalized, radius);
                // Lines
                Handles.DrawLine(p1 + p1Rotation * Vector3.down * radius, p2 + p2Rotation * Vector3.down * radius);
                Handles.DrawLine(p1 + p1Rotation * Vector3.left * radius, p2 + p2Rotation * Vector3.right * radius);
                Handles.DrawLine(p1 + p1Rotation * Vector3.up * radius, p2 + p2Rotation * Vector3.up * radius);
                Handles.DrawLine(p1 + p1Rotation * Vector3.right * radius, p2 + p2Rotation * Vector3.left * radius);
            }
        }
    }
}
