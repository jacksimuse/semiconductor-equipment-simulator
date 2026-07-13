using DigitalTwin.Stations;
using UnityEngine;

namespace Onboarding
{
    public class StationInteractionPoint : MonoBehaviour
    {
        public StationDefinition definition;
        public StationLearningProfile learningProfile;
        public TrainingMissionController mission;

        void OnTriggerEnter(Collider other)
        {
            if (mission == null) mission = FindAnyObjectByType<TrainingMissionController>();
            if (mission == null) return;

            if (other.GetComponent<EngineerPlayerController>() != null)
                mission.FocusStation(this);
        }

        void OnTriggerStay(Collider other)
        {
            if (mission == null) mission = FindAnyObjectByType<TrainingMissionController>();
            if (mission == null) return;

            if (other.GetComponent<EngineerPlayerController>() != null)
                mission.FocusStation(this);
        }

        void OnTriggerExit(Collider other)
        {
            if (mission == null) return;

            if (other.GetComponent<EngineerPlayerController>() != null)
                mission.ClearFocusedStation(this);
        }
    }
}
