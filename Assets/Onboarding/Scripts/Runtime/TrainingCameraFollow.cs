using UnityEngine;

namespace Onboarding
{
    public class TrainingCameraFollow : MonoBehaviour
    {
        public enum PlayerCameraMode
        {
            Follow,
            Overhead,
            Front
        }

        public Transform target;
        public Vector3 playerOffset = new Vector3(0f, 3.2f, 5.2f);
        public Vector3 overheadOffset = new Vector3(0f, 6.8f, 1.2f);
        public Vector3 frontOffset = new Vector3(0f, 2.8f, -4.8f);
        public Vector3 equipmentOffset = new Vector3(1.8f, 1.45f, 2.4f);
        public float followSmooth = 8f;
        public float playerLookHeight = 1.1f;
        public float overheadLookHeight = 0.3f;
        public float equipmentLookHeight = 0.75f;

        Vector3 activeOffset;
        float activeLookHeight;
        PlayerCameraMode playerMode;

        void Awake()
        {
            activeOffset = playerOffset;
            activeLookHeight = playerLookHeight;
        }

        public void FollowPlayer(Transform followTarget)
        {
            target = followTarget;
            ApplyPlayerMode(playerMode);
        }

        public void FocusEquipment(Transform equipmentTarget)
        {
            target = equipmentTarget;
            activeOffset = equipmentOffset;
            activeLookHeight = equipmentLookHeight;
        }

        public void CyclePlayerCamera()
        {
            playerMode = playerMode switch
            {
                PlayerCameraMode.Follow => PlayerCameraMode.Overhead,
                PlayerCameraMode.Overhead => PlayerCameraMode.Front,
                _ => PlayerCameraMode.Follow
            };
            ApplyPlayerMode(playerMode);
        }

        void ApplyPlayerMode(PlayerCameraMode mode)
        {
            activeOffset = mode switch
            {
                PlayerCameraMode.Overhead => overheadOffset,
                PlayerCameraMode.Front => frontOffset,
                _ => playerOffset
            };

            activeLookHeight = mode == PlayerCameraMode.Overhead ? overheadLookHeight : playerLookHeight;
        }

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + activeOffset;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSmooth * Time.deltaTime));
            transform.LookAt(target.position + Vector3.up * activeLookHeight);
        }
    }
}
