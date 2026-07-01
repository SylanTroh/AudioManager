using UnityEditor;
using UnityEngine;

namespace Sylan.AudioManager
{
    public abstract class ColliderHandles
    {
        protected const float HandleSize = 0.1f;

        public abstract bool IsRelevantToAudioZones();
        public virtual bool CanResetLocation() => false;
        public virtual void ResetLocation() { }
        public virtual bool CanResetCollider() => false;
        public virtual void ResetCollider() { }
        public virtual bool CanGrowCollider() => false;
        public virtual void GrowCollider(float delta) { }
        public virtual void DrawHandles() { }

        public static bool TryCreate(Collider collider, out ColliderHandles handles)
        {
            handles = collider switch
            {
                BoxCollider boxCollider => new BoxColliderHandles(boxCollider),
                SphereCollider sphereCollider => new SphereColliderHandles(sphereCollider),
                CapsuleCollider capsuleCollider => new CapsuleColliderHandles(capsuleCollider),
                MeshCollider meshCollider => new MeshColliderHandles(meshCollider),
                _ => null, // 2D colliders, etc.
            };
            return handles != null;
        }
    }

    public abstract class ColliderHandles<T> : ColliderHandles
        where T : Collider
    {
        protected T collider;
        protected SerializedObject colliderSo;

        protected ColliderHandles(T collider)
        {
            this.collider = collider;
            colliderSo = new(collider);
        }

        protected Bounds GetBoundsFromAttachedMesh(Transform transform)
        {
            MeshFilter meshFilter = transform?.gameObject.GetComponent<MeshFilter>();
            return meshFilter?.sharedMesh.bounds ?? new Bounds(Vector3.zero, Vector3.one);
        }

        protected void ResetTransformPositionAndRotation()
        {
            SerializedObject so = new(collider.transform);
            so.FindProperty("m_LocalPosition").vector3Value = Vector3.zero;
            so.FindProperty("m_LocalRotation").quaternionValue = Quaternion.identity;
            so.ApplyModifiedProperties();
        }
    }
}
