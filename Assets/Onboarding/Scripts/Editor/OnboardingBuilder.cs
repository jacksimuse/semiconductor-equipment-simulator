using DigitalTwin.Stations;
using Onboarding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Onboarding.EditorTools
{
    public static class OnboardingBuilder
    {
        const string RootName = "OnboardingTrainingArea";
        const string ScenePath = "Assets/Scenes/Onboarding.unity";
        const string ContentRoot = "Assets/Onboarding/Content";

        [MenuItem("Tools/Onboarding/Build Training Area")]
        public static void Build()
        {
            var scene = System.IO.File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            RemoveLegacyOnboardingRoots();

            EnsureDefaultLearningContent();
            var definitions = LoadStationDefinitions();

            var root = new GameObject(RootName);
            var lobbyMat = MakeMat(new Color(0.42f, 0.46f, 0.50f));
            var roomMat = MakeMat(new Color(0.58f, 0.62f, 0.66f));
            var wallMat = MakeMat(new Color(0.82f, 0.86f, 0.88f));
            var doorMat = MakeMat(new Color(0.12f, 0.38f, 0.58f));

            CreateFacilityLayout(root.transform, lobbyMat, roomMat, wallMat, doorMat);

            var missionGo = new GameObject("TrainingMissionController");
            missionGo.transform.SetParent(root.transform, false);
            var mission = missionGo.AddComponent<TrainingMissionController>();
            mission.defaultStation = definitions.Length > 0 ? definitions[0] : null;
            mission.defaultLearningProfile = mission.defaultStation != null ? LoadLearningProfile(mission.defaultStation.id) : null;

            var player = CreatePlayer(root.transform);
            mission.player = player.GetComponent<EngineerPlayerController>();

            SetupCamera(root.transform, player.transform);
            mission.cameraFollow = Camera.main != null ? Camera.main.GetComponent<TrainingCameraFollow>() : null;

            var guideHud = CreateGuideHud(root.transform);
            CreateGuideZones(root.transform, guideHud);
            CreateStationInteractionPoints(root.transform, mission, definitions);

            Selection.activeGameObject = player;
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Onboarding] Onboarding.unity 생성/저장 완료. StationDefinition 기반 상호작용 포인트를 배치했습니다.");
        }

        static void RemoveLegacyOnboardingRoots()
        {
            RemoveRootIfExists("SixAxisRobot");
            RemoveRootIfExists("IK_Target");
        }

        static void RemoveRootIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null && go.transform.parent == null)
                Object.DestroyImmediate(go);
        }

        static StationDefinition[] LoadStationDefinitions()
        {
            var guids = AssetDatabase.FindAssets("t:StationDefinition", new[] { "Assets/DigitalTwin/Stations" });
            var defs = new StationDefinition[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                defs[i] = AssetDatabase.LoadAssetAtPath<StationDefinition>(path);
            }
            return defs;
        }

        static FacilityGuideHud CreateGuideHud(Transform parent)
        {
            var go = new GameObject("FacilityGuideHud");
            go.transform.SetParent(parent, false);
            var hud = go.AddComponent<FacilityGuideHud>();
            hud.guideName = "한서윤 매니저";
            hud.playerPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/플레이어.png");
            hud.hanSeoYoonPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/한서윤.png");
            hud.parkDoHyunPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/박도현.png");
            hud.leeJiHoonPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/이지훈.png");
            hud.choiMinAhPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/최민아.png");
            hud.kimTaeJoonPortrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/김태준.png");
            return hud;
        }

        static void CreateGuideZones(Transform parent, FacilityGuideHud guideHud)
        {
            CreateGuideZone(parent, guideHud, "lobby", "Onboarding Lobby", new Vector3(0f, 0.9f, 7.0f), new Vector3(6.0f, 1.8f, 3.4f));
            CreateGuideZone(parent, guideHud, "gowning", "Gowning Area", new Vector3(0f, 0.9f, 3.8f), new Vector3(6.0f, 1.8f, 2.8f));
            CreateGuideZone(parent, guideHud, "cleanroom", "Training Cleanroom", new Vector3(0f, 0.9f, 0.5f), new Vector3(6.0f, 1.8f, 3.4f));
            CreateGuideZone(parent, guideHud, "robotlab", "Robot Transfer Lab", new Vector3(-4.6f, 0.9f, -2.9f), new Vector3(4.0f, 1.8f, 3.2f));
            CreateGuideZone(parent, guideHud, "control", "Control Room", new Vector3(4.6f, 0.9f, -2.9f), new Vector3(4.0f, 1.8f, 3.2f));
            CreateGuideZone(parent, guideHud, "maintenance", "Maintenance Bay", new Vector3(-4.6f, 0.9f, -6.4f), new Vector3(4.0f, 1.8f, 3.2f));
            CreateGuideZone(parent, guideHud, "twinops", "Twin Operations Room", new Vector3(4.6f, 0.9f, -6.4f), new Vector3(4.0f, 1.8f, 3.2f));
            CreateGuideZone(parent, guideHud, "demo", "Customer Demo Hall", new Vector3(0f, 0.9f, -10.0f), new Vector3(7.2f, 1.8f, 3.4f));
        }

        static void CreateGuideZone(Transform parent, FacilityGuideHud guideHud, string zoneId, string displayName, Vector3 center, Vector3 size)
        {
            var go = new GameObject($"{zoneId}_GuideZone");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var zone = go.AddComponent<FacilityGuideZone>();
            zone.zoneId = zoneId;
            zone.displayName = displayName;
            zone.guideHud = guideHud;
        }

        static void CreateFacilityLayout(Transform parent, Material lobbyMat, Material roomMat, Material wallMat, Material doorMat)
        {
            var gownMat = MakeMat(new Color(0.48f, 0.56f, 0.62f));
            var labMat = MakeMat(new Color(0.50f, 0.58f, 0.64f));
            var controlMat = MakeMat(new Color(0.42f, 0.50f, 0.58f));
            var maintenanceMat = MakeMat(new Color(0.55f, 0.52f, 0.47f));
            var twinMat = MakeMat(new Color(0.38f, 0.48f, 0.60f));
            var demoMat = MakeMat(new Color(0.52f, 0.50f, 0.57f));
            var pathMat = MakeMat(new Color(0.18f, 0.42f, 0.58f));
            var safetyMat = MakeMat(new Color(1.00f, 0.82f, 0.12f));
            var zoneMat = MakeMat(new Color(0.05f, 0.72f, 0.82f));
            var equipmentMat = MakeMat(new Color(0.22f, 0.28f, 0.32f));
            var screenMat = MakeMat(new Color(0.05f, 0.18f, 0.26f));
            var glassMat = MakeMat(new Color(0.62f, 0.78f, 0.86f));

            CreateCube("FacilitySafetyFloor", parent, new Vector3(0f, -0.075f, -1.5f), new Vector3(14.2f, 0.08f, 21.8f), roomMat);
            CreateCube("OnboardingLobby_Floor", parent, new Vector3(0f, -0.03f, 7.0f), new Vector3(6.0f, 0.06f, 3.4f), lobbyMat);
            CreateCube("GowningArea_Floor", parent, new Vector3(0f, -0.03f, 3.8f), new Vector3(6.0f, 0.06f, 2.8f), gownMat);
            CreateCube("TrainingCleanroom_Floor", parent, new Vector3(0f, -0.03f, 0.5f), new Vector3(6.0f, 0.06f, 3.4f), roomMat);
            CreateCube("RobotTransferLab_Floor", parent, new Vector3(-4.6f, -0.03f, -2.9f), new Vector3(4.0f, 0.06f, 3.2f), labMat);
            CreateCube("ControlRoom_Floor", parent, new Vector3(4.6f, -0.03f, -2.9f), new Vector3(4.0f, 0.06f, 3.2f), controlMat);
            CreateCube("MaintenanceBay_Floor", parent, new Vector3(-4.6f, -0.03f, -6.4f), new Vector3(4.0f, 0.06f, 3.2f), maintenanceMat);
            CreateCube("TwinOperationsRoom_Floor", parent, new Vector3(4.6f, -0.03f, -6.4f), new Vector3(4.0f, 0.06f, 3.2f), twinMat);
            CreateCube("CustomerDemoHall_Floor", parent, new Vector3(0f, -0.03f, -10.0f), new Vector3(7.2f, 0.06f, 3.4f), demoMat);

            CreateMarkerCube("MainPath_LobbyToCleanroom", parent, new Vector3(0f, 0.005f, 3.8f), new Vector3(1.15f, 0.025f, 8.8f), pathMat);
            CreateMarkerCube("MainPath_RobotToControl", parent, new Vector3(0f, 0.006f, -2.9f), new Vector3(9.6f, 0.025f, 0.9f), pathMat);
            CreateMarkerCube("MainPath_MaintenanceToTwin", parent, new Vector3(0f, 0.007f, -6.4f), new Vector3(9.6f, 0.025f, 0.9f), pathMat);
            CreateMarkerCube("MainPath_Demo", parent, new Vector3(0f, 0.008f, -8.2f), new Vector3(1.15f, 0.025f, 3.2f), pathMat);
            CreateZoneOutlines(parent, zoneMat);

            CreateBoundary(parent, wallMat);
            CreateLowDivider(parent, doorMat, new Vector3(0f, 0.3f, 5.35f), new Vector3(5.8f, 0.6f, 0.08f), 1.4f);
            CreateLowDivider(parent, doorMat, new Vector3(0f, 0.3f, 2.15f), new Vector3(5.8f, 0.6f, 0.08f), 1.4f);
            CreateLowDivider(parent, wallMat, new Vector3(0f, 0.3f, -1.25f), new Vector3(5.8f, 0.6f, 0.08f), 1.4f);
            CreateLowDivider(parent, wallMat, new Vector3(0f, 0.3f, -4.75f), new Vector3(9.4f, 0.6f, 0.08f), 1.4f);
            CreateLowDivider(parent, wallMat, new Vector3(0f, 0.3f, -8.05f), new Vector3(7.0f, 0.6f, 0.08f), 1.4f);

            CreateLabel("JSM Semiconductor Equipment", parent, new Vector3(0f, 1.7f, 8.35f), 0.055f, Color.white);
            CreateAreaLabel("Onboarding Lobby", parent, new Vector3(-1.75f, 0.04f, 7.65f));
            CreateAreaLabel("Gowning Area", parent, new Vector3(-1.7f, 0.04f, 4.15f));
            CreateAreaLabel("Training Cleanroom", parent, new Vector3(-1.95f, 0.04f, 0.92f));
            CreateAreaLabel("Robot Transfer Lab", parent, new Vector3(-5.2f, 0.04f, -1.8f));
            CreateAreaLabel("Control Room", parent, new Vector3(4.1f, 0.04f, -1.8f));
            CreateAreaLabel("Maintenance Bay", parent, new Vector3(-5.1f, 0.04f, -5.35f));
            CreateAreaLabel("Twin Operations", parent, new Vector3(4.15f, 0.04f, -5.35f));
            CreateAreaLabel("Customer Demo Hall", parent, new Vector3(-1.1f, 0.04f, -9.2f));
            CreateZoneGuides(parent, screenMat, safetyMat);

            CreateLobbyProps(parent, equipmentMat, screenMat, safetyMat);
            CreateGowningProps(parent, equipmentMat, glassMat);
            CreateCleanroomProps(parent, equipmentMat, glassMat, safetyMat);
            CreateRobotLabProps(parent, equipmentMat, screenMat, safetyMat);
            CreateControlRoomProps(parent, equipmentMat, screenMat);
            CreateMaintenanceProps(parent, equipmentMat, safetyMat);
            CreateTwinOpsProps(parent, equipmentMat, screenMat);
            CreateDemoHallProps(parent, equipmentMat, screenMat);
        }

        static void CreateZoneOutlines(Transform parent, Material zoneMat)
        {
            CreateZoneOutline(parent, "Zone01_Lobby", new Vector3(0f, 0f, 7.0f), new Vector2(6.1f, 3.5f), zoneMat);
            CreateZoneOutline(parent, "Zone02_Gowning", new Vector3(0f, 0f, 3.8f), new Vector2(6.1f, 2.9f), zoneMat);
            CreateZoneOutline(parent, "Zone03_Cleanroom", new Vector3(0f, 0f, 0.5f), new Vector2(6.1f, 3.5f), zoneMat);
            CreateZoneOutline(parent, "Zone04_RobotLab", new Vector3(-4.6f, 0f, -2.9f), new Vector2(4.1f, 3.3f), zoneMat);
            CreateZoneOutline(parent, "Zone05_ControlRoom", new Vector3(4.6f, 0f, -2.9f), new Vector2(4.1f, 3.3f), zoneMat);
            CreateZoneOutline(parent, "Zone06_Maintenance", new Vector3(-4.6f, 0f, -6.4f), new Vector2(4.1f, 3.3f), zoneMat);
            CreateZoneOutline(parent, "Zone07_TwinOps", new Vector3(4.6f, 0f, -6.4f), new Vector2(4.1f, 3.3f), zoneMat);
            CreateZoneOutline(parent, "Zone08_DemoHall", new Vector3(0f, 0f, -10.0f), new Vector2(7.3f, 3.5f), zoneMat);
        }

        static void CreateZoneOutline(Transform parent, string name, Vector3 center, Vector2 size, Material mat)
        {
            float y = 0.035f;
            float thickness = 0.08f;
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;

            CreateMarkerCube($"{name}_North", parent, new Vector3(center.x, y, center.z + halfZ), new Vector3(size.x, 0.025f, thickness), mat);
            CreateMarkerCube($"{name}_South", parent, new Vector3(center.x, y, center.z - halfZ), new Vector3(size.x, 0.025f, thickness), mat);
            CreateMarkerCube($"{name}_West", parent, new Vector3(center.x - halfX, y, center.z), new Vector3(thickness, 0.025f, size.y), mat);
            CreateMarkerCube($"{name}_East", parent, new Vector3(center.x + halfX, y, center.z), new Vector3(thickness, 0.025f, size.y), mat);
        }

        static void CreateZoneGuides(Transform parent, Material screenMat, Material safetyMat)
        {
            CreateZoneGuide(parent, screenMat, "1 Lobby\nCheck ID + Mission", new Vector3(-2.75f, 1.05f, 7.85f));
            CreateZoneGuide(parent, screenMat, "2 Gowning\nEntry Check", new Vector3(-2.65f, 1.05f, 4.55f));
            CreateZoneGuide(parent, screenMat, "3 Cleanroom\nFollow Blue Path", new Vector3(-2.65f, 1.05f, 1.25f));
            CreateZoneGuide(parent, screenMat, "4 Robot Lab\nTransfer Practice", new Vector3(-6.25f, 1.05f, -1.85f));
            CreateZoneGuide(parent, screenMat, "5 Control\nRead Alarms", new Vector3(3.25f, 1.05f, -1.85f));
            CreateZoneGuide(parent, safetyMat, "6 Maintenance\nReset E-STOP", new Vector3(-6.15f, 1.05f, -5.35f), Color.black);
            CreateZoneGuide(parent, screenMat, "7 Twin Ops\nSync Status", new Vector3(3.25f, 1.05f, -5.35f));
            CreateZoneGuide(parent, screenMat, "8 Demo Hall\nFinal Demo", new Vector3(-3.15f, 1.05f, -9.2f));
        }

        static void CreateZoneGuide(Transform parent, Material panelMat, string text, Vector3 pos)
        {
            CreateZoneGuide(parent, panelMat, text, pos, Color.white);
        }

        static void CreateZoneGuide(Transform parent, Material panelMat, string text, Vector3 pos, Color textColor)
        {
            CreateCube($"{text.Split('\n')[0]}_GuidePanel", parent, pos, new Vector3(1.6f, 0.85f, 0.08f), panelMat);
            CreateLabel(text, parent, pos + new Vector3(0f, 0.02f, -0.08f), 0.025f, textColor);
        }

        static void CreateBoundary(Transform parent, Material wallMat)
        {
            CreateCube("OuterWall_Left", parent, new Vector3(-7.0f, 0.55f, -1.5f), new Vector3(0.12f, 1.1f, 21.4f), wallMat);
            CreateCube("OuterWall_Right", parent, new Vector3(7.0f, 0.55f, -1.5f), new Vector3(0.12f, 1.1f, 21.4f), wallMat);
            CreateCube("OuterWall_Front", parent, new Vector3(0f, 0.55f, 8.75f), new Vector3(14.0f, 1.1f, 0.12f), wallMat);
            CreateCube("OuterWall_Back", parent, new Vector3(0f, 0.55f, -11.75f), new Vector3(14.0f, 1.1f, 0.12f), wallMat);
        }

        static void CreateLowDivider(Transform parent, Material mat, Vector3 pos, Vector3 totalScale, float openingWidth)
        {
            float sideWidth = (totalScale.x - openingWidth) * 0.5f;
            if (sideWidth <= 0f) return;

            float leftX = pos.x - openingWidth * 0.5f - sideWidth * 0.5f;
            float rightX = pos.x + openingWidth * 0.5f + sideWidth * 0.5f;
            CreateCube($"{pos.z:F1}_DividerLeft", parent, new Vector3(leftX, pos.y, pos.z), new Vector3(sideWidth, totalScale.y, totalScale.z), mat);
            CreateCube($"{pos.z:F1}_DividerRight", parent, new Vector3(rightX, pos.y, pos.z), new Vector3(sideWidth, totalScale.y, totalScale.z), mat);
        }

        static void CreateLobbyProps(Transform parent, Material equipmentMat, Material screenMat, Material safetyMat)
        {
            CreateCube("ReceptionDesk", parent, new Vector3(-2.0f, 0.38f, 7.15f), new Vector3(1.7f, 0.75f, 0.45f), equipmentMat);
            CreateCube("VisitorBadgeKiosk", parent, new Vector3(1.9f, 0.55f, 7.2f), new Vector3(0.45f, 1.1f, 0.32f), screenMat);
            CreateCube("TodayMissionBoard", parent, new Vector3(0f, 0.85f, 8.35f), new Vector3(2.2f, 1.2f, 0.08f), screenMat);
            CreateMarkerCube("SafetyLine_Lobby", parent, new Vector3(0f, 0.03f, 5.55f), new Vector3(5.6f, 0.03f, 0.12f), safetyMat);
            CreateLabel("ID / Mission", parent, new Vector3(1.9f, 1.25f, 7.2f), 0.028f, Color.white);
        }

        static void CreateGowningProps(Transform parent, Material equipmentMat, Material glassMat)
        {
            for (int i = 0; i < 4; i++)
                CreateCube($"GowningLocker_{i + 1}", parent, new Vector3(-2.5f + i * 0.55f, 0.75f, 4.75f), new Vector3(0.42f, 1.5f, 0.35f), equipmentMat);

            CreateCube("GowningBench", parent, new Vector3(1.45f, 0.22f, 4.1f), new Vector3(1.8f, 0.28f, 0.45f), equipmentMat);
            CreateCube("AirShowerGate", parent, new Vector3(0f, 0.9f, 2.38f), new Vector3(1.55f, 1.8f, 0.16f), glassMat);
            CreateLabel("Cleanroom Entry Check", parent, new Vector3(0f, 1.45f, 2.34f), 0.03f, Color.white);
        }

        static void CreateCleanroomProps(Transform parent, Material equipmentMat, Material glassMat, Material safetyMat)
        {
            CreateCube("CleanroomObservationWindow", parent, new Vector3(-2.85f, 0.85f, 0.7f), new Vector3(0.1f, 1.1f, 1.7f), glassMat);
            CreateCube("CleanroomStatusTower", parent, new Vector3(2.25f, 0.85f, 0.75f), new Vector3(0.22f, 1.7f, 0.22f), equipmentMat);
            CreateMarkerCube("CleanroomSafetyZoneMark", parent, new Vector3(0f, 0.025f, -0.45f), new Vector3(4.8f, 0.02f, 0.08f), safetyMat);
            CreateLabel("Cleanroom Rule: Walk / Check Status / No Direct Reset", parent, new Vector3(0f, 1.4f, 1.75f), 0.028f, Color.white);
        }

        static void CreateRobotLabProps(Transform parent, Material equipmentMat, Material screenMat, Material safetyMat)
        {
            CreateCube("FOUP_Mock", parent, new Vector3(-6.0f, 0.65f, -3.25f), new Vector3(0.7f, 1.3f, 0.55f), equipmentMat);
            CreateCube("LoadPort_Mock", parent, new Vector3(-5.25f, 0.45f, -3.25f), new Vector3(0.58f, 0.9f, 0.45f), screenMat);
            CreateCube("TransferRobot_MockBase", parent, new Vector3(-4.45f, 0.28f, -3.25f), new Vector3(0.55f, 0.55f, 0.55f), equipmentMat);
            CreateCube("TransferRobot_MockArm", parent, new Vector3(-4.05f, 0.75f, -3.25f), new Vector3(0.9f, 0.15f, 0.18f), equipmentMat);
            CreateCube("ProcessChamber_Mock", parent, new Vector3(-3.05f, 0.65f, -3.25f), new Vector3(0.8f, 1.3f, 0.65f), equipmentMat);
            CreateMarkerCube("RobotLabSafetyBox", parent, new Vector3(-4.55f, 0.025f, -3.25f), new Vector3(3.3f, 0.02f, 1.8f), safetyMat);
            CreateLabel("FOUP -> Robot -> Chamber", parent, new Vector3(-4.55f, 1.45f, -3.25f), 0.03f, Color.white);
        }

        static void CreateControlRoomProps(Transform parent, Material equipmentMat, Material screenMat)
        {
            for (int i = 0; i < 3; i++)
            {
                CreateCube($"ControlConsole_{i + 1}", parent, new Vector3(3.4f + i * 0.85f, 0.35f, -3.35f), new Vector3(0.68f, 0.7f, 0.45f), equipmentMat);
                CreateCube($"ControlMonitor_{i + 1}", parent, new Vector3(3.4f + i * 0.85f, 0.95f, -3.58f), new Vector3(0.55f, 0.42f, 0.08f), screenMat);
            }
            CreateCube("AlarmLogWall", parent, new Vector3(4.6f, 1.05f, -1.45f), new Vector3(2.2f, 1.2f, 0.08f), screenMat);
            CreateLabel("Alarm / HMI / Event Log", parent, new Vector3(4.6f, 1.75f, -1.42f), 0.03f, Color.white);
        }

        static void CreateMaintenanceProps(Transform parent, Material equipmentMat, Material safetyMat)
        {
            CreateCube("MaintenanceWorkbench", parent, new Vector3(-5.6f, 0.42f, -6.65f), new Vector3(1.7f, 0.55f, 0.65f), equipmentMat);
            CreateCube("ToolCart", parent, new Vector3(-3.8f, 0.45f, -6.7f), new Vector3(0.55f, 0.9f, 0.45f), equipmentMat);
            CreateCube("EStopTrainingPanel", parent, new Vector3(-4.6f, 0.85f, -5.25f), new Vector3(1.0f, 1.3f, 0.1f), safetyMat);
            CreateLabel("E-STOP / Interlock Recovery", parent, new Vector3(-4.6f, 1.55f, -5.22f), 0.028f, Color.black);
        }

        static void CreateTwinOpsProps(Transform parent, Material equipmentMat, Material screenMat)
        {
            CreateCube("TwinServerRack_A", parent, new Vector3(3.25f, 0.85f, -6.8f), new Vector3(0.55f, 1.7f, 0.65f), equipmentMat);
            CreateCube("TwinServerRack_B", parent, new Vector3(3.95f, 0.85f, -6.8f), new Vector3(0.55f, 1.7f, 0.65f), equipmentMat);
            CreateCube("TwinStatusWall", parent, new Vector3(5.15f, 1.05f, -5.25f), new Vector3(1.9f, 1.2f, 0.08f), screenMat);
            CreateCube("NetworkDesk", parent, new Vector3(5.1f, 0.35f, -6.9f), new Vector3(1.5f, 0.55f, 0.55f), equipmentMat);
            CreateLabel("Simulator / Real Equipment Sync", parent, new Vector3(5.15f, 1.75f, -5.22f), 0.027f, Color.white);
        }

        static void CreateDemoHallProps(Transform parent, Material equipmentMat, Material screenMat)
        {
            CreateCube("CustomerDemoScreen", parent, new Vector3(0f, 1.05f, -11.2f), new Vector3(3.3f, 1.3f, 0.08f), screenMat);
            CreateCube("DemoPodium", parent, new Vector3(-2.2f, 0.42f, -9.7f), new Vector3(0.9f, 0.85f, 0.55f), equipmentMat);
            CreateCube("CustomerTable", parent, new Vector3(1.65f, 0.32f, -9.5f), new Vector3(2.1f, 0.35f, 0.75f), equipmentMat);
            CreateLabel("Final Customer Demo", parent, new Vector3(0f, 1.8f, -11.15f), 0.035f, Color.white);
        }

        static void CreateStationInteractionPoints(Transform parent, TrainingMissionController mission, StationDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0)
            {
                CreateLabel("StationDefinition 없음", parent, new Vector3(0f, 0.1f, -1.3f), 0.04f, Color.yellow);
                return;
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                var def = definitions[i];
                Vector3 pos = new Vector3(-5.75f + i * 1.15f, 0.45f, -2.05f);
                var kiosk = CreateCube($"{def.id}_StationKiosk", parent, pos, new Vector3(0.8f, 0.9f, 0.8f), MakeMat(new Color(0.22f, 0.32f, 0.40f)));
                var kioskCollider = kiosk.GetComponent<BoxCollider>();
                kioskCollider.isTrigger = false;

                var trigger = new GameObject($"{def.id}_StationTrigger");
                trigger.transform.SetParent(kiosk.transform, false);
                trigger.transform.localPosition = Vector3.zero;
                var triggerCollider = trigger.AddComponent<BoxCollider>();
                triggerCollider.isTrigger = true;
                triggerCollider.size = new Vector3(2.1f, 1.4f, 2.1f);

                var interaction = trigger.AddComponent<StationInteractionPoint>();
                interaction.definition = def;
                interaction.learningProfile = LoadLearningProfile(def.id);
                interaction.mission = mission;

                string label = string.IsNullOrEmpty(def.displayName) ? def.id : def.displayName;
                CreateLabel(label, parent, pos + new Vector3(0f, 0.62f, 0f), 0.035f, Color.white);
            }
        }

        static GameObject CreatePlayer(Transform parent)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "TraineeEngineer";
            player.transform.SetParent(parent, true);
            player.transform.position = new Vector3(0f, 0.95f, 7.35f);
            player.transform.localScale = new Vector3(0.42f, 0.95f, 0.42f);
            player.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.10f, 0.26f, 0.42f));

            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.85f;
            controller.radius = 0.32f;
            controller.center = Vector3.zero;

            player.AddComponent<EngineerPlayerController>();
            var avatar = player.AddComponent<PlayerImageAvatar>();
            avatar.portraitTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/플레이어.png");
            CreateHardHat(player.transform);
            return player;
        }

        static StationLearningProfile LoadLearningProfile(string stationId)
        {
            var guids = AssetDatabase.FindAssets("t:StationLearningProfile", new[] { ContentRoot });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<StationLearningProfile>(path);
                if (profile != null && profile.stationId == stationId)
                    return profile;
            }
            return null;
        }

        static void EnsureDefaultLearningContent()
        {
            EnsureFolder("Assets/Onboarding");
            EnsureFolder(ContentRoot);

            string missionPath = $"{ContentRoot}/mission_aligner_notch.asset";
            var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(missionPath);
            if (mission == null)
            {
                mission = ScriptableObject.CreateInstance<MissionDefinition>();
                mission.id = "aligner_notch_intro";
                mission.stationId = "aligner";
                mission.title = "웨이퍼 노치 정렬";
                mission.briefing = "얼라이너는 웨이퍼의 노치 방향을 기준 위치로 맞춰 다음 공정 장비가 정확한 방향으로 웨이퍼를 받을 수 있게 합니다.";
                mission.successCriteria = "Align 명령을 실행하고 진행도가 100%에 도달하면 성공입니다.";
                mission.successFeedback = "정렬 완료. 웨이퍼 방향 기준이 맞춰졌습니다.";
                mission.failureFeedback = "정렬 시간이 초과되었습니다. 장비 상태와 Stop 여부를 확인하세요.";
                mission.startCommand = "Align";
                mission.targetProgress = 1f;
                mission.timeLimitSeconds = 30f;
                AssetDatabase.CreateAsset(mission, missionPath);
            }

            string profilePath = $"{ContentRoot}/profile_aligner.asset";
            var profile = AssetDatabase.LoadAssetAtPath<StationLearningProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<StationLearningProfile>();
                profile.stationId = "aligner";
                profile.chapterTitle = "Chapter 1. 웨이퍼 얼라이너 기초";
                profile.roleInFab = "역할: 웨이퍼의 노치 방향을 읽고 기준 방향으로 정렬하는 장비입니다.";
                profile.lessonGoal = "학습 목표: Align 명령으로 정렬을 시작하고, 상태와 진행도를 관찰합니다.";
                profile.safetyNote = "동작 중에는 Stop 외의 반복 명령을 남발하지 말고 상태가 완료될 때까지 기다립니다.";
                profile.missions = new[] { mission };
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            else if (profile.missions == null || profile.missions.Length == 0)
            {
                profile.missions = new[] { mission };
                EditorUtility.SetDirty(profile);
            }

            AssetDatabase.SaveAssets();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static void CreateHardHat(Transform parent)
        {
            var hat = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hat.name = "SafetyHelmet";
            Object.DestroyImmediate(hat.GetComponent<Collider>());
            hat.transform.SetParent(parent, false);
            hat.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            hat.transform.localScale = new Vector3(0.72f, 0.22f, 0.72f);
            hat.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(1.0f, 0.82f, 0.12f));
        }

        static void SetupCamera(Transform parent, Transform target)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            cam.transform.SetParent(parent, true);
            var follow = cam.GetComponent<TrainingCameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<TrainingCameraFollow>();
            follow.FollowPlayer(target);
            cam.transform.position = target.position + follow.playerOffset;
            cam.transform.LookAt(target.position + Vector3.up * follow.playerLookHeight);
        }

        static GameObject CreateCube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject CreateMarkerCube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = CreateCube(name, parent, pos, scale, mat);
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            return go;
        }

        static void CreateLabel(string text, Transform parent, Vector3 pos, float size, Color color)
        {
            var go = new GameObject(text);
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 32;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            go.AddComponent<FacilityLabelBillboard>();
        }

        static void CreateAreaLabel(string text, Transform parent, Vector3 pos)
        {
            CreateLabel(text, parent, pos, 0.04f, Color.white);
        }

        static Material MakeMat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f);
            return mat;
        }
    }
}
