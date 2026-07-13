# 게임형 교육 시뮬레이터 — 확장 계획서 (경험 레이어)

> 기존 "6축 로봇 제어"를 **반도체 장비 회사 탐험형 교육 게임**으로 확장한다.
> 학생이 로그인 → 캐릭터로 클린룸을 걸어다니며 → 장비에 다가가면
> "장비를 제어하시겠습니까?" 프롬프트 → 확인 시 **해당 장비의 제어 모드**로 진입.
> 마치 게임처럼 미션을 수행하고 평가받는 구조.
>
> **핵심 설계 목표: 장비(스테이션)를 계속 추가할 수 있는 데이터-드리븐 확장 구조.**

---

## 0. 담당 범위

- **Codex:** 회사 세계관, 온보딩 흐름, 탐험/상호작용/미션/평가 경험, `Assets/Onboarding/**` 코드와 데이터.
- **Claude Code:** 반도체 장비, 로봇, 제어, 통신, 실제 장비 연동, `Assets/DigitalTwin/**` 장비 코드와 Unity MCP 씬 검증.
- **연동 방식:** Codex는 장비 내부 구현을 직접 만지지 않고, Claude Code가 제공하는 스테이션 API와 상태값을 이용해 교육 경험을 만든다.
- **장비 계약:** `Assets/DigitalTwin/StationContract/`의 `StationDefinition`, `StationRegistry`, `StationBase`, `StationStatus`를 읽고 사용만 한다. 계약 수정은 Claude Code에 요청한다.

상위 세계관 기준은 [WORLD_BUILDING_PLAN.md](WORLD_BUILDING_PLAN.md)를 따른다.

---

## 1. 핵심 게임 루프

```
[로그인] → [탐험 모드: 캐릭터로 이동]
              │  장비 근접 → 프롬프트 "제어하시겠습니까? [E]예 / [Esc]아니오"
              ▼ (확인)
        [장비 제어 모드: 스테이션별 미션 수행]  ── 6축 로봇 이송 / 에처 / 얼라이너 …
              │  미션 완료 → 채점·피드백
              ▼
        [디브리핑] → 진행도 저장 → [탐험 모드로 복귀] (다음 장비 해금)
```

---

## 2. 전체 아키텍처 (시스템 분해)

| 시스템 | 역할 | 대표 스크립트 |
|--------|------|---------------|
| **GameManager (상태머신)** | 앱 전체 모드 전환 관리, 씬 로딩 | `GameManager`, `GameState` |
| **Session/Profile** | 학생 프로필(학번/이름), 진행도 저장 | `StudentProfile`, `SaveSystem` |
| **Explore (탐험)** | 캐릭터 이동·카메라·충돌 | `PlayerController`, `FollowCamera` |
| **Interaction (상호작용)** | 근접 감지 + 프롬프트 + 확인 | `Interactable`, `InteractionPrompt` |
| **Station Framework** | 장비 제어 모드 추상화·진입/이탈 | `StationBase`, `StationRegistry` |
| **Mission/Assessment** | 목표·채점·피드백·해금 | `MissionDef(SO)`, `MissionRunner`, `Scorer` |
| **UI Layers** | HUD / 프롬프트 / 제어패널 / 결과 | `HudUI`, `ResultUI`, `MenuUI` |
| **Facility (레벨)** | 클린룸 배치, 사이니지, 미니맵 | 씬/프리팹, `Minimap` |
| **EventBus** | 시스템 간 느슨한 결합 | `GameEvents`(이벤트 채널) |

**설계 원칙**
- **씬 구성:** `Bootstrap`(영속) + `Facility`(탐험, 상시) + `Station_*`(제어, **Additive 로드/언로드**).
- **결합도:** 시스템은 `EventBus`(ScriptableObject 이벤트 채널)로 통신 → 서로 하드 의존 X.
- **데이터-드리븐:** 장비/미션은 **ScriptableObject 로 정의** → 코드 수정 없이 콘텐츠 추가.

---

## 3. 앱 상태머신 (GameState)

```
Boot → Login → Lobby(탐험) ⇄ Interacting → StationControl → Debrief → Lobby
                                                     │
                                              (Pause/Menu 어디서나)
```
- `GameManager` 는 `DontDestroyOnLoad`, 상태 전환 시 씬 additive 로드/언로드.
- 전환은 이벤트로: `RequestEnterStation(stationId)` → 로드 → 카메라/입력 전환.

---

## 4. ★확장성의 핵심 — Station Framework (데이터-드리븐)

**장비를 계속 추가하려면 "장비 = 데이터 + 규약" 으로 만든다.**

- **`StationDefinition` (ScriptableObject)**: `id`, `표시이름`, `설명`, `제어씬 이름`,
  `해금 조건`, `미션 리스트`, `썸네일`. → **새 장비 = SO 애셋 1개 + 제어 프리팹/씬 1개.**
- **`StationRegistry` (SO)**: 모든 StationDefinition 목록. 탐험 씬의 각 장비 오브젝트는
  자신의 `StationDefinition` 만 참조.
- **`StationBase` (추상 MonoBehaviour)**: 모든 제어 모드가 상속.
  `OnEnter()`, `OnExit()`, `GetMissions()`, `EvaluateResult()` 규약.
  → **6축 로봇 제어 = `RobotStation : StationBase`** (기존 SixAxisRobot/IK/Sequencer 재사용).
- **추가 예시 장비:** 로드포트/FOUP, 웨이퍼 얼라이너, 식각(Etch) 챔버, CMP, 계측기…
  각각 `EtcherStation : StationBase` 처럼 껍데기만 구현하면 플랫폼에 자동 편입.

> 이렇게 하면 "장비 추가" 가 **코어 수정 없이 애셋 작업**으로 끝난다. ← 확장 계획의 심장.

---

## 5. 단계별 로드맵 (경험 레이어 = E-트랙)

> 기존 로봇 제어(Phase 0~6)는 **A-트랙**으로 병행. E-트랙이 그 위의 "게임 셸".

### Phase E0 — 앱 셸 & 상태머신
- `GameManager`, `GameState`, `EventBus`, Bootstrap 씬, Additive 씬 로더.
- **완료 기준:** 버튼으로 Login↔Lobby↔가짜 Station 씬 additive 전환.

### Phase E1 — 로그인 / 프로필  *(주의: 실제 인증 아님)*
- **학번/이름 선택 또는 입력 → 로컬 프로필**. 진행도 JSON/PlayerPrefs 저장.
- (실제 비밀번호·계정 인증은 구현하지 않음. 필요 시 학교 LMS 연동은 별도 백엔드 과제.)
- **완료 기준:** 프로필 생성/불러오기, 진행도 유지.

### Phase E2 — 탐험 모드 (캐릭터 이동)
- **Input System**(설치됨) 기반 `PlayerController`(방향키/WASD) + 3인칭 팔로우 카메라.
- 클린룸 그레이박스(바닥/벽/장비 자리) 배치. 이동 충돌.
- **완료 기준:** 방향키로 시설을 걸어다니고 벽/장비에 막힘.

### Phase E3 — 상호작용 (프롬프트)
- `Interactable`(트리거 볼륨 + 하이라이트) + 레이캐스트/근접 감지.
- 근접 시 **"장비를 제어하시겠습니까? [E]예 / [Esc]아니오"** 프롬프트 UI.
- 클릭 또는 키 → `RequestEnterStation(id)` 이벤트 발행.
- **완료 기준:** 장비 앞에서 프롬프트 표시, 확인 시 이벤트 발행 확인.

### Phase E4 — 스테이션 편입 & 모드 전환 ★
- `StationBase`/`StationDefinition`/`StationRegistry` 구축.
- **기존 6축 로봇 제어를 `RobotStation` 으로 래핑**, 제어 씬 additive 로드.
- 진입 시: 캐릭터 입력 잠금 → 제어 UI 활성 / 이탈 시 반대. 카메라 전환.
- **완료 기준:** 걸어가서 로봇 장비 클릭 → 제어 모드 진입 → 조작 → ESC로 탐험 복귀.

### Phase E5 — 미션 / 평가 / 피드백
- `MissionDefinition`(SO): 목표·성공조건·제한시간·힌트·배점.
- `MissionRunner` + `Scorer`: 예) "슬롯3 웨이퍼를 챔버로 60초 내 무충돌 이송".
- 디브리핑 화면: 사이클타임·안전위반·점수·개선 피드백. 성공 시 다음 장비 **해금**.
- **완료 기준:** 미션 수행→채점→피드백→진행도 저장→해금.

### Phase E6 — 콘텐츠 확장 (데이터-드리븐 증명)
- 장비 2~3종 추가(예: 얼라이너, 식각 챔버)를 **SO + 제어 프리팹만으로** 편입.
- 미니 장비별 미션 세트. 시설 맵 확장.
- **완료 기준:** 코어 코드 수정 없이 신규 장비/미션 추가 성공.

### Phase E7 — 폴리시 & 교육 기능
- 미니맵/내비게이션 화살표, 튜토리얼, 사운드/피드백, 일시정지 메뉴.
- **강사용 대시보드/분석**: 학생별 완료·점수·소요시간 로깅(교육 효과 측정).
- (선택) 디지털 트윈 통신(A-트랙 Phase 5) 연동 — 실장비 상태를 게임 내 반영.
- (선택) 멀티플레이/협동 — 향후 확장 여지.

---

## 6. 기술 선택 메모

- **입력:** `com.unity.inputsystem`(설치됨) 사용 — Action Map 을 "Explore"/"Station" 으로 분리해 모드별 입력 전환.
- **씬:** Additive 로딩 + `DontDestroyOnLoad` GameManager. 스테이션은 언로드로 메모리 회수.
- **카메라:** 단순 팔로우로 시작, 필요 시 Cinemachine 패키지 추가.
- **UI:** uGUI(설치됨)로 HUD/메뉴. 데이터 많은 대시보드는 UI Toolkit 고려.
- **저장:** 로컬 JSON(`Application.persistentDataPath`). 후에 백엔드/LMS 연동 여지.
- **데이터-드리븐:** Station/Mission = ScriptableObject. 콘텐츠 팀(비프로그래머)도 추가 가능.

---

## 7. 기존 작업과의 통합 지도

| 기존(A-트랙) | 확장 후 위치 |
|--------------|--------------|
| `SixAxisRobot`, `RobotJogUI`, IK, Sequencer | `RobotStation` 내부 제어 로직으로 재사용 |
| `RobotBuilder`(메뉴 생성) | 로봇 스테이션 프리팹 생성기로 유지 |
| 디지털 트윈 통신(Phase 5) | Station 제어 중 실시간 데이터 소스로 선택 연결 |
| 웨이퍼 픽/플레이스(Phase 4) | RobotStation 미션의 성공 판정 로직 |

---

## 8. 진행 순서 요약

```
E0 앱셸 → E1 로그인/프로필 → E2 탐험 → E3 상호작용
   → E4 스테이션 편입(로봇) → E5 미션/평가 → E6 콘텐츠 확장 → E7 폴리시
```
각 Phase 완료 기준 충족 → 커밋 → 다음. **E4 까지 가면 "게임처럼 걸어가서 장비 제어" 코어 완성.**

## 9. 주의 (안전/범위)
- 여기서 "로그인" 은 **학생 프로필 선택/입력**이며, **실제 비밀번호·계정 인증은 구현 대상이 아님**.
  실인증이 필요하면 별도 보안 백엔드 과제로 분리.
