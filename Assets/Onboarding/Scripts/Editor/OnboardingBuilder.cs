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

        [MenuItem("Tools/Onboarding/Build Training Area")]
        public static void Build()
        {
            var scene = System.IO.File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            var definitions = LoadStationDefinitions();

            var root = new GameObject(RootName);
            var lobbyMat = MakeMat(new Color(0.42f, 0.46f, 0.50f));
            var roomMat = MakeMat(new Color(0.58f, 0.62f, 0.66f));
            var wallMat = MakeMat(new Color(0.82f, 0.86f, 0.88f));
            var doorMat = MakeMat(new Color(0.12f, 0.38f, 0.58f));

            CreateCube("LobbyFloor", root.transform, new Vector3(0f, -0.03f, 2.3f), new Vector3(5.2f, 0.06f, 3.2f), lobbyMat);
            CreateCube("TrainingRoomFloor", root.transform, new Vector3(0f, -0.03f, -1.5f), new Vector3(5.2f, 0.06f, 4.4f), roomMat);
            CreateCube("LeftWall", root.transform, new Vector3(-2.65f, 1f, -0.2f), new Vector3(0.12f, 2f, 6.8f), wallMat);
            CreateCube("RightWall", root.transform, new Vector3(2.65f, 1f, -0.2f), new Vector3(0.12f, 2f, 6.8f), wallMat);
            CreateCube("BackWall", root.transform, new Vector3(0f, 1f, -3.75f), new Vector3(5.2f, 2f, 0.12f), wallMat);
            CreateCube("EntranceLeftPost", root.transform, new Vector3(-1.0f, 1f, 0.45f), new Vector3(0.18f, 2f, 0.18f), doorMat);
            CreateCube("EntranceRightPost", root.transform, new Vector3(1.0f, 1f, 0.45f), new Vector3(0.18f, 2f, 0.18f), doorMat);
            CreateCube("EntranceHeader", root.transform, new Vector3(0f, 1.88f, 0.45f), new Vector3(2.18f, 0.22f, 0.18f), doorMat);

            var missionGo = new GameObject("TrainingMissionController");
            missionGo.transform.SetParent(root.transform, false);
            var mission = missionGo.AddComponent<TrainingMissionController>();
            mission.defaultStation = definitions.Length > 0 ? definitions[0] : null;

            var player = CreatePlayer(root.transform);
            mission.player = player.GetComponent<EngineerPlayerController>();

            SetupCamera(root.transform, player.transform);
            mission.cameraFollow = Camera.main != null ? Camera.main.GetComponent<TrainingCameraFollow>() : null;

            CreateStationInteractionPoints(root.transform, mission, definitions);
            CreateLabel("JSM Semiconductor Equipment", root.transform, new Vector3(0f, 1.92f, 0.32f), 0.045f, Color.white);
            CreateLabel("Onboarding Lobby", root.transform, new Vector3(-1.55f, 0.04f, 2.95f), 0.04f, Color.white);
            CreateLabel("Training Cleanroom", root.transform, new Vector3(-1.35f, 0.04f, -2.95f), 0.04f, Color.white);

            Selection.activeGameObject = player;
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Onboarding] Onboarding.unity 생성/저장 완료. StationDefinition 기반 상호작용 포인트를 배치했습니다.");
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
                Vector3 pos = new Vector3(-1.4f + i * 1.4f, 0.45f, -1.45f);
                var kiosk = CreateCube($"{def.id}_StationKiosk", parent, pos, new Vector3(0.8f, 0.9f, 0.8f), MakeMat(new Color(0.22f, 0.32f, 0.40f)));
                var interaction = kiosk.AddComponent<StationInteractionPoint>();
                interaction.definition = def;
                interaction.mission = mission;

                var collider = kiosk.GetComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(1.4f, 1.2f, 1.4f);

                string label = string.IsNullOrEmpty(def.displayName) ? def.id : def.displayName;
                CreateLabel(label, parent, pos + new Vector3(0f, 0.62f, 0f), 0.035f, Color.white);
            }
        }

        static GameObject CreatePlayer(Transform parent)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "TraineeEngineer";
            player.transform.SetParent(parent, true);
            player.transform.position = new Vector3(0f, 0.95f, 2.65f);
            player.transform.localScale = new Vector3(0.42f, 0.95f, 0.42f);
            player.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.10f, 0.26f, 0.42f));

            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.85f;
            controller.radius = 0.32f;
            controller.center = Vector3.zero;

            player.AddComponent<EngineerPlayerController>();
            CreateHardHat(player.transform);
            return player;
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

        static void CreateLabel(string text, Transform parent, Vector3 pos, float size, Color color)
        {
            var go = new GameObject(text);
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(80f, 0f, 0f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 32;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
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
