using UnityEngine;
using UnityEditor;

namespace DigitalTwin.Stations.Aligner
{
    /// <summary>메뉴로 얼라이너 제어 프리팹 + StationDefinition 애셋을 생성한다(씬 미변경).</summary>
    public static class AlignerBuilder
    {
        const string Dir        = "Assets/DigitalTwin/Stations/Aligner";
        const string PrefabPath = Dir + "/Aligner.prefab";
        const string DefPath    = Dir + "/AlignerDefinition.asset";

        [MenuItem("Tools/Digital Twin/Build Aligner (Prefab)")]
        public static void Build()
        {
            EnsureFolders();

            // StationDefinition 먼저 준비(프리팹이 이걸 참조).
            var def = AssetDatabase.LoadAssetAtPath<StationDefinition>(DefPath);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<StationDefinition>();
                AssetDatabase.CreateAsset(def, DefPath);
            }

            // 계층 구성
            var root  = new GameObject("Aligner");
            var body  = MakeCylinder("Body", root.transform, 0.06f, 0.16f, new Color(0.55f, 0.57f, 0.62f));
            body.transform.localPosition = new Vector3(0f, 0.03f, 0f);

            var chuck = new GameObject("Chuck").transform;
            chuck.SetParent(root.transform, false);
            chuck.localPosition = new Vector3(0f, 0.07f, 0f);

            var wafer = MakeCylinder("Wafer", chuck, 0.008f, 0.15f, new Color(0.76f, 0.79f, 0.84f));
            wafer.transform.localPosition = Vector3.zero;

            var notch = GameObject.CreatePrimitive(PrimitiveType.Cube);   // 가장자리 노치 마커
            notch.name = "Notch";
            Object.DestroyImmediate(notch.GetComponent<Collider>());
            notch.transform.SetParent(chuck, false);
            notch.transform.localPosition = new Vector3(0f, 0.006f, 0.15f);  // +Z 가장자리
            notch.transform.localScale    = new Vector3(0.02f, 0.012f, 0.02f);
            notch.GetComponent<MeshRenderer>().sharedMaterial = Mat(new Color(0.85f, 0.2f, 0.2f));

            var ctrl    = root.AddComponent<AlignerController>();
            ctrl.chuck  = chuck;
            var panel   = root.AddComponent<AlignerPanelUI>();
            panel.controller = ctrl;
            var station = root.AddComponent<AlignerStation>();
            station.controller = ctrl; station.panel = panel; station.definition = def;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            // 정의 마무리(프리팹 참조 연결)
            def.id = "aligner";
            def.displayName = "웨이퍼 얼라이너";
            def.description = "웨이퍼 노치를 목표 방향으로 정렬한다.";
            def.controlPrefab = prefab;
            EditorUtility.SetDirty(def);
            AssetDatabase.SaveAssets();

            Selection.activeObject = prefab;
            Debug.Log($"[DigitalTwin] 얼라이너 프리팹 생성: {PrefabPath}");
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/DigitalTwin/Stations"))
                AssetDatabase.CreateFolder("Assets/DigitalTwin", "Stations");
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/DigitalTwin/Stations", "Aligner");
        }

        static GameObject MakeCylinder(string name, Transform parent, float height, float radius, Color col)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            var c = go.GetComponent<Collider>();
            if (c) Object.DestroyImmediate(c);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(col);
            return go;
        }

        static Material Mat(Color col)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col); else m.color = col;
            return m;
        }
    }
}
