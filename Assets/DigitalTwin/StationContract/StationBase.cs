using UnityEngine;

namespace DigitalTwin.Stations
{
    /// <summary>
    /// 장비 상태 요약(미션 채점·HMI 용). 셸/미션이 읽는 표준 필드.
    ///   busy      → IsCycleRunning (동작 중: 로봇=사이클 실행, 얼라이너=정렬 중)
    ///   text      → CurrentStatusText (현재 상태 문자열)
    ///   eStop     → HasEStop (E-STOP/인터락 발동)
    ///   lastEvent → LastSafetyEvent (최근 이벤트/안전 이벤트 문자열)
    /// </summary>
    public struct StationStatus
    {
        public bool   busy;      // 동작 중
        public bool   eStop;     // E-STOP/인터락 발동
        public bool   fault;     // 기타 알람/오류
        public float  progress;  // 0..1 현재 작업 진행
        public string text;      // 상태 문자열
        public string lastEvent; // 최근 이벤트/안전 이벤트
    }

    /// <summary>
    /// 모든 장비 제어 모듈이 상속하는 계약. 게임 셸(E-트랙)은 이 규약으로만 장비를 다룬다.
    ///   Enter()/Exit() : 제어 모드 진입/이탈 (입력·UI 활성/비활성)
    ///   GetStatus()    : 미션 채점·HMI 표시용 상태
    /// 실제 장비 로직은 하위 컨트롤러가 담당하고, 이 클래스는 얇은 어댑터로 유지한다.
    ///
    /// ★ 공유 계약 ★ : 변경은 두 트랙 합의 필요(동결).
    /// </summary>
    public abstract class StationBase : MonoBehaviour
    {
        public StationDefinition definition;
        public bool IsActive { get; private set; }

        public void Enter() { if (IsActive) return; IsActive = true;  OnEnter(); }
        public void Exit()  { if (!IsActive) return; OnExit(); IsActive = false; }

        protected abstract void OnEnter();
        protected abstract void OnExit();
        public abstract StationStatus GetStatus();

        /// <summary>
        /// 장비별 명령 실행. 셸/미션이 verb 로 호출한다(예: "StartCycle","StopCycle","ResetEStop","Home","Align").
        /// 지원하지 않는 verb 는 false. 각 장비 어댑터가 지원 verb 를 오버라이드로 구현.
        /// </summary>
        public virtual bool Command(string verb) => false;
    }
}
