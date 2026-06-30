using UnityEngine;

namespace Sylan.AudioManager
{
    public class CapsuleColliderHandles : ColliderHandles<CapsuleCollider>
    {
        public CapsuleColliderHandles(CapsuleCollider collider)
            : base(collider)
        { }

        public override bool CanResetLocation() => true;
        public override void ResetLocation() => ResetTransformPositionAndRotation();

        public override bool IsRelevantToAudioZones() => true;
    }
}
