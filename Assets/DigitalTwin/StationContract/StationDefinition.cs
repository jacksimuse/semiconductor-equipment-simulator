using UnityEngine;

namespace DigitalTwin.Stations
{
    /// <summary>
    /// 장비(스테이션) 정의 — 데이터-드리븐 확장의 핵심.
    /// 새 장비 = 이 SO 애셋 1개 + 제어 프리팹 1개. 코어 수정 불필요.
    ///
    /// ★ 공유 계약 ★ : 게임 셸(E-트랙, Codex)이 소비한다. 변경은 두 트랙 합의 필요(동결).
    /// </summary>
    [CreateAssetMenu(menuName = "DigitalTwin/Station Definition", fileName = "StationDefinition")]
    public class StationDefinition : ScriptableObject
    {
        public string id = "station";
        public string displayName = "장비";
        [TextArea] public string description;
        public GameObject controlPrefab;   // 셸이 진입 시 인스턴스화할 제어 프리팹
        public Sprite thumbnail;
        public string[] missionIds;        // 이 장비의 미션 ID (E5 MissionDefinition 연결)
    }

    /// <summary>모든 StationDefinition 목록. 탐험 씬/셸이 참조해 장비를 열거한다.</summary>
    [CreateAssetMenu(menuName = "DigitalTwin/Station Registry", fileName = "StationRegistry")]
    public class StationRegistry : ScriptableObject
    {
        public StationDefinition[] stations;
    }
}
