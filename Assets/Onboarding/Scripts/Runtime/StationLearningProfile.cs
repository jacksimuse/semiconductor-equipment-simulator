using UnityEngine;

namespace Onboarding
{
    [CreateAssetMenu(menuName = "Onboarding/Station Learning Profile", fileName = "StationLearningProfile")]
    public class StationLearningProfile : ScriptableObject
    {
        public string stationId = "station";
        public string chapterTitle = "장비 실습";
        [TextArea] public string roleInFab;
        [TextArea] public string lessonGoal;
        [TextArea] public string safetyNote;
        public MissionDefinition[] missions;

        public MissionDefinition FirstMission =>
            missions != null && missions.Length > 0 ? missions[0] : null;
    }
}
