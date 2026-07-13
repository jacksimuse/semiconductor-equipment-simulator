using UnityEngine;

namespace DigitalTwin
{
    /// <summary>
    /// Play 모드에서 6축 조그 슬라이더 + TCP 좌표를 표시하는 IMGUI 패널.
    /// 씬 배선(Canvas/Slider) 불필요 — 컴포넌트만 붙으면 동작.
    /// </summary>
    [RequireComponent(typeof(SixAxisRobot))]
    public class RobotJogUI : MonoBehaviour
    {
        static int nextWindowId = 61000;

        [Tooltip("조그 패널 전체 확대 배율 (클수록 크게 보임)")]
        public float uiScale = 2.2f;

        [Tooltip("-/+ 버튼을 누르고 있을 때 연속 회전 속도 (°/초)")]
        public float jogSpeed = 60f;

        SixAxisRobot robot;
        RobotIK ik;
        RobotTeach teach;
        WaferScenario scenario;
        Rect win = new Rect(12, 12, 360, 0);
        int windowId;

        void Awake()
        {
            robot = GetComponent<SixAxisRobot>();
            ik    = GetComponent<RobotIK>();
            teach = GetComponent<RobotTeach>();
            windowId = nextWindowId++;
        }

        void OnGUI()
        {
            if (robot == null) return;

            // 전체 IMGUI를 좌상단 기준으로 확대 → 글자/슬라이더가 함께 커진다.
            Matrix4x4 prev = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(uiScale, uiScale), Vector2.zero);
            win = GUILayout.Window(windowId, win, DrawWindow, "6축 로봇 조그 (Jog)");
            GUI.matrix = prev;
        }

        void DrawWindow(int id)
        {
            // hold 버튼 증분은 프레임당 1회만 적용 (OnGUI 는 Layout+Repaint 로 매 프레임 2회 호출됨).
            bool  repaint = Event.current.type == EventType.Repaint;
            float step    = jogSpeed * Time.deltaTime;

            for (int i = 0; i < robot.joints.Length; i++)
            {
                var j = robot.joints[i];
                if (j == null) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label(j.name, GUILayout.Width(42));

                // 누르는 동안 연속 감소
                if (GUILayout.RepeatButton("－", GUILayout.Width(26)) && repaint)
                    robot.SetJoint(i, j.target - step);

                float v = GUILayout.HorizontalSlider(j.target, j.min, j.max);
                if (!Mathf.Approximately(v, j.target)) robot.SetJoint(i, v);

                // 누르는 동안 연속 증가
                if (GUILayout.RepeatButton("＋", GUILayout.Width(26)) && repaint)
                    robot.SetJoint(i, j.target + step);

                GUILayout.Label($"{j.target,7:F1}°", GUILayout.Width(56));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            var p = robot.TcpPosition;
            var e = robot.TcpEuler;
            GUILayout.Label($"TCP  X:{p.x:F3}  Y:{p.y:F3}  Z:{p.z:F3}  (m)");
            GUILayout.Label($"Rot  Rx:{e.x:F1}  Ry:{e.y:F1}  Rz:{e.z:F1}  (°)");

            if (ik != null)
            {
                GUILayout.Space(6);
                ik.follow = GUILayout.Toggle(ik.follow, "  IK: 타깃 따라가기");
                float d = ik.DistanceToTarget;
                GUILayout.Label(d >= 0f ? $"타깃 거리: {d * 1000f:F0} mm" : "타깃 없음");
            }

            if (teach != null) DrawTeach();

            if (scenario == null) scenario = FindFirstObjectByType<WaferScenario>();
            if (scenario != null) DrawScenario();

            GUILayout.Space(4);
            if (GUILayout.Button("Home (모두 0°)"))
            {
                if (teach != null) teach.Stop();
                if (ik != null) ik.follow = false;   // 홈 복귀 시 추적 해제
                robot.Home();
            }

            GUI.DragWindow();
        }

        void DrawTeach()
        {
            GUILayout.Space(8);
            GUILayout.Label($"── 티칭 & 재생  (웨이포인트 {teach.waypoints.Count}개) ──");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Teach (현재자세)")) teach.Teach();
            if (GUILayout.Button("마지막 삭제"))      teach.DeleteLast();
            if (GUILayout.Button("Clear"))            teach.Clear();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (!teach.IsPlaying)
            {
                if (GUILayout.Button("▶ 재생"))
                {
                    if (ik != null) ik.follow = false;
                    teach.Play();
                }
            }
            else if (GUILayout.Button("■ 정지")) teach.Stop();
            teach.loop = GUILayout.Toggle(teach.loop, " 반복", GUILayout.Width(70));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("구간시간", GUILayout.Width(70));
            teach.segmentTime = GUILayout.HorizontalSlider(teach.segmentTime, 0.2f, 4f);
            GUILayout.Label($"{teach.segmentTime:F1}s", GUILayout.Width(46));
            GUILayout.EndHorizontal();

            if (teach.IsPlaying)
                GUILayout.Label($"재생 중… 구간 {teach.CurrentIndex + 1}/{Mathf.Max(1, teach.waypoints.Count - 1)}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("저장(디스크)"))   teach.SaveToDisk();
            if (GUILayout.Button("불러오기"))        teach.LoadFromDisk();
            GUILayout.EndHorizontal();
        }

        void DrawScenario()
        {
            GUILayout.Space(8);
            GUILayout.Label("── 웨이퍼 시나리오 (Phase 4) ──");

            var map = scenario.MapSlots();
            GUILayout.BeginHorizontal();
            GUILayout.Label("FOUP 매핑:", GUILayout.Width(96));
            for (int i = map.Length - 1; i >= 0; i--)   // 위 슬롯이 왼쪽
                GUILayout.Label(map[i] ? "■" : "□", GUILayout.Width(18));
            GUILayout.EndHorizontal();

            GUILayout.Label($"상태: {scenario.Status}");

            GUILayout.BeginHorizontal();
            if (!scenario.IsRunning)
            {
                if (GUILayout.Button("▶ 사이클 실행"))
                {
                    if (teach != null) teach.Stop();
                    scenario.RunCycle();
                }
            }
            else if (GUILayout.Button("■ 정지")) scenario.StopCycle();
            GUILayout.EndHorizontal();
        }
    }
}
