using System.IO;
using UnityEngine;

namespace Onboarding
{
    public class PlayerImageAvatar : MonoBehaviour
    {
        public Texture2D portraitTexture;
        public string fallbackFileName = "플레이어.png";
        public float avatarHeight = 2.15f;
        public Vector3 localOffset = new Vector3(0f, 0.05f, 0f);
        public bool keepUpright = true;

        Transform avatarVisual;
        Camera targetCamera;

        void Awake()
        {
            HidePlaceholderRenderers();
            EnsureTexture();
            EnsureVisual();
        }

        void LateUpdate()
        {
            if (avatarVisual == null) return;

            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null) return;

            Vector3 toCamera = targetCamera.transform.position - avatarVisual.position;
            if (keepUpright) toCamera.y = 0f;
            if (toCamera.sqrMagnitude < 0.0001f) return;

            avatarVisual.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        }

        void HidePlaceholderRenderers()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.transform == transform || renderer.transform.name == "SafetyHelmet")
                    renderer.enabled = false;
            }
        }

        void EnsureTexture()
        {
            if (portraitTexture != null) return;

            string path = Path.Combine(Application.dataPath, fallbackFileName);
            if (!File.Exists(path)) return;

            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                return;
            }

            texture.name = Path.GetFileNameWithoutExtension(fallbackFileName);
            portraitTexture = texture;
        }

        void EnsureVisual()
        {
            var existing = transform.Find("PlayerImageBillboard");
            if (existing != null)
            {
                avatarVisual = existing;
                return;
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "PlayerImageBillboard";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = localOffset;

            var collider = quad.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateAvatarMaterial();

            float aspect = portraitTexture != null && portraitTexture.height > 0
                ? (float)portraitTexture.width / portraitTexture.height
                : 0.65f;
            quad.transform.localScale = new Vector3(avatarHeight * aspect, avatarHeight, 1f);

            avatarVisual = quad.transform;
        }

        Material CreateAvatarMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader);
            if (portraitTexture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", portraitTexture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", portraitTexture);
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);

            return material;
        }
    }
}
