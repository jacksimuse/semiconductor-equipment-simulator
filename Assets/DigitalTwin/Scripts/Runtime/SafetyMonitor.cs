using System.Collections.Generic;
using UnityEngine;

namespace DigitalTwin
{
    /// <summary>
    /// Phase 6 (부분) — 충돌 감지 인터락(E-stop).
    ///
    /// 이 로봇은 물리(ArticulationBody)가 아니라 Transform 계층 기반 kinematic 이므로,
    /// 콜라이더를 붙여도 "물리적으로 밀려나지" 않는다. 대신 매 프레임 로봇 콜라이더와
    /// 장애물 콜라이더의 관통(penetration)을 직접 질의해, 파고들면 모션을 정지(E-stop)한다.
    ///   - Physics.ComputePenetration 사용 → Rigidbody/시뮬레이션 스텝 불필요(결정론적).
    ///   - Time.timeScale=0(헤드리스 제어) 상태에서도 Update 는 돌아가므로 검증에 안전.
    ///
    /// 장애물 판정: obstacleMask 레이어에 속한 콜라이더. (로봇/바닥은 제외)
    /// </summary>
    public class SafetyMonitor : MonoBehaviour
    {
        public SixAxisRobot  robot;
        public RobotIK       ik;
        public WaferScenario scenario;

        [Tooltip("장애물로 취급할 레이어 (FOUP/챔버 등)")]
        public LayerMask obstacleMask;

        [Tooltip("이보다 깊게 파고들면 충돌로 판정 (m)")]
        public float penetrationTol = 0.003f;

        public bool   EStop     { get; private set; }
        public string LastEvent { get; private set; } = "";

        readonly List<Collider> robotCols    = new List<Collider>();
        readonly List<Collider> obstacleCols = new List<Collider>();

        void Awake()
        {
            if (robot == null) robot = GetComponent<SixAxisRobot>();
            if (ik == null && robot != null) ik = robot.GetComponent<RobotIK>();
            if (scenario == null) scenario = FindFirstObjectByType<WaferScenario>();
            RebuildColliderCache();
        }

        /// <summary>로봇/장애물 콜라이더 목록을 다시 수집. (씬 재생성 후 호출)</summary>
        public void RebuildColliderCache()
        {
            robotCols.Clear();
            obstacleCols.Clear();

            if (robot != null)
                foreach (var c in robot.GetComponentsInChildren<Collider>(true))
                    if (c != null && !c.isTrigger) robotCols.Add(c);

            foreach (var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if (c != null && ((obstacleMask.value & (1 << c.gameObject.layer)) != 0))
                    obstacleCols.Add(c);
        }

        void Update() => CheckNow();

        /// <summary>충돌을 1회 검사. 관통 발견 시 E-stop 발동하고 true 반환. (Update + 외부 검증에서 호출)</summary>
        public bool CheckNow()
        {
            if (EStop) return true;
            if (robotCols.Count == 0 || obstacleCols.Count == 0) return false;

            // 수동으로 Transform 을 움직이므로 질의 전에 물리 좌표를 동기화.
            Physics.SyncTransforms();

            for (int i = 0; i < robotCols.Count; i++)
            {
                var a = robotCols[i];
                if (a == null) continue;
                for (int j = 0; j < obstacleCols.Count; j++)
                {
                    var b = obstacleCols[j];
                    if (b == null) continue;

                    if (Physics.ComputePenetration(
                            a, a.transform.position, a.transform.rotation,
                            b, b.transform.position, b.transform.rotation,
                            out _, out float dist)
                        && dist > penetrationTol)
                    {
                        Trigger($"{a.transform.parent?.name}/{a.name} ↔ {b.name} (침투 {dist * 1000f:F1}mm)");
                        return true;
                    }
                }
            }
            return false;
        }

        void Trigger(string what)
        {
            EStop     = true;
            LastEvent = what;
            if (scenario != null) scenario.StopCycle();
            if (ik != null) ik.follow = false;
            Debug.LogWarning($"[SafetyMonitor] ⛔ E-STOP — 충돌 감지: {what}");
        }

        /// <summary>E-stop 해제. 겹침이 남아 있으면 다음 프레임 재발동.</summary>
        public void ResetEStop()
        {
            EStop = false;
            LastEvent = "";
        }
    }
}
