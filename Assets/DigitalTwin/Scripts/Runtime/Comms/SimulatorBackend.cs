using System;
using UnityEngine;

namespace DigitalTwin.Comms
{
    /// <summary>
    /// 장비가 없을 때: Unity 로봇 자체를 "장비"로 취급하는 백엔드.
    /// 네트워크·DLL 불필요. 명령은 로봇에 직접, 상태는 FK 결과에서 읽는다.
    /// (Phase 5b 에서 SecsGemBackend 가 이 백엔드를 감싸 가상 GEM 장비로 확장.)
    /// </summary>
    public class SimulatorBackend : IEquipmentBackend
    {
        readonly SixAxisRobot  robot;
        readonly RobotIK       ik;
        readonly WaferScenario scenario;
        readonly SafetyMonitor safety;

        public EquipmentMode Mode => EquipmentMode.Simulator;
        public bool IsConnected { get; private set; }
        public event Action<TwinEvent> OnEvent;

        public SimulatorBackend(SixAxisRobot robot, RobotIK ik, WaferScenario scenario, SafetyMonitor safety)
        {
            this.robot = robot; this.ik = ik; this.scenario = scenario; this.safety = safety;
        }

        public void Connect()    => IsConnected = true;
        public void Disconnect() => IsConnected = false;

        public JointState ReadJoints()
        {
            int n = (robot != null && robot.joints != null) ? robot.joints.Length : 0;
            var js = new JointState { joints = new float[n], tSec = Time.timeAsDouble };
            for (int i = 0; i < n; i++)
                js.joints[i] = robot.joints[i] != null ? robot.joints[i].target : 0f;
            if (robot != null) { js.tcpPos = robot.TcpPosition; js.tcpEuler = robot.TcpEuler; }
            return js;
        }

        public EquipmentStatus ReadStatus()
        {
            return new EquipmentStatus
            {
                control    = ControlState.Local,
                running    = scenario != null && scenario.IsRunning,
                eStop      = safety   != null && safety.EStop,
                waferCount = CountWafers(),
                text       = scenario != null ? scenario.Status : "대기"
            };
        }

        public void CommandJoints(float[] targetsDeg)
        {
            if (robot == null || targetsDeg == null) return;
            for (int i = 0; i < targetsDeg.Length; i++) robot.SetJoint(i, targetsDeg[i]);
        }

        public void CommandJog(int axis, float deltaDeg)
        {
            if (robot == null || robot.joints == null || axis < 0 || axis >= robot.joints.Length) return;
            robot.SetJoint(axis, robot.joints[axis].target + deltaDeg);
        }

        public bool CommandRemote(string command)
        {
            switch ((command ?? "").Trim().ToUpperInvariant())
            {
                case "HOME":
                    if (robot == null) return false;
                    if (ik != null) ik.follow = false;
                    robot.Home();
                    Emit(1, "HOME");
                    return true;
                case "START":
                    if (scenario == null) return false;
                    scenario.RunCycle();
                    Emit(2, "CYCLE_START");
                    return true;
                case "STOP":
                    if (scenario == null) return false;
                    scenario.StopCycle();
                    Emit(3, "CYCLE_STOP");
                    return true;
                default:
                    return false;
            }
        }

        int CountWafers()
        {
            if (scenario == null || scenario.wafers == null) return 0;
            int c = 0;
            foreach (var w in scenario.wafers) if (w != null) c++;
            return c;
        }

        void Emit(int id, string name)
        {
            var h = OnEvent;
            if (h != null) h(new TwinEvent(id, name));
        }
    }
}
