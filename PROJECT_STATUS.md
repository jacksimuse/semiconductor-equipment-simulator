# 프로젝트 현황 (핸드오프 노트)

> 두 에이전트가 번갈아 작업할 때 **받는 쪽이 먼저 읽는 문서.**
> 작업을 넘길 때마다 이 문서를 갱신한다. 상세 규칙은 [UNITY_MCP_GUIDE.md](UNITY_MCP_GUIDE.md).
> 최종 갱신: 2026-07-13 (Claude)

---

## 지금까지 만든 것

### A-트랙 — 장비/로봇/통신 (Claude, `Assets/DigitalTwin/**`)

**6축 로봇** (`Scripts/Runtime/`, `Scripts/Editor/RobotBuilder.cs`)
- FK + 조그 UI (`SixAxisRobot`, `RobotJogUI`), IK (`RobotIK`, CCD), 티칭/재생 (`RobotTeach`)
- 웨이퍼 픽/플레이스 시나리오 (`WaferScenario`) — FOUP 5슬롯 + 챔버
- 충돌 감지 → E-stop 인터락 (`SafetyMonitor`): 로봇 링크 콜라이더 vs `Obstacle` 레이어 관통 감지. **로봇은 kinematic Transform**(물리 차단 아님, 감지→정지 방식).
- 메뉴: `Tools/Digital Twin/Build 6-Axis Robot`, `Build Wafer Scenario`

**통신 (Phase 5)** (`Scripts/Runtime/Comms/`)
- 백엔드 추상화: `IEquipmentBackend` + `SimulatorBackend` + `EquipmentLink` + `MainThreadDispatcher`
- **SECS/GEM 서브셋(순수 C#)** `Comms/Secs/`: `SecsItem`(SECS-II) · `HsmsServer`(HSMS-SS, :5000) · `GemEquipment`(가상 GEM 장비)
  - 구현: S1F1/F2, S1F13/F14, S1F3/F4(SVID), S2F41/F42(원격명령), S6F11(이벤트), S5F1(알람=충돌)
- 파이썬 호스트 테스터: `tools/secs_host.py` (무의존)

**Station 프레임워크** (`StationContract/`, `Stations/`)
- 계약: `StationBase` · `StationDefinition` · `StationRegistry` · `StationStatus`(+`Command(verb)`) — **동결**
- **웨이퍼 얼라이너** (`Stations/Aligner/`): `Aligner.prefab` + `AlignerDefinition.asset`. 노치 정렬 검증됨.

### E-트랙 — 세계관 (Codex, `Assets/Onboarding/**`, `Onboarding.unity`)
- `WORLD_BUILDING_PLAN.md` 기준. JSM Semiconductor Equipment 세계관, 챕터 0~7, NPC, 시설 맵.
- 온보딩 전용 씬 `Assets/Scenes/Onboarding.unity` 사용.

---

## 진행 중 / 다음 (Claude)

1. **RobotStation 어댑터** (다음 우선) — 기존 로봇을 `StationBase`로 래핑.
   `Command`: StartCycle/StopCycle/ResetEStop/Home. `GetStatus`: busy=IsRunning, eStop=SafetyMonitor.EStop, text=Status, lastEvent=SafetyMonitor.LastEvent.
   → Codex의 M3 수직 슬라이스(걸어가서 로봇 제어) / Chapter 2 를 열어줌.
2. 신규 장비: **로드포트/FOUP → 식각 챔버 → CMP → 계측기** (각 `Stations/<장비>/` 자체완결 모듈 + 어댑터).

## 다음 (Codex)
- StationDefinition/MissionDefinition 교육 문구, 챕터 흐름, 온보딩 씬 구성.
- 장비는 계약 API(`Enter/Exit/GetStatus/Command`)로만 호출. 새 verb/필드 필요 시 Claude에 요청.

---

## 알려진 이슈 / 주의
- `Assets/Scenes/DigitalTwin.unity`에 예전 `OnboardingTrainingArea`가 남아 있을 수 있음(Onboarding.unity로 이관됐으면 중복). 제거는 Claude가 확인 후 처리 예정.
- 동시 작업 중 `.cs` 저장 → 재컴파일 → 상대 Play 종료됨. 번갈아 사용 원칙 준수.
