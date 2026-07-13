using System;

namespace DigitalTwin.Comms
{
    /// <summary>
    /// 제어 대상(시뮬레이터 또는 실장비)을 추상화하는 단일 인터페이스.
    /// HMI/시나리오는 이 인터페이스만 호출하므로, 뒤에 어떤 프로토콜(TCP/OPC-UA/SECS-GEM)이
    /// 붙어도 상위 코드는 바뀌지 않는다. 구현체를 교체하는 것만으로 모드/프로토콜이 전환된다.
    /// </summary>
    public interface IEquipmentBackend
    {
        EquipmentMode Mode { get; }
        bool IsConnected { get; }

        void Connect();
        void Disconnect();

        // ── inbound (상태 읽기) ──
        JointState      ReadJoints();
        EquipmentStatus ReadStatus();

        // ── outbound (명령) ──
        void CommandJoints(float[] targetsDeg);
        void CommandJog(int axis, float deltaDeg);
        bool CommandRemote(string command);   // "HOME" | "START" | "STOP" (GEM S2F41 대응)

        // ── 이벤트 (CEID/알람) ──
        event Action<TwinEvent> OnEvent;
    }
}
