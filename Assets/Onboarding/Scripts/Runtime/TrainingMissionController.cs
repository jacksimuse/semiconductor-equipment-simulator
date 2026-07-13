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
        public StationLearningProfile defaultLearningProfile;
        public TrainingCameraFollow cameraFollow;
        public Transform equipmentFocusTarget;

        public string traineeName = "신입 엔지니어";
        public bool lockPlayerAfterEntry = true;
        public bool allowStationPanelToggle = true;

        public bool IsInStationMode { get; private set; }
        public bool PracticeStarted { get; private set; }

        Rect window = new Rect(16, 16, 680, 300);
        bool showWindow = true;
        GUIStyle stationLabelStyle;
        GUIStyle stationButtonStyle;
        StationBase activeStation;
        StationDefinition activeDefinition;
        StationInteractionPoint focusedPoint;
        StationLearningProfile activeLearningProfile;
        float missionStartTime = -1f;

        void Awake()
        {
            if (player == null) player = FindAnyObjectByType<EngineerPlayerController>();
            if (cameraFollow == null && Camera.main != null)
                cameraFollow = Camera.main.GetComponent<TrainingCameraFollow>();
        }

        void Update()
        {
            if (ReadCameraCycleDown() && cameraFollow != null && !IsInStationMode)
                cameraFollow.CyclePlayerCamera();

            if (IsInStationMode && ReadReturnToPlayerDown())
            {
                ExitActiveStation();
                return;
            }

            if (!IsInStationMode && focusedPoint != null && ReadEnterStationDown())
            {
                EnterStation(focusedPoint.definition, focusedPoint.learningProfile);
                return;
            }

            if (!allowStationPanelToggle || activeStation == null) return;
            if (!ReadEquipmentToggleDown()) return;

            if (activeStation.IsActive) activeStation.Exit();
            else activeStation.Enter();
        }

        public void EnterTrainingRoom()
        {
            EnterStation(defaultStation != null ? defaultStation : FirstStation(), defaultLearningProfile);
        }

        public void FocusStation(StationInteractionPoint point)
        {
            if (point == null || point.definition == null || IsInStationMode) return;
            focusedPoint = point;
        }

        public void ClearFocusedStation(StationInteractionPoint point)
        {
            if (focusedPoint == point) focusedPoint = null;
        }

        public void EnterStation(StationDefinition definition, StationLearningProfile learningProfile = null)
        {
            if (definition == null) return;
            if (IsInStationMode && activeDefinition == definition) return;

            ExitActiveStation();

            activeDefinition = definition;
            activeLearningProfile = learningProfile;
            activeStation = CreateStation(definition);
            if (activeStation == null) return;

            IsInStationMode = true;
            PracticeStarted = false;
            missionStartTime = -1f;
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
            activeLearningProfile = null;
            missionStartTime = -1f;
            IsInStationMode = false;

            if (player != null)
            {
                player.enabled = true;
                if (cameraFollow != null)
                    cameraFollow.FollowPlayer(player.transform);
            }
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
            EnsureGuiStyles();
            if (!showWindow)
            {
                if (GUI.Button(new Rect(18f, 18f, 100f, 50f), "가이드", stationButtonStyle))
                    showWindow = true;
                return;
            }

            window = GUILayout.Window(62010, window, DrawWindow, "JSM 온보딩 스테이션");
        }

        void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{traineeName} 온보딩", stationLabelStyle);
            if (GUILayout.Button("닫기", stationButtonStyle, GUILayout.Width(86f), GUILayout.Height(43f)))
            {
                showWindow = false;
                GUI.DragWindow();
                GUILayout.EndHorizontal();
                return;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (!IsInStationMode)
            {
                GUILayout.Label("목표: 방향키/WASD로 장비 키오스크 앞까지 이동하세요.", stationLabelStyle);
                if (focusedPoint != null && focusedPoint.definition != null)
                {
                    GUILayout.Space(4);
                    GUILayout.Label($"{focusedPoint.definition.displayName} 앞에 있습니다.", stationLabelStyle);
                    GUILayout.Label("E: 장비 제어 모드 진입", stationLabelStyle);
                }
                else
                {
                    GUILayout.Label("장비 앞에 서면 진입 안내가 표시됩니다.", stationLabelStyle);
                }
            }
            else
            {
                DrawStationPanel();
            }

            GUILayout.Space(4);
            GUILayout.Label(IsInStationMode
                ? "현재 모드: 장비 포커스 | Esc/Backspace: 캐릭터 조작 복귀"
                : "조작: WASD/방향키 이동 | C: 카메라 전환", stationLabelStyle);
            GUI.DragWindow();
        }

        void EnsureGuiStyles()
        {
            if (stationLabelStyle != null) return;

            stationLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 25,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            stationButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 25,
                fontStyle = FontStyle.Bold
            };
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
            DrawLearningProfile();

            StationStatus status = activeStation.GetStatus();
            GUILayout.Label($"상태: {status.text}");
            GUILayout.Label($"진행도: {Mathf.RoundToInt(Mathf.Clamp01(status.progress) * 100f)}%");
            if (status.busy) GUILayout.Label("동작 중");
            if (status.eStop) GUILayout.Label($"E-STOP/인터락: {status.lastEvent}");
            if (status.fault) GUILayout.Label($"알람/오류: {status.lastEvent}");

            GUILayout.Space(4);
            DrawCommandButtons();
            GUILayout.Label("- Tab: 장비 제어 활성/비활성");
            GUILayout.Label("- Esc/Backspace: 캐릭터 조작 모드 복귀");
            GUILayout.Space(4);

            GUI.enabled = !status.busy && !status.eStop && !status.fault;
            if (!PracticeStarted)
            {
                string missionTitle = CurrentMission() != null ? CurrentMission().title : "첫 미션";
                if (GUILayout.Button($"{missionTitle} 시작"))
                {
                    PracticeStarted = true;
                    missionStartTime = Time.time;
                    StartDefaultMissionCommand();
                }
                GUILayout.Label("첫 미션 실행");
            }
            else
            {
                if (GUILayout.Button("미션 다시 실행"))
                {
                    missionStartTime = Time.time;
                    StartDefaultMissionCommand();
                }
                GUILayout.Label(status.progress >= 1f ? "■ 미션 완료" : "□ 미션 진행 중");
                DrawMissionResult(status);
            }
            GUI.enabled = true;
        }

        void DrawLearningProfile()
        {
            if (activeLearningProfile == null) return;

            GUILayout.Space(4);
            GUILayout.Label(activeLearningProfile.chapterTitle);
            if (!string.IsNullOrWhiteSpace(activeLearningProfile.roleInFab))
                GUILayout.Label(activeLearningProfile.roleInFab);
            if (!string.IsNullOrWhiteSpace(activeLearningProfile.lessonGoal))
                GUILayout.Label(activeLearningProfile.lessonGoal);
            if (!string.IsNullOrWhiteSpace(activeLearningProfile.safetyNote))
                GUILayout.Label($"안전: {activeLearningProfile.safetyNote}");

            var mission = CurrentMission();
            if (mission != null)
            {
                GUILayout.Space(4);
                GUILayout.Label($"미션: {mission.title}");
                if (!string.IsNullOrWhiteSpace(mission.briefing)) GUILayout.Label(mission.briefing);
                if (!string.IsNullOrWhiteSpace(mission.successCriteria)) GUILayout.Label(mission.successCriteria);
            }
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

            var mission = CurrentMission();
            if (mission != null && !string.IsNullOrWhiteSpace(mission.startCommand))
            {
                activeStation.Command(mission.startCommand);
                return;
            }

            activeStation.Command(activeDefinition.id == "aligner" ? "Align" : "StartCycle");
        }

        MissionDefinition CurrentMission()
        {
            return activeLearningProfile != null ? activeLearningProfile.FirstMission : null;
        }

        void DrawMissionResult(StationStatus status)
        {
            var mission = CurrentMission();
            if (mission == null) return;

            if (status.progress >= mission.targetProgress)
            {
                if (!string.IsNullOrWhiteSpace(mission.successFeedback))
                    GUILayout.Label(mission.successFeedback);
                return;
            }

            if (missionStartTime > 0f && mission.timeLimitSeconds > 0f)
            {
                float remain = mission.timeLimitSeconds - (Time.time - missionStartTime);
                GUILayout.Label($"남은 시간: {Mathf.Max(0f, remain):F0}s");
                if (remain <= 0f && !string.IsNullOrWhiteSpace(mission.failureFeedback))
                    GUILayout.Label(mission.failureFeedback);
            }
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

        static bool ReadEnterStationDown()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        static bool ReadReturnToPlayerDown()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null &&
                   (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
#endif
        }

        static bool ReadCameraCycleDown()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.cKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.C);
#endif
        }
    }
}
