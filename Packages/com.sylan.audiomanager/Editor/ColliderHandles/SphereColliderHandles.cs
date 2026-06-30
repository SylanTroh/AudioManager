using UnityEditor;
using UnityEngine;

namespace Sylan.AudioManager
{
    public class SphereColliderHandles : ColliderHandles<SphereCollider>
    {
        public SphereColliderHandles(SphereCollider collider)
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
            colliderSo.FindProperty("m_Radius").floatValue = bounds.extents.magnitude;
            colliderSo.FindProperty("m_IsTrigger").boolValue = true;
            colliderSo.ApplyModifiedProperties();
        }

        public override void DrawHandles()
        {
            EditorGUI.BeginChangeCheck();

            Vector3 centerHandle = GetCenterHandlePosition();
            Vector3 radiusHandle = GetRadiusHandlePosition();

            centerHandle = Handles.FreeMoveHandle(centerHandle, HandleSize, Vector3.one * 0.1f, Handles.SphereHandleCap);
            radiusHandle = Handles.FreeMoveHandle(radiusHandle, HandleSize, Vector3.one * 0.1f, Handles.SphereHandleCap);

            if (!EditorGUI.EndChangeCheck()) return;

            colliderSo.Update();

            Vector3 newCenter = collider.transform.InverseTransformPoint(centerHandle);
            Vector3 delta = newCenter - collider.center;
            colliderSo.FindProperty("m_Center").vector3Value = newCenter;

            radiusHandle += delta;
            float newRadius = Vector3.Distance(centerHandle, radiusHandle);
            colliderSo.FindProperty("m_Radius").floatValue = newRadius;

            colliderSo.ApplyModifiedProperties();
        }

        private Vector3 GetCenterHandlePosition()
        {
            return collider.transform.TransformPoint(collider.center);
        }

        private Vector3 GetRadiusHandlePosition()
        {
            return collider.transform.TransformPoint(collider.center + Vector3.up * collider.radius);
        }
    }
}
