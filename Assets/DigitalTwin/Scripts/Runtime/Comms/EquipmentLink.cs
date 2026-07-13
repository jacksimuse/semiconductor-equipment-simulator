using UnityEngine;

namespace DigitalTwin.Comms
{
    /// <summary>
    /// 활성 백엔드를 선택·보유하고, 모드에 따라 트윈을 구동한다.
    ///   - Simulator 모드: Unity 로봇이 마스터. (HMI 가 직접 조작)
    ///   - DigitalTwin 모드: 실장비가 마스터. 매 프레임 ReadJoints() 를 로봇에 미러링.
    /// 시작 시 하드웨어 연결을 시도하고, 실패하면 시뮬레이터로 폴백한다.
    /// (Phase 5a 는 SimulatorBackend 만 존재. 5b 에서 SecsGemBackend 를 여기서 시도.)
    /// </summary>
    public class EquipmentLink : MonoBehaviour
    {
        [Tooltip("선호 모드. 하드웨어 연결 실패 시 autoFallbackToSim 이면 Sim 으로 전환")]
        public EquipmentMode preferredMode = EquipmentMode.Simulator;
        public bool autoFallbackToSim = true;

        public IEquipmentBackend Backend { get; private set; }
        public EquipmentMode Mode => Backend != null ? Backend.Mode : EquipmentMode.Simulator;

        SixAxisRobot  robot;
        RobotIK       ik;
        WaferScenario scenario;
        SafetyMonitor safety;

        void Awake()
        {
            robot    = GetComponent<SixAxisRobot>();
            ik       = GetComponent<RobotIK>();
            safety   = GetComponent<SafetyMonitor>();
            scenario = FindAnyObjectByType<WaferScenario>();
            MainThreadDispatcher.Ensure();
        }

        void Start() => SelectBackend();

        void SelectBackend()
        {
            // Phase 5b: preferredMode==DigitalTwin 이면 SecsGemBackend.Connect() 시도,
            //           실패 && autoFallbackToSim → SimulatorBackend 로 폴백.
            var sim = new SimulatorBackend(robot, ik, scenario, safety);
            sim.Connect();
            Backend = sim;
            Debug.Log($"[EquipmentLink] 백엔드 = {Backend.Mode} (connected={Backend.IsConnected})");
        }

        void Update()
        {
            if (Backend == null || robot == null) return;
            if (Backend.Mode == EquipmentMode.DigitalTwin)
            {
                var js = Backend.ReadJoints();   // 실장비 → 트윈 미러링
                if (js.joints != null)
                    for (int i = 0; i < js.joints.Length; i++) robot.SetJoint(i, js.joints[i]);
            }
        }

        // ── HMI 편의 API (항상 인터페이스 경유) ──
        public void CmdHome()  { if (Backend != null) Backend.CommandRemote("HOME"); }
        public void CmdStart() { if (Backend != null) Backend.CommandRemote("START"); }
        public void CmdStop()  { if (Backend != null) Backend.CommandRemote("STOP"); }
        public EquipmentStatus Status() => Backend != null ? Backend.ReadStatus() : default;
    }
}
