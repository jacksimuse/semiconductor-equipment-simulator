# 프로젝트 현황 (핸드오프 노트)

> 두 에이전트가 번갈아 작업할 때 **받는 쪽이 먼저 읽는 문서.**
> 작업을 넘길 때마다 이 문서를 갱신한다. 상세 규칙은 [UNITY_MCP_GUIDE.md](UNITY_MCP_GUIDE.md).
> 최종 갱신: 2026-07-13 (Claude — RobotStation 어댑터 완료)

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
- **6축 로봇 스테이션** (`Stations/Robot/`): `RobotStation`(어댑터) + `RobotDefinition.asset`. verb: StartCycle/StopCycle/ResetEStop/Home. 계약 API(GetStatus/Command/Enter/Exit) 검증됨. 로봇 루트(DigitalTwin.unity)에 부착. **접근**: 로봇은 씬 기반이라 controlPrefab=null → 셸은 DigitalTwin.unity 를 additive 로드해 `RobotStation`(id="robot") 사용.

### E-트랙 — 세계관 (Codex, `Assets/Onboarding/**`, `Onboarding.unity`)
- `WORLD_BUILDING_PLAN.md` 기준. JSM Semiconductor Equipment 세계관, 챕터 0~7, NPC, 시설 맵.
- 온보딩 전용 씬 `Assets/Scenes/Onboarding.unity` 사용.
- `OnboardingTrainingArea`는 `DigitalTwin.unity`에서 분리되어 `Onboarding.unity`에만 존재함.
- 신입 엔지니어가 방향키/WASD로 키오스크 앞까지 이동한 뒤 `E`로 장비 제어 모드에 진입하는 흐름으로 변경됨.
- 장비 포커스 모드에서 `Esc` 또는 `Backspace`로 캐릭터 조작 모드에 복귀 가능.
- 탐색 모드에서 `C`로 카메라 시점 전환 가능.
- 키오스크 본체 콜라이더는 물리 차단용 Solid, 주변 감지 영역은 별도 Trigger 자식 오브젝트로 분리.
- 얼라이너 교육 데이터 추가: `StationLearningProfile` + `MissionDefinition` (`Assets/Onboarding/Content/`).
- 장비 제어는 `StationBase.Enter/Exit/GetStatus/Command` 계약으로만 호출함. 현재 얼라이너 미션은 `Command("Align")` 사용.
- 시설 구현 확장:
  - Lobby, Gowning Area, Training Cleanroom, Robot Transfer Lab, Control Room, Maintenance Bay, Twin Operations Room, Customer Demo Hall.
  - 전체 안전 바닥(`FacilitySafetyFloor`) 추가로 바닥 밖 이동 시 추락하지 않도록 처리.
  - 구역별 가이드 패널, 바닥 동선, 구역 외곽선, 주요 설비 목업 배치.
- 미션/가이드 HUD 추가:
  - 하단 안내에서 중앙 대화창 형태로 확대 변경.
  - `다음`/`이전` 페이지 버튼, `요약`/`상세` 전환, `확인`/`닫기` 버튼.
  - 우상단 `알림` 버튼으로 현재 진행 중인 미션 확인.
  - 알림 패널은 스크롤로 긴 미션 내용을 볼 수 있음.
  - 같은 구역에 반복 진입할 때 미션 페이지/텍스트가 계속 초기화되어 깜빡이는 현상을 방지.
- 캐릭터 이미지 적용:
  - `Assets/플레이어.png`
  - `Assets/한서윤.png`
  - `Assets/박도현.png`
  - `Assets/이지훈.png`
  - `Assets/최민아.png`
  - `Assets/김태준.png`
  - `FacilityGuideHud` 왼쪽 캐릭터 영역에 PNG 표시.
  - 현재 미션 구역에 따라 담당 캐릭터 자동 전환:
    - 기본 안내: 한서윤 매니저
    - Robot Transfer Lab: 박도현 책임
    - Control Room / Twin Operations Room: 이지훈 선임
    - Training Cleanroom / Maintenance Bay: 최민아 책임
    - Customer Demo Hall: 김태준 매니저
- `OnboardingBuilder`는 씬을 다시 빌드할 때 위 캐릭터 이미지를 자동 참조함.
- `FacilityGuideHud`는 씬 참조가 비어 있어도 Editor Play에서 `Assets/*.png`를 fallback 로딩함.
- 최근 Unity 검증:
  - 이전 단계에서 `Onboarding.unity` 씬 검증 통과 및 Play 진입 후 콘솔 error/warning 0개 확인.
  - 캐릭터 이미지 표시/깜빡임 방지 변경 이후에는 아직 Play 검증 전.

---

## 진행 중 / 다음 (Claude)

1. ✅ **RobotStation 어댑터 완료** — `Stations/Robot/RobotStation` + `RobotDefinition.asset`. verb StartCycle/StopCycle/ResetEStop/Home, GetStatus 매핑(busy/eStop/text/lastEvent)·Enter/Exit 검증됨. **Codex는 이제 Chapter 2 로봇 이송 미션을 붙일 수 있음**(접근: DigitalTwin.unity additive 로드 → RobotStation id="robot").
2. 신규 장비(다음 우선): **로드포트/FOUP → 식각 챔버 → CMP → 계측기** (각 `Stations/<장비>/` 자체완결 모듈 + 어댑터).
3. Claude가 Play 검증할 때 확인할 항목:
   - Onboarding 씬에서 HUD 텍스트 깜빡임이 사라졌는지.
   - 미션 팝업 왼쪽에 캐릭터 이미지가 표시되는지.
   - `E`로 장비 제어 모드 진입, `Esc`/`Backspace`로 캐릭터 조작 복귀가 유지되는지.
   - 키오스크 본체 통과가 막히고 Trigger 영역에서만 상호작용 안내가 뜨는지.

## 다음 (Codex)
- StationDefinition/MissionDefinition 교육 문구, 챕터 흐름, 온보딩 씬 구성 계속 확장.
- RobotStation 어댑터가 들어오면 Chapter 2 로봇 이송 미션용 `StationLearningProfile`/`MissionDefinition` 추가.
- 장비는 계약 API(`Enter/Exit/GetStatus/Command`)로만 호출. 새 verb/필드 필요 시 Claude에 요청.
- 다음 세계관 작업 후보:
  - 각 NPC별 대사 세트 분리.
  - 미션별 담당자/초상화/요약/상세 안내를 ScriptableObject 데이터로 이동.
  - 고객 데모 평가 루브릭 추가.

---

## 알려진 이슈 / 주의
- `Assets/Scenes/DigitalTwin.unity`의 예전 `OnboardingTrainingArea` 중복 이슈는 처리됨. 현재 `OnboardingTrainingArea`는 `Assets/Scenes/Onboarding.unity`에만 있음.
- 동시 작업 중 `.cs` 저장 → 재컴파일 → 상대 Play 종료됨. 번갈아 사용 원칙 준수.
- Codex 작업 범위는 계속 `Assets/Onboarding/**`, `Assets/Scenes/Onboarding.unity`, 세계관/문서 중심.
- Claude 작업 범위는 계속 `Assets/DigitalTwin/**`, 장비/로봇/통신/Station 구현 중심.
- `Assets/DigitalTwin/StationContract/**` 계약은 직접 수정하지 말 것. 새 상태 필드/verb가 필요하면 Claude가 계약 확장 여부를 판단.
