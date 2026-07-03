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
        private static float shrinkGrowthAmount = 0.5f;
        private static bool showFoldout = true;

        private readonly List<ColliderHandles> allColliderHandles = new();

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
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawAudioZoneEditorSettings();
        }

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
                EditorGUILayout.LabelField("Shrinking Audiozones can help with clipping", EditorStyles.boldLabel);
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
                        // This makes all other handles not show hovers and interactivity, which is bothersome.
                        // In other words, this is the wrong/hacky way to do it.
                        // case CapsuleCollider capsuleCollider:
                        //     var capsule = new CapsuleBoundsHandle()
                        //     {
                        //         center = capsuleCollider.center,
                        //         height = capsuleCollider.height,
                        //         radius = capsuleCollider.radius,
                        //         heightAxis = (CapsuleBoundsHandle.HeightAxis)capsuleCollider.direction,
                        //         handleColor = new Color(0f, 0f, 0f, 0f),
                        //         wireframeColor = color,
                        //     };
                        //     Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
                        //     Handles.matrix = audioZone.transform.localToWorldMatrix;
                        //     capsule.DrawHandle();
                        //     break;
                }
            }
        }
    }
}
