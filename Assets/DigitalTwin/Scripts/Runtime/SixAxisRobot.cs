using UnityEngine;

namespace DigitalTwin
{
    /// <summary>
    /// 6축 로봇의 관절 각도를 관리하고 FK(순기구학)로 자세를 갱신하는 컴포넌트.
    /// Phase 0-1: 물리(ArticulationBody) 대신 Transform 계층 기반 kinematic 방식.
    ///   - 결정론적이고 지터가 없어 시각화/조그에 적합.
    ///   - Phase 5+ 에서 URDF Importer / ArticulationBody 로 교체 가능.
    /// </summary>
    public class SixAxisRobot : MonoBehaviour
    {
        [System.Serializable]
        public class Joint
        {
            public string name = "J";
            public Transform pivot;             // 회전하는 축 오브젝트
            public Vector3 axis = Vector3.up;   // 로컬 회전축
            public float min = -180f;
            public float max = 180f;
            public float target;                // 현재 목표 각도(deg)
            [HideInInspector] public Quaternion rest = Quaternion.identity; // 0도일 때 로컬 회전
        }

        public Joint[] joints = new Joint[6];
        public Transform tcp;   // Tool Center Point (엔드이펙터 기준점)

        [Header("Gizmo")]
        public bool  showTcpGizmo = true;   // TCP 좌표계 기즈모 표시
        public float gizmoAxisLength = 0.15f;

        void Awake()  => CacheRest();
        void Update() => ApplyFK();

        /// <summary>현재 로컬 회전을 "0도 기준(rest)"으로 캐싱.</summary>
        public void CacheRest()
        {
            foreach (var j in joints)
                if (j != null && j.pivot != null)
                    j.rest = j.pivot.localRotation;
        }

        /// <summary>모든 관절의 목표 각도를 계층 회전에 반영 (FK).</summary>
        public void ApplyFK()
        {
            foreach (var j in joints)
            {
                if (j == null || j.pivot == null) continue;
                float a = Mathf.Clamp(j.target, j.min, j.max);
                j.pivot.localRotation = j.rest * Quaternion.AngleAxis(a, j.axis);
            }
        }

        public void SetJoint(int i, float deg)
        {
            if (i < 0 || i >= joints.Length || joints[i] == null) return;
            joints[i].target = Mathf.Clamp(deg, joints[i].min, joints[i].max);
        }

        public void Home()
        {
            foreach (var j in joints) if (j != null) j.target = 0f;
        }

        public Vector3 TcpPosition => tcp ? tcp.position : transform.position;
        public Vector3 TcpEuler    => tcp ? tcp.eulerAngles : Vector3.zero;

        // ── TCP 좌표계 기즈모 (X=빨강, Y=초록, Z=파랑) ─────────────────────
        void OnDrawGizmos()
        {
            if (!showTcpGizmo || tcp == null) return;
            float len = gizmoAxisLength;
            DrawAxis(tcp.position, tcp.right,   len, Color.red);   // X
            DrawAxis(tcp.position, tcp.up,      len, Color.green); // Y
            DrawAxis(tcp.position, tcp.forward, len, Color.blue);  // Z
        }

        static void DrawAxis(Vector3 origin, Vector3 dir, float len, Color col)
        {
            Gizmos.color = col;
            Vector3 end = origin + dir * len;
            Gizmos.DrawLine(origin, end);

            // 화살촉
            Vector3 ortho = Vector3.Cross(dir, Vector3.up);
            if (ortho.sqrMagnitude < 1e-4f) ortho = Vector3.Cross(dir, Vector3.right);
            ortho.Normalize();
            Vector3 baseP = end - dir * (len * 0.22f);
            float w = len * 0.10f;
            Gizmos.DrawLine(end, baseP + ortho * w);
            Gizmos.DrawLine(end, baseP - ortho * w);
        }
    }
}
