using UnityEngine;

namespace DigitalTwin.Stations.Robot
{
    /// <summary>
    /// 6축 로봇 웨이퍼 이송 장비를 게임 셸 계약(StationBase)에 연결하는 얇은 어댑터.
    /// 기존 SixAxisRobot / RobotIK / WaferScenario / SafetyMonitor / RobotJogUI 를 재사용한다.
    ///   verb: "StartCycle" / "StopCycle" / "ResetEStop" / "Home"
    ///   상태: busy=사이클중, eStop=충돌E-stop, text=시나리오상태, lastEvent=안전이벤트
    /// 참조는 Awake 에서 자동 배선(빌더가 채우지 않아도 동작).
    /// </summary>
    public class RobotStation : StationBase
    {
        public SixAxisRobot  robot;
        public RobotIK       ik;
        public WaferScenario scenario;
        public SafetyMonitor safety;
        public RobotJogUI    jogUI;

        void Awake()
        {
            if (robot == null)    robot    = GetComponent<SixAxisRobot>();
            if (ik == null)       ik       = GetComponent<RobotIK>();
            if (safety == null)   safety   = GetComponent<SafetyMonitor>();
            if (jogUI == null)    jogUI    = GetComponent<RobotJogUI>();
            if (scenario == null) scenario = FindAnyObjectByType<WaferScenario>();
        }

        protected override void OnEnter()
        {
            if (jogUI) jogUI.enabled = true;   // 제어 UI 활성
        }

        protected override void OnExit()
        {
            if (scenario) scenario.StopCycle();
            if (ik) ik.follow = false;
            if (jogUI) jogUI.enabled = false;
        }

        public override StationStatus GetStatus()
        {
            bool running = scenario != null && scenario.IsRunning;
            bool estop   = safety != null && safety.EStop;
            return new StationStatus
            {
                busy      = running,
                eStop     = estop,
                fault     = estop,
                progress  = 0f,   // 사이클 진행도는 추후 WaferScenario 확장 시 연결
                text      = scenario != null ? scenario.Status : (robot != null ? "대기" : "미연결"),
                lastEvent = safety != null ? safety.LastEvent : ""
            };
        }

        public override bool Command(string verb)
        {
            switch ((verb ?? "").Trim())
            {
                case "StartCycle":
                    if (scenario == null) return false;
                    scenario.RunCycle();
                    return true;
                case "StopCycle":
                    if (scenario == null) return false;
                    scenario.StopCycle();
                    return true;
                case "ResetEStop":
                    if (safety == null) return false;
                    safety.ResetEStop();
                    if (ik) ik.follow = false;
                    return true;
                case "Home":
                    if (robot == null) return false;
                    if (ik) ik.follow = false;
                    robot.Home();
                    return true;
                default:
                    return false;
            }
        }
    }
}
