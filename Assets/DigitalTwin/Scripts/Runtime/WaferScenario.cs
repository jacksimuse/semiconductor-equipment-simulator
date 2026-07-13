using System.Collections;
using UnityEngine;

namespace DigitalTwin
{
    /// <summary>
    /// Phase 4 — 반도체 픽/플레이스 시나리오.
    ///   - FOUP 슬롯 ↔ 챔버 간 웨이퍼 이송 (IK 타깃 추적으로 이동).
    ///   - 그립: 웨이퍼를 TCP 에 리페어런트(kinematic) → 팔을 따라 이동.
    ///   - 웨이퍼 매핑: 슬롯별 유무를 읽어 리포트.
    /// FK/IK 결과와 무관하게 조인트 계층만으로 동작.
    /// </summary>
    public class WaferScenario : MonoBehaviour
    {
        public SixAxisRobot robot;
        public RobotIK      ik;
        public Transform[]  slots;        // FOUP 슬롯 (인덱스 0 = 최하단)
        public Transform[]  wafers;       // 슬롯별 웨이퍼 (없으면 null, slots 와 동일 길이)
        public Transform    chamberSpot;  // 챔버 로드 위치

        [Tooltip("IK 도달 판정 오차 (m)")]
        public float reachTol = 0.012f;
        [Tooltip("한 지점 이동 최대 대기 (초)")]
        public float moveTimeout = 6f;
        [Tooltip("챔버 공정 대기 (초)")]
        public float processPause = 1.0f;

        public bool   IsRunning { get; private set; }
        public string Status    { get; private set; } = "대기";

        Coroutine co;

        void Awake()
        {
            if (robot == null) robot = FindFirstObjectByType<SixAxisRobot>();
            if (ik == null && robot != null) ik = robot.GetComponent<RobotIK>();
        }

        // ── 웨이퍼 매핑 ──────────────────────────────────────────
        public bool[] MapSlots()
        {
            int n = slots != null ? slots.Length : 0;
            var map = new bool[n];
            for (int i = 0; i < n; i++)
                map[i] = wafers != null && i < wafers.Length && wafers[i] != null;
            return map;
        }

        // ── 픽/플레이스 프리미티브 (즉시 실행) ────────────────────
        public void Grip(Transform wafer)
        {
            if (wafer == null || robot == null || robot.tcp == null) return;
            wafer.SetParent(robot.tcp, true);
            wafer.position = robot.tcp.position;
        }

        public void ReleaseTo(Transform wafer, Transform parent, Vector3 pos)
        {
            if (wafer == null) return;
            wafer.SetParent(parent, true);
            wafer.position = pos;
        }

        // ── 자동 사이클 ──────────────────────────────────────────
        public void RunCycle()
        {
            if (IsRunning || robot == null || ik == null || ik.target == null) return;
            co = StartCoroutine(CycleRoutine());
        }

        public void StopCycle()
        {
            if (co != null) StopCoroutine(co);
            co = null;
            IsRunning = false;
            if (ik != null) ik.follow = false;
            Status = "정지";
        }

        IEnumerator CycleRoutine()
        {
            IsRunning = true;

            int slot = -1;
            var map = MapSlots();
            for (int i = 0; i < map.Length; i++) if (map[i]) { slot = i; break; }
            if (slot < 0) { Status = "웨이퍼 없음"; IsRunning = false; yield break; }

            var wafer = wafers[slot];

            Status = $"슬롯{slot + 1} 접근";
            yield return MoveTo(slots[slot].position);

            Status = "그립";
            Grip(wafer);
            wafers[slot] = null;                 // 매핑 반영 (슬롯 비움)
            yield return null;

            Status = "챔버로 이송";
            yield return MoveTo(chamberSpot.position);

            Status = "챔버 배치 (공정)";
            ReleaseTo(wafer, chamberSpot, chamberSpot.position);
            yield return new WaitForSeconds(processPause);

            Status = "챔버에서 픽";
            yield return MoveTo(chamberSpot.position);
            Grip(wafer);

            Status = $"슬롯{slot + 1} 복귀";
            yield return MoveTo(slots[slot].position);
            ReleaseTo(wafer, slots[slot], slots[slot].position);
            wafers[slot] = wafer;                // 매핑 복원 (슬롯 채움)

            if (ik != null) ik.follow = false;
            Status = "완료";
            IsRunning = false;
        }

        IEnumerator MoveTo(Vector3 pos)
        {
            ik.target.position = pos;
            ik.follow = true;
            float t = 0f;
            while (ik.DistanceToTarget > reachTol && t < moveTimeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }
}
