using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Enums;

namespace Sylan.AudioManager
{
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioZonePlayerFollower : UdonSharpBehaviour
    {
        public HumanBodyBones trackedBone = HumanBodyBones.Hips;

        [Tooltip("Interval of updating the position in seconds. 0 will lead to an update every frame")] 
        [Range(0f, 1f)]
        public float positionUpdateInterval = .1f;

        private bool _avatarHasBone = false;
        private float _avatarHeightInMeters = 0f;

        private void Start()
        {
            OnAvatarChanged(Networking.LocalPlayer);
            UpdatePosition();
        }

        public void UpdatePosition()
        {
            var position = _avatarHasBone ? Networking.LocalPlayer.GetBonePosition(trackedBone) : Networking.LocalPlayer.GetPosition();
            var rotation = _avatarHasBone ? Networking.LocalPlayer.GetBoneRotation(trackedBone) : Networking.LocalPlayer.GetRotation();
            if (!_avatarHasBone)
            {
                //Move the position up to be about hip height. We don't want to collide with anything beneath the floor.
                position.y += _avatarHeightInMeters / 2;
            }

            transform.SetPositionAndRotation(position, rotation);

            //EventTiming is important for us to update the position only after player collisions with AudioZone were handled
            SendCustomEventDelayedSeconds(nameof(UpdatePosition), positionUpdateInterval, EventTiming.LateUpdate);
        }

        public override void OnAvatarChanged(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player) || !player.IsValid() || !player.isLocal) return;
            _avatarHasBone = player.GetBonePosition(trackedBone) != Vector3.zero && player.GetBoneRotation(trackedBone) != Quaternion.identity;
            _avatarHeightInMeters = player.GetAvatarEyeHeightAsMeters();
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
        {
            OnAvatarChanged(player);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!Utilities.IsValid(other)) return;
            var audioZoneCollider = other.gameObject.GetComponent<AudioZoneCollider>();
            if (!Utilities.IsValid(audioZoneCollider)) return;
            
            //We want to react to OnTriggerEnter after the normal OnPlayerEnter is handled.
            //To make sure of it, we delay it a little bit
            audioZoneCollider.SendCustomEventDelayedFrames(nameof(audioZoneCollider.OnLocalPlayerEnterStationFix), 5, EventTiming.LateUpdate);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!Utilities.IsValid(other)) return;
            var audioZoneCollider = other.gameObject.GetComponent<AudioZoneCollider>();
            if (!Utilities.IsValid(audioZoneCollider)) return;
            
            //We want to react to TriggerExit after the normal OnPlayerExit is handled.
            //But thanks to follower collider possibly being smaller than player Collider, OnTriggerExit can trigger before OnPlayerExit
            //Thus we delay it to make sure.
            audioZoneCollider.SendCustomEventDelayedFrames(nameof(audioZoneCollider.OnLocalPlayerExitStationFix), 5, EventTiming.LateUpdate);
        }
    }
}