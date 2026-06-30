using UnityEditor;
using UnityEngine;

namespace Sylan.AudioManager
{
    public class BoxColliderHandles : ColliderHandles<BoxCollider>
    {
        public BoxColliderHandles(BoxCollider collider)
            : base(collider)
        { }

        public override bool IsRelevantToAudioZones() => true;

        public override bool CanResetLocation() => true;
        public override void ResetLocation() => ResetTransformPositionAndRotation();

        public override bool CanResetCollider() => true;
        public override void ResetCollider()
        {
            Bounds bounds = GetBoundsFromAttachedMesh(collider.transform.parent);
            colliderSo.Update();
            colliderSo.FindProperty("m_Center").vector3Value = bounds.center;
            colliderSo.FindProperty("m_Size").vector3Value = bounds.size;
            colliderSo.FindProperty("m_IsTrigger").boolValue = true;
            colliderSo.ApplyModifiedProperties();
        }

        public override bool CanGrowCollider() => true;
        public override void GrowCollider(float delta)
        {
            Vector3 newSize = collider.size + Vector3.one * delta;
            newSize = Vector3.Max(newSize, Vector3.zero); // Ensure the size doesn't go below zero
            colliderSo.Update();
            colliderSo.FindProperty("m_Size").vector3Value = newSize;
            colliderSo.ApplyModifiedProperties();
        }

        public override void DrawHandles()
        {
            EditorGUI.BeginChangeCheck();

            Vector3[] handles = GetHandlePositions();

            for (int i = 0; i < handles.Length; i++)
            {
                handles[i] = collider.transform.TransformPoint(handles[i]);
                handles[i] = Handles.FreeMoveHandle(handles[i], HandleSize, Vector3.one * 0.1f, Handles.SphereHandleCap);
                handles[i] = collider.transform.InverseTransformPoint(handles[i]);
            }

            if (!EditorGUI.EndChangeCheck()) return;

            colliderSo.Update();

            Vector3 newSize = Vector3.zero;
            newSize.x = Mathf.Abs(handles[1].x - handles[0].x);
            newSize.y = Mathf.Abs(handles[3].y - handles[2].y);
            newSize.z = Mathf.Abs(handles[5].z - handles[4].z);
            colliderSo.FindProperty("m_Size").vector3Value = newSize;

            Vector3 newCenter = Vector3.zero;
            newCenter.x = (handles[1].x + handles[0].x) / 2;
            newCenter.y = (handles[3].y + handles[2].y) / 2;
            newCenter.z = (handles[5].z + handles[4].z) / 2;
            colliderSo.FindProperty("m_Center").vector3Value = newCenter;

            colliderSo.ApplyModifiedProperties();
        }

        private Vector3[] GetHandlePositions()
        {
            Vector3[] positions = new Vector3[6];

            Vector3 halfSize = collider.size / 2;
            positions[0] = collider.center + Vector3.left * halfSize.x;
            positions[1] = collider.center + Vector3.right * halfSize.x;
            positions[2] = collider.center + Vector3.down * halfSize.y;
            positions[3] = collider.center + Vector3.up * halfSize.y;
            positions[4] = collider.center + Vector3.back * halfSize.z;
            positions[5] = collider.center + Vector3.forward * halfSize.z;

            return positions;
        }
    }
}
