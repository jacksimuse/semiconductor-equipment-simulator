using UnityEngine;

namespace DigitalTwin.Stations.Aligner
{
    /// <summary>Play 모드 IMGUI 제어 패널(단독 동작). 셸 진입 시 uiEnabled 로 표시 제어.</summary>
    public class AlignerPanelUI : MonoBehaviour
    {
        public AlignerController controller;
        public bool  uiEnabled = true;
        public float uiScale = 2.0f;

        static int nextId = 62000;
        Rect win = new Rect(12, 12, 300, 0);
        int id;

        void Awake()
        {
            if (controller == null) controller = GetComponent<AlignerController>();
            id = nextId++;
        }

        void OnGUI()
        {
            if (!uiEnabled || controller == null) return;
            var prev = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(uiScale, uiScale), Vector2.zero);
            win = GUILayout.Window(id, win, Draw, "웨이퍼 얼라이너");
            GUI.matrix = prev;
        }

        void Draw(int _)
        {
            bool repaint = Event.current.type == EventType.Repaint;
            float step = controller.spinSpeed * Time.deltaTime;

            GUILayout.Label($"노치 각도: {controller.NotchAngle:F1}°   목표: {controller.targetAngle:F0}°");
            GUILayout.Label($"오차: {controller.NotchError:F1}°   {(controller.IsAligned ? "✔ 정렬됨" : "정렬 필요")}");

            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("◀ CCW") && repaint) controller.Jog(-step);
            if (GUILayout.RepeatButton("CW ▶")  && repaint) controller.Jog(step);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (!controller.Aligning) { if (GUILayout.Button("자동 정렬")) controller.StartAlign(); }
            else if (GUILayout.Button("정지")) controller.StopAlign();
            if (GUILayout.Button("무작위 틀기")) controller.SetOrientation(Random.Range(0f, 360f));
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }
    }
}
