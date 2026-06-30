using UnityEngine;

namespace Sylan.AudioManager
{
    public class MeshColliderHandles : ColliderHandles<MeshCollider>
    {
        public MeshColliderHandles(MeshCollider collider)
            : base(collider)
        { }

        public override bool IsRelevantToAudioZones() => ZoneEditor.IsRelevantMeshCollider(collider);
    }
}
