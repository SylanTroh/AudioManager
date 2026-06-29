using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Sylan.AudioManager
{
    public abstract class ZoneEditor : Editor
    {
        private Component zone;
        BoxCollider boxCollider;
        CapsuleCollider capsuleCollider;
        SphereCollider sphereCollider;
        MeshCollider meshCollider;
        private const float handleSize = 0.1f;
        // These are static to be remembered throughout this unity session.
        private static float shrinkGrowthAmount = 0.5f;
        private static bool showFoldout = true;
        private bool hasValidMeshCollider = false;

        private void OnEnable()
        {
            zone = target as Component;
            boxCollider = zone.GetComponent<BoxCollider>();
            capsuleCollider = zone.GetComponent<CapsuleCollider>();
            sphereCollider = zone.GetComponent<SphereCollider>();
            meshCollider = zone.GetComponent<MeshCollider>();
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
            hasValidMeshCollider = meshCollider != null && meshCollider.isTrigger;

            if (boxCollider == null && sphereCollider == null && capsuleCollider == null && !hasValidMeshCollider)
            {
                EditorGUILayout.HelpBox("Add a Collider component to enable resizing handles in the scene view.", MessageType.Info);

                if (GUILayout.Button("Add BoxCollider"))
                {
                    boxCollider = Undo.AddComponent<BoxCollider>(zone.gameObject);
                    ResetBoxCollider(boxCollider);
                }
                if (GUILayout.Button("Add SphereCollider"))
                {
                    sphereCollider = Undo.AddComponent<SphereCollider>(zone.gameObject);
                    ResetSphereCollider(sphereCollider);
                }
                return;
            }

            if (boxCollider != null)
            {
                EditorGUILayout.LabelField("Shrinking Audiozones can help with clipping", EditorStyles.boldLabel);
                shrinkGrowthAmount = Mathf.Max(0f, EditorGUILayout.FloatField("Shrink/Growth Amount", shrinkGrowthAmount));

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Shrink")) GrowCollider(boxCollider, -shrinkGrowthAmount);
                if (GUILayout.Button("Grow")) GrowCollider(boxCollider, shrinkGrowthAmount);
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Reset Audiozone Size"))
                {
                    ResetBoxCollider(boxCollider);
                }
                return;
            }

            if (sphereCollider != null)
            {
                if (GUILayout.Button("Reset Audiozone Size"))
                {
                    ResetSphereCollider(sphereCollider);
                }
                return;
            }
        }

        private void GrowCollider(BoxCollider collider, float delta)
        {
            Vector3 newSize = collider.size + Vector3.one * delta;
            newSize = Vector3.Max(newSize, Vector3.zero); // Ensure the size doesn't go below zero
            SerializedObject so = new(collider);
            so.FindProperty("m_Size").vector3Value = newSize;
            so.ApplyModifiedProperties();
        }

        private void ResetBoxCollider(BoxCollider collider)
        {
            Bounds bounds = GetBoundsFromAttachedMesh(collider.transform.parent);
            SerializedObject so = new(collider);
            so.FindProperty("m_Center").vector3Value = bounds.center;
            so.FindProperty("m_Size").vector3Value = bounds.size;
            so.FindProperty("m_IsTrigger").boolValue = true;
            so.ApplyModifiedProperties();
            ResetTransformPositionAndRotation();
        }

        private void ResetSphereCollider(SphereCollider collider)
        {
            Bounds bounds = GetBoundsFromAttachedMesh(collider.transform.parent);
            SerializedObject so = new(collider);
            so.FindProperty("m_Center").vector3Value = bounds.center;
            so.FindProperty("m_Radius").floatValue = bounds.extents.magnitude;
            so.FindProperty("m_IsTrigger").boolValue = true;
            so.ApplyModifiedProperties();
            ResetTransformPositionAndRotation();
        }

        private void ResetTransformPositionAndRotation()
        {
            SerializedObject so = new(zone.transform);
            so.FindProperty("m_LocalPosition").vector3Value = Vector3.zero;
            so.FindProperty("m_LocalRotation").quaternionValue = Quaternion.identity;
            so.ApplyModifiedProperties();
        }

        private Bounds GetBoundsFromAttachedMesh(Transform transform)
        {
            MeshFilter meshFilter = transform.gameObject.GetComponent<MeshFilter>();
            return meshFilter?.sharedMesh.bounds ?? new Bounds(Vector3.zero, Vector3.one);
        }

        // Must not be private in order for it to be run by unity for deriving classes.
        protected void OnSceneGUI()
        {
            hasValidMeshCollider = meshCollider != null && meshCollider.isTrigger;

            if (boxCollider == null && sphereCollider == null && !hasValidMeshCollider) return;

            if (boxCollider != null)
            {
                EditorGUI.BeginChangeCheck();

                Vector3[] handles = GetHandlePositions();

                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i] = boxCollider.transform.TransformPoint(handles[i]);
                    handles[i] = Handles.FreeMoveHandle(handles[i], handleSize, Vector3.one * 0.1f, Handles.SphereHandleCap);
                    handles[i] = boxCollider.transform.InverseTransformPoint(handles[i]);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    SerializedObject so = new(boxCollider);

                    Vector3 newSize = Vector3.zero;
                    newSize.x = Mathf.Abs(handles[1].x - handles[0].x);
                    newSize.y = Mathf.Abs(handles[3].y - handles[2].y);
                    newSize.z = Mathf.Abs(handles[5].z - handles[4].z);
                    so.FindProperty("m_Size").vector3Value = newSize;

                    Vector3 newCenter = Vector3.zero;
                    newCenter.x = (handles[1].x + handles[0].x) / 2;
                    newCenter.y = (handles[3].y + handles[2].y) / 2;
                    newCenter.z = (handles[5].z + handles[4].z) / 2;
                    so.FindProperty("m_Center").vector3Value = newCenter;

                    so.ApplyModifiedProperties();
                }
                return;
            }

            if (sphereCollider != null)
            {
                EditorGUI.BeginChangeCheck();

                Vector3 centerHandle = GetCenterHandlePosition();
                Vector3 radiusHandle = GetRadiusHandlePosition();

                centerHandle = Handles.FreeMoveHandle(centerHandle, handleSize, Vector3.one * 0.1f, Handles.SphereHandleCap);
                radiusHandle = Handles.FreeMoveHandle(radiusHandle, handleSize, Vector3.one * 0.1f, Handles.SphereHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    SerializedObject so = new(sphereCollider);

                    Vector3 newCenter = sphereCollider.transform.InverseTransformPoint(centerHandle);
                    Vector3 delta = newCenter - sphereCollider.center;
                    so.FindProperty("m_Center").vector3Value = newCenter;

                    radiusHandle += delta;
                    float newRadius = Vector3.Distance(centerHandle, radiusHandle);
                    so.FindProperty("m_Radius").floatValue = newRadius;

                    so.ApplyModifiedProperties();
                }
            }
        }

        private Vector3[] GetHandlePositions()
        {
            Vector3[] positions = new Vector3[6];

            Vector3 halfSize = boxCollider.size / 2;
            positions[0] = boxCollider.center - Vector3.right * halfSize.x;
            positions[1] = boxCollider.center + Vector3.right * halfSize.x;
            positions[2] = boxCollider.center - Vector3.up * halfSize.y;
            positions[3] = boxCollider.center + Vector3.up * halfSize.y;
            positions[4] = boxCollider.center - Vector3.forward * halfSize.z;
            positions[5] = boxCollider.center + Vector3.forward * halfSize.z;

            return positions;
        }

        private Vector3 GetCenterHandlePosition()
        {
            return sphereCollider.transform.TransformPoint(sphereCollider.center);
        }

        private Vector3 GetRadiusHandlePosition()
        {
            return sphereCollider.transform.TransformPoint(sphereCollider.center + Vector3.up * sphereCollider.radius);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        private static void DrawGizmos(Component audioZone, GizmoType gizmoType)
        {
            var colliderTransform = audioZone.transform.Find("Component");
            if (colliderTransform == null) return;
            var colliderObject = colliderTransform.gameObject;

            BoxCollider boxCollider = colliderObject.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                Gizmos.color = new Color(0, 1, 0, 1.0f);
                Gizmos.matrix = Matrix4x4.TRS(boxCollider.transform.position, boxCollider.transform.rotation, boxCollider.transform.lossyScale);
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                return;
            }

            SphereCollider sphereCollider = colliderObject.GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                Gizmos.color = new Color(0, 1, 0, 1.0f);
                Gizmos.matrix = Matrix4x4.TRS(sphereCollider.transform.position, sphereCollider.transform.rotation, sphereCollider.transform.lossyScale);
                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }

            MeshCollider meshCollider = colliderObject.GetComponent<MeshCollider>();
            bool hasValidMeshCollider = meshCollider != null && meshCollider.isTrigger;
            if (hasValidMeshCollider)
            {
                Gizmos.color = new Color(0, 1, 0, 1.0f);
                Gizmos.matrix = Matrix4x4.TRS(meshCollider.transform.position, meshCollider.transform.rotation, meshCollider.transform.lossyScale);
                Gizmos.DrawWireMesh(meshCollider.sharedMesh);
            }
        }
    }
}
