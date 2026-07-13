using UnityEngine;

namespace Onboarding
{
    public class TrainingCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 playerOffset = new Vector3(0f, 3.2f, -5.2f);
        public Vector3 equipmentOffset = new Vector3(1.8f, 1.45f, -2.4f);
        public float followSmooth = 8f;
        public float playerLookHeight = 1.1f;
        public float equipmentLookHeight = 0.75f;

        Vector3 activeOffset;
        float activeLookHeight;

        void Awake()
        {
            activeOffset = playerOffset;
            activeLookHeight = playerLookHeight;
        }

        public void FollowPlayer(Transform followTarget)
        {
            target = followTarget;
            activeOffset = playerOffset;
            activeLookHeight = playerLookHeight;
        }

        public void FocusEquipment(Transform equipmentTarget)
        {
            target = equipmentTarget;
            activeOffset = equipmentOffset;
            activeLookHeight = equipmentLookHeight;
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
