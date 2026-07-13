using DigitalTwin.Stations;
using UnityEngine;

namespace Onboarding
{
    public class StationInteractionPoint : MonoBehaviour
    {
        public StationDefinition definition;
        public TrainingMissionController mission;

        void OnTriggerEnter(Collider other)
        {
            if (mission == null) mission = FindFirstObjectByType<TrainingMissionController>();
            if (mission == null) return;

            if (other.GetComponent<EngineerPlayerController>() != null)
                mission.EnterStation(definition);
        }
    }
}
