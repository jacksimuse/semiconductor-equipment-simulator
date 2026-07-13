using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Onboarding
{
    [RequireComponent(typeof(CharacterController))]
    public class EngineerPlayerController : MonoBehaviour
    {
        public Transform cameraTransform;
        public float moveSpeed = 3.2f;
        public float turnSpeed = 540f;
        public float gravity = -18f;

        CharacterController controller;
        float verticalVelocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            Vector2 input = ReadMoveInput();
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }

            Vector3 move = forward * input.y + right * input.x;
            if (move.sqrMagnitude > 1f) move.Normalize();

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -1f;
            verticalVelocity += gravity * Time.deltaTime;

            controller.Move((move * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }

        static Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            Vector2 input = Vector2.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            return input;
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }
    }
}
