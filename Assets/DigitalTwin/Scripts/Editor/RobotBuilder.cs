using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DigitalTwin
{
    /// <summary>
    /// 메뉴 클릭 한 번으로 6축 로봇 계층 전체를 코드로 생성한다.
    /// Tools ▸ Digital Twin ▸ Build 6-Axis Robot
    ///
    /// 각 관절(J1~J6)은 이전 링크 끝에 위치하고, 링크 비주얼은 로컬 +Y 로 뻗는다.
    /// 홈 자세에서는 팔이 수직으로 서 있으며, J2/J3(Z축)을 돌리면 팔이 굽는다.
    /// </summary>
    public static class RobotBuilder
    {
        const string RootName = "SixAxisRobot";

        [MenuItem("Tools/Digital Twin/Build 6-Axis Robot")]
        public static void Build()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            // 독립 오브젝트(IK_Target)는 root 와 함께 지워지지 않으므로 개별 정리 (중복 방지)
            for (GameObject t; (t = GameObject.Find("IK_Target")) != null; ) Object.DestroyImmediate(t);

            var root  = new GameObject(RootName);
            var robot = root.AddComponent<SixAxisRobot>();

            // EPSON 스타일 컬러: 흰색 유광 바디 + 블루 손목 액센트.
            Color body   = new Color(0.93f, 0.94f, 0.96f);
            Color accent = new Color(0.10f, 0.34f, 0.80f);

            // 베이스 페데스탈 (흰색) + 하단 마운팅 플레이트 + 옐로우 마킹.
            CreateCylinder("BasePlate", root.transform, 0.03f, 0.150f, body);
            CreateCylinder("Base",      root.transform, 0.22f, 0.120f, body);
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mark.name = "Marking";
            Object.DestroyImmediate(mark.GetComponent<Collider>());
            mark.transform.SetParent(root.transform, false);
            mark.transform.localPosition = new Vector3(0f, 0.11f, -0.119f);   // 전면(-Z)
            mark.transform.localScale    = new Vector3(0.065f, 0.016f, 0.02f);
            mark.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.98f, 0.78f, 0.06f), 0.45f);

            // 관절 체인 생성. MakeJoint 가 parent 를 방금 만든 pivot 으로 갱신한다.
            Transform parent = root.transform;
            var joints = new SixAxisRobot.Joint[6];
            //                       이름  offY   회전축            min   max   링크길이 링크반경 부모반경  색
            joints[0] = MakeJoint(ref parent, "J1", 0.22f, Vector3.up,      -180, 180, 0.12f, 0.105f, 0.120f, body);   // 베이스 요(yaw)
            joints[1] = MakeJoint(ref parent, "J2", 0.12f, Vector3.forward,  -90,  90, 0.40f, 0.090f, 0.105f, body);   // 숄더
            joints[2] = MakeJoint(ref parent, "J3", 0.40f, Vector3.forward, -150, 150, 0.38f, 0.076f, 0.090f, body);   // 엘보
            joints[3] = MakeJoint(ref parent, "J4", 0.38f, Vector3.up,      -180, 180, 0.12f, 0.068f, 0.076f, accent); // 리스트 롤 (블루 손목)
            joints[4] = MakeJoint(ref parent, "J5", 0.12f, Vector3.forward, -120, 120, 0.10f, 0.062f, 0.068f, accent); // 리스트 피치 (블루 손목)
            joints[5] = MakeJoint(ref parent, "J6", 0.10f, Vector3.up,      -360, 360, 0.06f, 0.072f, 0.062f, body);   // 툴 플랜지

            // TCP (엔드이펙터 기준점)
            var tcp = new GameObject("TCP").transform;
            tcp.SetParent(parent, false);
            tcp.localPosition = new Vector3(0f, 0.08f, 0f);

            robot.joints = joints;
            robot.tcp    = tcp;

            // Phase 2 — IK 타깃(드래그용 구) + CCD 솔버
            var targetGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetGo.name = "IK_Target";
            targetGo.transform.SetParent(null, true);         // 로봇과 독립된 월드 목표점
            targetGo.transform.position   = tcp.position;      // 홈 자세 TCP에서 시작
            targetGo.transform.localScale = Vector3.one * 0.06f;
            targetGo.GetComponent<MeshRenderer>().sharedMaterial =
                MakeMat(new Color(0.10f, 0.90f, 0.90f));

            var ik = root.AddComponent<RobotIK>();
            ik.target = targetGo.transform;

            root.AddComponent<RobotTeach>();   // Phase 3 — 티칭 & 재생
            root.AddComponent<RobotJogUI>();

            // Phase 6 — 충돌 인터락(E-stop). 로봇 링크 vs Obstacle 레이어 관통 감지.
            var safety = root.AddComponent<SafetyMonitor>();
            safety.robot = robot;
            safety.ik    = ik;
            int obLayer  = LayerMask.NameToLayer("Obstacle");
            safety.obstacleMask = obLayer >= 0 ? (1 << obLayer) : 0;

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[DigitalTwin] 6축 로봇 생성 완료. ▶ Play 를 누르면 좌상단에 조그 UI 가 나타납니다.");
        }

        // ── Phase 4 — 웨이퍼 시나리오 (FOUP + 챔버 + 웨이퍼) ──────────
        [MenuItem("Tools/Digital Twin/Build Wafer Scenario")]
        public static void BuildScenario()
        {
            var robot = Object.FindFirstObjectByType<SixAxisRobot>();
            if (robot == null)
            {
                Debug.LogError("[DigitalTwin] 먼저 Tools ▸ Digital Twin ▸ Build 6-Axis Robot 을 실행하세요.");
                return;
            }

            var old = GameObject.Find("WaferScenario");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("WaferScenario");
            var scen = root.AddComponent<WaferScenario>();
            scen.robot = robot;
            scen.ik    = robot.GetComponent<RobotIK>();

            // 안전 인터락에 시나리오 연결(충돌 시 사이클 정지).
            var safety = robot.GetComponent<SafetyMonitor>();
            if (safety != null) safety.scenario = scen;

            const int   N   = 5;
            const float y0  = 0.45f;   // 최하단 슬롯 높이
            const float dy  = 0.09f;   // 슬롯 간격
            var slotBase    = new Vector3(0.46f, 0f, 0.30f); // 슬롯 X,Z (로봇 앞쪽)
            bool[] occupied = { true, true, true, false, true }; // 슬롯4(idx3) 비움 → 매핑 데모

            // FOUP 하우징
            var foup = new GameObject("FOUP").transform;
            foup.SetParent(root.transform, false);
            var housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "Housing";
            // 웨이퍼가 꽂힌 벽 = 충돌 장애물. 기본 BoxCollider 유지 + Obstacle 레이어.
            int obLayer = LayerMask.NameToLayer("Obstacle");
            if (obLayer >= 0) housing.layer = obLayer;
            housing.transform.SetParent(foup, true);
            housing.transform.position   = new Vector3(slotBase.x + 0.10f, y0 + dy * (N - 1) * 0.5f, slotBase.z);
            housing.transform.localScale  = new Vector3(0.06f, dy * N + 0.06f, 0.30f);
            housing.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.68f, 0.70f, 0.74f));

            var slots  = new Transform[N];
            var wafers = new Transform[N];
            for (int i = 0; i < N; i++)
            {
                var s = new GameObject($"Slot{i + 1}").transform;
                s.SetParent(foup, true);
                s.position = new Vector3(slotBase.x, y0 + i * dy, slotBase.z);
                slots[i] = s;

                if (occupied[i])
                {
                    var w = MakeWafer();
                    w.SetParent(s, true);
                    w.position = s.position;
                    wafers[i] = w;
                }
            }
            scen.slots  = slots;
            scen.wafers = wafers;

            // 챔버 (플랫폼 + 로드 위치)
            var chamber = new GameObject("Chamber").transform;
            chamber.SetParent(root.transform, false);
            var spotPos = new Vector3(0.40f, 0.48f, -0.40f);

            var plat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plat.name = "Platform";
            Object.DestroyImmediate(plat.GetComponent<Collider>());
            plat.transform.SetParent(chamber, true);
            plat.transform.position   = spotPos + new Vector3(0f, -0.03f, 0f);
            plat.transform.localScale  = new Vector3(0.30f, 0.03f, 0.30f);
            plat.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.38f, 0.40f, 0.50f));

            var spot = new GameObject("ChamberSpot").transform;
            spot.SetParent(chamber, true);
            spot.position = spotPos;
            scen.chamberSpot = spot;

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[DigitalTwin] 웨이퍼 시나리오 생성 완료 (FOUP {N}슬롯). ▶ Play → '사이클 실행'.");
        }

        /// <summary>얇은 원반(웨이퍼) 생성. 지름 0.20m, 두께 ~0.008m.</summary>
        static Transform MakeWafer()
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            w.name = "Wafer";
            Object.DestroyImmediate(w.GetComponent<Collider>());
            w.transform.localScale = new Vector3(0.20f, 0.004f, 0.20f);
            w.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.76f, 0.79f, 0.84f));
            return w.transform;
        }

        // 관절 액추에이터(하우징) 공통 색 — 흰색 바디와 이어지는 라이트 실버.
        static readonly Color JointColor = new Color(0.80f, 0.81f, 0.84f);

        static SixAxisRobot.Joint MakeJoint(ref Transform parent, string name, float offsetY,
            Vector3 axis, float min, float max, float linkLen, float linkR, float parentR, Color col)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(0f, offsetY, 0f);

            // 관절 비주얼: 회전축 방향 모터 하우징 + 틈 메움 구 + +Y 링크.
            float jr = Mathf.Max(parentR, linkR);
            CreateJointHousing(pivot, axis, jr * 1.10f, jr * 3.0f, JointColor); // 모터 배럴(축 방향)
            CreateSphere("Knuckle", pivot, jr * 1.18f, col);                    // 관절 구(회전 틈 메움)
            CreateCylinder("Link", pivot, linkLen, linkR, col, collider: true); // +Y 로 뻗는 링크(충돌 콜라이더 부착)

            parent = pivot; // 다음 관절은 이 pivot 의 자식이 된다
            return new SixAxisRobot.Joint
            {
                name = name, pivot = pivot, axis = axis, min = min, max = max, target = 0f
            };
        }

        /// <summary>pivot 원점에 놓인 구(관절 사이 틈을 메워 매끄럽게 이어 보이게).</summary>
        static GameObject CreateSphere(string name, Transform parent, float radius, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            var c = go.GetComponent<Collider>();
            if (c) Object.DestroyImmediate(c);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one * (radius * 2f);
            go.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(col);
            return go;
        }

        /// <summary>pivot 원점에서 회전축(axis) 방향으로 놓인 짧은 실린더(모터 하우징).</summary>
        static GameObject CreateJointHousing(Transform parent, Vector3 axis, float radius, float length, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Housing";
            var c = go.GetComponent<Collider>();
            if (c) Object.DestroyImmediate(c);

            go.transform.SetParent(parent, false);
            // 기본 실린더는 +Y 축. 회전축 방향으로 눕힌다.
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            go.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(col);
            return go;
        }

        /// <summary>+Y 방향으로 length 만큼 뻗는 실린더 생성. collider=true 면 링크 충돌용 CapsuleCollider 부착.</summary>
        static GameObject CreateCylinder(string name, Transform parent, float length, float radius, Color col, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            var col0 = go.GetComponent<Collider>();
            if (col0) Object.DestroyImmediate(col0); // 기본(메시) 콜라이더 제거

            go.transform.SetParent(parent, false);
            // 유니티 기본 실린더는 Y로 2unit(±1). scale.y = length/2 → 높이 length.
            go.transform.localScale    = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            go.transform.localPosition = new Vector3(0f, length * 0.5f, 0f);
            go.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(col);

            if (collider)
            {
                // 링크 메시(높이 2, 반경 0.5)에 맞춘 캡슐. 로컬 스케일이 실제 치수로 변환됨.
                // CapsuleCollider 는 볼록(convex) → Physics.ComputePenetration 에 안전.
                var cap = go.AddComponent<CapsuleCollider>();
                cap.direction = 1;      // Y축
                cap.height    = 2f;
                cap.radius    = 0.5f;
                cap.center    = Vector3.zero;
            }
            return go;
        }

        /// <summary>URP 프로젝트 대응: URP/Lit 우선, 없으면 Standard. 광택(smoothness)·메탈릭 지원.</summary>
        static Material MakeMat(Color col, float smoothness = 0.72f, float metallic = 0f)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            else m.color = col;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness); // Standard 폴백
            if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", metallic);
            return m;
        }
    }
}
