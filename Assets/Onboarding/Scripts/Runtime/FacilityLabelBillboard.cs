using UnityEngine;

namespace Onboarding
{
    public class FacilityLabelBillboard : MonoBehaviour
    {
        public bool keepUpright = true;
        public bool hideNearPlayer = true;
        public float hideDistance = 2.4f;

        Camera targetCamera;
        Renderer labelRenderer;
        Transform player;

        void Awake()
        {
            labelRenderer = GetComponent<Renderer>();
        }

        void LateUpdate()
        {
            UpdateVisibility();

            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null) return;

            Vector3 toCamera = targetCamera.transform.position - transform.position;
            if (keepUpright) toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        }

        void UpdateVisibility()
        {
            if (!hideNearPlayer || hideDistance <= 0f) return;

            if (player == null)
            {
                var controller = FindAnyObjectByType<EngineerPlayerController>();
                if (controller != null)
                    player = controller.transform;
            }
            if (player == null || labelRenderer == null) return;

            Vector3 delta = player.position - transform.position;
            delta.y = 0f;
            labelRenderer.enabled = delta.sqrMagnitude > hideDistance * hideDistance;
        }
    }
}
