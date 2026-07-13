using UnityEngine;

namespace DigitalTwin
{
    /// <summary>
    /// Phase 2 — CCD(Cyclic Coordinate Descent) 기반 위치 IK.
    /// target 위치로 TCP가 도달하도록 J1~J6 각도를 반복 수렴시킨다.
    ///   - 각 관절을 회전축(hinge)에 투영해 보정 → 조인트 축/한계를 그대로 존중.
    ///   - FK 계산은 SixAxisRobot.ApplyFK 를 재사용 (미터·Y-up 좌표계 유지).
    /// Phase 2 v1 은 "위치"만 목표. 자세(방향)는 이후 확장.
    /// </summary>
    [RequireComponent(typeof(SixAxisRobot))]
    public class RobotIK : MonoBehaviour
    {
        [Tooltip("TCP가 도달할 목표점 (씬에서 드래그)")]
        public Transform target;

        [Tooltip("true면 매 프레임 target 을 추적")]
        public bool follow = false;

        [Tooltip("프레임당 CCD 반복 횟수")]
        [Range(1, 20)] public int iterations = 10;

        [Tooltip("도달 허용 오차 (m)")]
        public float tolerance = 0.002f;

        [Tooltip("반복당 관절 최대 회전량 (deg) — 클수록 빠르지만 덜 부드러움")]
        public float maxStepDeg = 20f;

        SixAxisRobot robot;

        void Awake() => robot = GetComponent<SixAxisRobot>();

        void Update()
        {
            if (follow && target != null) Solve();
        }

        /// <summary>CCD 반복으로 target 에 TCP를 맞춘다.</summary>
        public void Solve()
        {
            if (robot == null) robot = GetComponent<SixAxisRobot>();
            if (robot == null || robot.tcp == null || target == null) return;

            float tol2 = tolerance * tolerance;

            for (int it = 0; it < iterations; it++)
            {
                // 팁(J6)에서 베이스(J1) 방향으로 한 관절씩 보정
                for (int i = robot.joints.Length - 1; i >= 0; i--)
                {
                    var j = robot.joints[i];
                    if (j == null || j.pivot == null) continue;

                    Vector3 axisW = j.pivot.TransformDirection(j.axis).normalized;
                    Vector3 piv   = j.pivot.position;
                    Vector3 toEnd = robot.tcp.position - piv;
                    Vector3 toTgt = target.position    - piv;

                    // 회전축에 수직인 평면으로 투영 (힌지 제약)
                    toEnd -= Vector3.Dot(toEnd, axisW) * axisW;
                    toTgt -= Vector3.Dot(toTgt, axisW) * axisW;
                    if (toEnd.sqrMagnitude < 1e-8f || toTgt.sqrMagnitude < 1e-8f) continue;

                    float ang = Vector3.SignedAngle(toEnd, toTgt, axisW);
                    ang = Mathf.Clamp(ang, -maxStepDeg, maxStepDeg);

                    j.target = Mathf.Clamp(j.target + ang, j.min, j.max);
                    robot.ApplyFK();   // 다음 관절 계산을 위해 즉시 반영
                }

                if ((robot.tcp.position - target.position).sqrMagnitude <= tol2)
                    break;
            }
        }

        /// <summary>TCP–target 거리(m). 참조가 없으면 -1.</summary>
        public float DistanceToTarget =>
            (robot != null && robot.tcp != null && target != null)
                ? Vector3.Distance(robot.tcp.position, target.position) : -1f;

        void OnDrawGizmos()
        {
            if (target == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(target.position, 0.03f);
            if (robot != null && robot.tcp != null)
                Gizmos.DrawLine(robot.tcp.position, target.position);
        }
    }
}
