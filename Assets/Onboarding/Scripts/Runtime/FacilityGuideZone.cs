using UnityEngine;

namespace Onboarding
{
    [RequireComponent(typeof(BoxCollider))]
    public class FacilityGuideZone : MonoBehaviour
    {
        public string zoneId;
        public string displayName;
        public FacilityGuideHud guideHud;

        void Reset()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<EngineerPlayerController>() == null) return;

            if (guideHud == null)
                guideHud = FindAnyObjectByType<FacilityGuideHud>();
            if (guideHud != null)
                guideHud.EnterZone(this);
        }
    }
}
