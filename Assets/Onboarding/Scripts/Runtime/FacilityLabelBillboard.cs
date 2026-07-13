using UnityEngine;

namespace Onboarding
{
    public class FacilityLabelBillboard : MonoBehaviour
    {
        public bool keepUpright = true;

        Camera targetCamera;

        void LateUpdate()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null) return;

            Vector3 toCamera = targetCamera.transform.position - transform.position;
            if (keepUpright) toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        }
    }
}
