using UnityEngine;

namespace Onboarding
{
    [CreateAssetMenu(menuName = "Onboarding/Mission Definition", fileName = "MissionDefinition")]
    public class MissionDefinition : ScriptableObject
    {
        public string id = "mission";
        public string stationId = "station";
        public string title = "미션";
        [TextArea] public string briefing;
        [TextArea] public string successCriteria;
        [TextArea] public string successFeedback;
        [TextArea] public string failureFeedback;
        public string startCommand = "StartCycle";
        public float targetProgress = 1f;
        public float timeLimitSeconds = 60f;
    }
}
