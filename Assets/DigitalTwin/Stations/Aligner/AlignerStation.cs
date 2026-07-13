using UnityEngine;

namespace DigitalTwin.Stations.Aligner
{
    /// <summary>얼라이너를 게임 셸 계약(StationBase)에 연결하는 얇은 어댑터.</summary>
    public class AlignerStation : StationBase
    {
        public AlignerController controller;
        public AlignerPanelUI   panel;

        protected override void OnEnter() { if (panel) panel.uiEnabled = true; }
        protected override void OnExit()
        {
            if (panel) panel.uiEnabled = false;
            if (controller) controller.StopAlign();
        }

        public override StationStatus GetStatus()
        {
            bool aligned = controller != null && controller.IsAligned;
            return new StationStatus
            {
                busy      = controller != null && controller.Aligning,
                eStop     = false,
                fault     = false,
                progress  = aligned ? 1f : 0f,
                text      = controller == null ? "미연결" : (aligned ? "정렬 완료" : $"오차 {controller.NotchError:F1}°"),
                lastEvent = ""
            };
        }

        public override bool Command(string verb)
        {
            if (controller == null) return false;
            switch ((verb ?? "").Trim())
            {
                case "Align":  controller.StartAlign(); return true;
                case "Stop":   controller.StopAlign();  return true;
                default:       return false;
            }
        }
    }
}
