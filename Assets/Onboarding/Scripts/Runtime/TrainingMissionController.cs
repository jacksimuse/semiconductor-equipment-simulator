using DigitalTwin.Stations;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Onboarding
{
    public class TrainingMissionController : MonoBehaviour
    {
        public EngineerPlayerController player;
        public StationRegistry stationRegistry;
        public StationDefinition defaultStation;
        public TrainingCameraFollow cameraFollow;
        public Transform equipmentFocusTarget;

        public string traineeName = "신입 엔지니어";
        public bool lockPlayerAfterEntry = true;
        public bool allowStationPanelToggle = true;

        public bool IsInStationMode { get; private set; }
        public bool PracticeStarted { get; private set; }

        Rect window = new Rect(18, 18, 420, 0);
        StationBase activeStation;
        StationDefinition activeDefinition;

        void Awake()
        {
            if (player == null) player = FindFirstObjectByType<EngineerPlayerController>();
            if (cameraFollow == null && Camera.main != null)
                cameraFollow = Camera.main.GetComponent<TrainingCameraFollow>();
        }

        void Update()
        {
            if (!allowStationPanelToggle || activeStation == null) return;
            if (!ReadEquipmentToggleDown()) return;

            if (activeStation.IsActive) activeStation.Exit();
            else activeStation.Enter();
        }

        public void EnterTrainingRoom()
        {
            EnterStation(defaultStation != null ? defaultStation : FirstStation());
        }

        public void EnterStation(StationDefinition definition)
        {
            if (definition == null) return;
            if (IsInStationMode && activeDefinition == definition) return;

            ExitActiveStation();

            activeDefinition = definition;
            activeStation = CreateStation(definition);
            if (activeStation == null) return;

            IsInStationMode = true;
            PracticeStarted = false;
            activeStation.Enter();
            FocusEquipmentMode(activeStation.transform);
        }

        void ExitActiveStation()
        {
            if (activeStation != null)
            {
                activeStation.Exit();
                Destroy(activeStation.gameObject);
            }

            activeStation = null;
            activeDefinition = null;
            IsInStationMode = false;
        }

        StationBase CreateStation(StationDefinition definition)
        {
            if (definition.controlPrefab == null) return null;

            var go = Instantiate(definition.controlPrefab);
            go.name = $"{definition.id}_StationControl";
            var station = go.GetComponentInChildren<StationBase>(true);
            if (station != null && station.definition == null)
                station.definition = definition;
            return station;
        }

        StationDefinition FirstStation()
        {
            if (stationRegistry == null || stationRegistry.stations == null || stationRegistry.stations.Length == 0)
                return null;
            return stationRegistry.stations[0];
        }

        void FocusEquipmentMode(Transform target)
        {
            if (target != null) equipmentFocusTarget = target;
            if (cameraFollow != null && equipmentFocusTarget != null)
                cameraFollow.FocusEquipment(equipmentFocusTarget);
            if (lockPlayerAfterEntry && player != null)
                player.enabled = false;
        }

        void OnGUI()
        {
            window = GUILayout.Window(62010, window, DrawWindow, "JSM 온보딩 스테이션");
        }

        void DrawWindow(int id)
        {
            GUILayout.Label($"{traineeName} 온보딩");
            GUILayout.Space(4);

            if (!IsInStationMode)
            {
                GUILayout.Label("목표: 방향키/WASD로 이동해 장비 구역으로 들어가세요.");
                GUILayout.Label("장비 구역에 들어가면 Station 계약 기반 제어 모드로 전환됩니다.");
            }
            else
            {
                DrawStationPanel();
            }

            GUILayout.Space(4);
            GUILayout.Label(IsInStationMode
                ? "현재 모드: 장비 포커스"
                : "조작: WASD/방향키 이동");
            GUI.DragWindow();
        }

        void DrawStationPanel()
        {
            if (activeDefinition == null || activeStation == null)
            {
                GUILayout.Label("활성 스테이션이 없습니다.");
                return;
            }

            GUILayout.Label($"{activeDefinition.displayName} 제어 모드");
            if (!string.IsNullOrWhiteSpace(activeDefinition.description))
                GUILayout.Label(activeDefinition.description);

            StationStatus status = activeStation.GetStatus();
            GUILayout.Label($"상태: {status.text}");
            GUILayout.Label($"진행도: {Mathf.RoundToInt(Mathf.Clamp01(status.progress) * 100f)}%");
            if (status.busy) GUILayout.Label("동작 중");
            if (status.eStop) GUILayout.Label($"E-STOP/인터락: {status.lastEvent}");
            if (status.fault) GUILayout.Label($"알람/오류: {status.lastEvent}");

            GUILayout.Space(4);
            DrawCommandButtons();
            GUILayout.Label("- Tab: 장비 제어 활성/비활성");
            GUILayout.Space(4);

            GUI.enabled = !status.busy && !status.eStop && !status.fault;
            if (!PracticeStarted)
            {
                if (GUILayout.Button("첫 미션 시작"))
                {
                    PracticeStarted = true;
                    StartDefaultMissionCommand();
                }
                GUILayout.Label("□ 첫 미션 실행");
            }
            else
            {
                if (GUILayout.Button("미션 다시 실행"))
                    StartDefaultMissionCommand();
                GUILayout.Label(status.progress >= 1f ? "■ 미션 완료" : "□ 미션 진행 중");
            }
            GUI.enabled = true;
        }

        void DrawCommandButtons()
        {
            GUILayout.Label("장비 조작법");
            if (activeDefinition.id == "aligner")
            {
                GUILayout.Label("- Align: 웨이퍼 노치 방향 정렬");
                GUILayout.Label("- Stop: 정렬 동작 정지");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Align")) activeStation.Command("Align");
                if (GUILayout.Button("Stop")) activeStation.Command("Stop");
                GUILayout.EndHorizontal();
                return;
            }

            GUILayout.Label("- StartCycle: 웨이퍼 이송 사이클 시작");
            GUILayout.Label("- StopCycle: 사이클 정지");
            GUILayout.Label("- ResetEStop: 안전 정지 해제");
            GUILayout.Label("- Home: 로봇 기준 자세 복귀");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("StartCycle")) activeStation.Command("StartCycle");
            if (GUILayout.Button("StopCycle")) activeStation.Command("StopCycle");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ResetEStop")) activeStation.Command("ResetEStop");
            if (GUILayout.Button("Home")) activeStation.Command("Home");
            GUILayout.EndHorizontal();
        }

        void StartDefaultMissionCommand()
        {
            if (activeStation == null || activeDefinition == null) return;

            if (activeDefinition.id == "aligner")
                activeStation.Command("Align");
            else
                activeStation.Command("StartCycle");
        }

        static bool ReadEquipmentToggleDown()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.tabKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Tab);
#endif
        }
    }
}
