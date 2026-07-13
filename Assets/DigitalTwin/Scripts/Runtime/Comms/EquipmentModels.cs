using UnityEngine;

namespace DigitalTwin.Comms
{
    /// <summary>Sim(장비 없음) ↔ DigitalTwin(실장비 미러) 동작 모드.</summary>
    public enum EquipmentMode { Simulator, DigitalTwin }

    /// <summary>GEM 제어상태 서브셋 (Phase 5b 에서 호스트 통신에 사용).</summary>
    public enum ControlState { EquipmentOffline, Local, Remote }

    /// <summary>로봇 관절/엔드이펙터 상태 스냅샷 (inbound/outbound 공통).</summary>
    public struct JointState
    {
        public float[] joints;   // 관절각(deg), 길이 6
        public Vector3 tcpPos;   // TCP 위치(m)
        public Vector3 tcpEuler; // TCP 자세(deg)
        public double  tSec;     // 타임스탬프(초)
    }

    /// <summary>장비 상태 요약. GEM SVID/알람에 매핑됨.</summary>
    public struct EquipmentStatus
    {
        public ControlState control;
        public bool   running;    // 사이클 실행 중
        public bool   eStop;      // 충돌/알람(E-stop)
        public int    waferCount; // FOUP 내 웨이퍼 수
        public string text;       // 상태 문자열
    }

    /// <summary>수집 이벤트(GEM CEID 에 대응).</summary>
    public struct TwinEvent
    {
        public int    id;
        public string name;
        public string info;

        public TwinEvent(int id, string name, string info = "")
        {
            this.id = id; this.name = name; this.info = info;
        }
    }
}
