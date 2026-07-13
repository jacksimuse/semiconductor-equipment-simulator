# 세계관 설계 계획 — 반도체 장비 회사 경험 레이어

> Codex 담당 범위: 반도체 장비 회사라는 세계관, 교육 흐름, 캐릭터, 부서, 시설, 미션 내러티브, 진행도, 평가 경험.
> Claude Code 담당 범위: 실제 반도체 장비, 로봇, 제어, 통신, SECS/GEM/OPC-UA 등 디지털 트윈 구현.

이 문서는 `EXPERIENCE_LAYER_PLAN.md`의 상위 세계관 기준 문서입니다. 장비 구현을 침범하지 않고, 장비가 교육 콘텐츠로 자연스럽게 연결되는 회사 경험을 설계합니다.

---

## 1. 제품 콘셉트

가칭:

- **FAB-LINK Academy**
- **JSM Semiconductor Equipment Training Center**
- **반도체 장비 회사 신입 엔지니어 온보딩 시뮬레이터**

플레이어는 반도체 장비 회사에 입사한 신입 엔지니어입니다. 회사 내부 교육센터, 클린룸, 제어실, 데모룸, 유지보수실을 돌아다니며 실제 장비 투입 전 필요한 개념과 조작 절차를 배웁니다.

핵심 감정은 “회사에 입사해서 실제 장비를 하나씩 배워간다”입니다. 장비는 Claude Code가 만드는 정교한 스테이션이고, Codex는 그 스테이션들이 왜 존재하고 어떤 순서로 배우는지 설계합니다.

---

## 2. 역할 분리

| 영역 | Codex | Claude Code |
| --- | --- | --- |
| 회사명, 부서, 장소, NPC | 담당 | 참고만 |
| 탐험/온보딩/미션 흐름 | 담당 | 장비 진입 지점 연결 |
| StationDefinition, MissionDefinition의 교육 문구 | 담당 | 장비 제어 API/상태 제공 |
| 장비 모델, 로봇, 센서, HMI | 호출/연결만 | 담당 |
| 실제 통신, 디지털 트윈 동기화 | 세계관상 의미 부여 | 담당 |
| 평가/피드백 문구, 해금 구조 | 담당 | 성공/실패 신호 제공 |
| Unity 씬 직접 배치 | 원칙적으로 하지 않음 | MCP로 담당 |

Codex는 `Assets/Onboarding/**`, 문서, 데이터 정의, 시나리오를 중심으로 작업합니다.
`Assets/DigitalTwin/**`의 장비 코드와 `RobotBuilder.cs`는 Claude Code 담당 영역입니다.

---

## 3. 회사 설정

회사명 후보:

- **JSM Semiconductor Equipment**
- **FabLink Systems**
- **NeoFab Automation**

권장 기본명: **JSM Semiconductor Equipment**

회사 소개:

JSM Semiconductor Equipment는 반도체 물류 자동화, 웨이퍼 핸들링, 로봇 이송 모듈, 장비 제어 소프트웨어를 개발하는 장비 회사입니다. 고객사는 학교, 연구소, 반도체 제조사, 장비 교육기관입니다.

회사의 핵심 시설:

- **Onboarding Lobby**: 신입 엔지니어 접수, 사원증, 교육 안내
- **Training Cleanroom**: 교육용 장비가 배치된 클린룸
- **Robot Transfer Lab**: 6축 로봇, FOUP, 챔버 이송 실습
- **Control Room**: HMI, 알람 로그, 장비 상태 모니터링
- **Maintenance Bay**: 센서 교체, 티칭 포인트 보정, E-STOP 복구
- **Customer Demo Hall**: 납품 전 고객사 데모와 PoC 시연
- **Twin Operations Room**: 실제 장비와 디지털 트윈 통신 상태 확인

---

## 4. 주요 등장인물

### 플레이어

- 역할: 신입 장비 엔지니어
- 목표: 회사의 교육 과정을 통과하고 장비 데모를 직접 수행할 수 있는 수준이 되는 것
- 성장: 관찰 → 조작 → 문제 해결 → 고객 데모 → 트윈 운용

### 교육 담당자

- 이름 후보: 한서윤 매니저
- 역할: 전체 온보딩 진행, 다음 장소 안내, 미션 해금
- 말투: 차분하고 명확함

### 선배 장비 엔지니어

- 이름 후보: 박도현 책임
- 역할: 로봇, 티칭, 웨이퍼 이송, 안전 인터락 실습 안내
- 말투: 현장 경험 기반의 짧은 조언

### 제어 소프트웨어 엔지니어

- 이름 후보: 이지훈 선임
- 역할: HMI, 알람 로그, 통신 상태, 디지털 트윈 모드 설명
- 말투: 시스템 관점, 로그와 상태값 중심

### 안전 관리자

- 이름 후보: 최민아 책임
- 역할: E-STOP, 인터락, 클린룸 동선, 작업 허가 절차
- 말투: 단호하고 규정 중심

### 고객사 담당자

- 이름 후보: 김태준 매니저
- 역할: 마지막 데모 평가, 실제 납품 요구사항 제시
- 말투: 결과와 안정성 중심

---

## 5. 챕터 구조

### Chapter 0. 첫 출근

- 장소: Onboarding Lobby
- 목표: 프로필 생성, 교육 과정 시작
- 장비 제어 없음
- 완료 조건: 사원증 발급, Training Cleanroom 안내 받기

### Chapter 1. 장비실 입장

- 장소: Training Cleanroom
- 목표: 캐릭터로 장비실에 들어가 장비 포커스 모드 진입
- 연결 스테이션: Robot Transfer Lab
- 완료 조건: 장비 조작 패널 확인

### Chapter 2. 첫 웨이퍼 이송 실습

- 장소: Robot Transfer Lab
- 목표: FOUP에서 챔버로 웨이퍼 이송 사이클 수행
- Claude Code 제공 신호:
  - 웨이퍼 매핑 상태
  - 사이클 시작/정지
  - 완료/실패 상태
  - E-STOP 여부
- Codex 제공 경험:
  - 미션 목표
  - 조작 안내
  - 완료 피드백

### Chapter 3. 티칭 포인트 보정

- 목표: 좌표가 어긋난 장비를 보정
- 학습 개념: 티칭, 반복 정밀도, 장비 설치 편차
- 평가: 보정 후 충돌 없이 이송 성공

### Chapter 4. 알람 대응

- 장소: Control Room
- 목표: 알람 로그를 보고 원인을 추론
- 예시 알람:
  - Wafer Not Detected
  - Robot Position Error
  - Chamber Door Interlock
  - Vacuum Grip Fail
  - Collision Risk Detected

### Chapter 5. 안전 인터락

- 장소: Maintenance Bay
- 목표: E-STOP 발생, 원인 확인, 복구 절차 수행
- 학습 개념: 충돌 감지, 세이프티존, 리셋 절차

### Chapter 6. 디지털 트윈 운용

- 장소: Twin Operations Room
- 목표: 실제 장비 또는 가상 장비의 상태를 트윈에 반영
- Claude Code 담당: 통신 연결, 상태 미러링, 명령 송수신
- Codex 담당: 트윈 모드 설명, 운영 절차, 사용자 피드백

### Chapter 7. 고객사 데모

- 장소: Customer Demo Hall
- 목표: 고객 앞에서 장비 이송, 안전, 알람 대응, 트윈 상태를 시연
- 완료 조건: 데모 점수, 납품 준비 리포트 생성

---

## 6. 시설 맵 방향

초기 M2/M3에서는 큰 회사를 만들지 않습니다. 아래처럼 작은 수직 슬라이스로 시작합니다.

```text
Lobby
  ↓
Training Cleanroom Entrance
  ↓
Robot Transfer Lab
  ↓
Debrief Panel
```

이후 확장:

```text
Lobby
├─ Training Cleanroom
│  ├─ Robot Transfer Lab
│  ├─ Aligner Station
│  ├─ Etch Chamber Station
│  └─ Metrology Station
├─ Control Room
├─ Maintenance Bay
├─ Twin Operations Room
└─ Customer Demo Hall
```

---

## 7. Station 경험 규약

모든 장비는 회사 세계관 안에서 다음 구조로 소개됩니다.

```text
장비명
역할
현장에서 쓰이는 이유
오늘 배울 것
조작 전 안전 확인
미션 목표
성공 기준
실패 시 피드백
다음 연결 교육
```

예시: 6축 로봇 웨이퍼 이송 스테이션

- 장비명: Robot Transfer Module
- 역할: FOUP와 챔버 사이에서 웨이퍼를 이송
- 오늘 배울 것: 조그, 홈 복귀, 웨이퍼 사이클, E-STOP 개념
- 성공 기준: 사이클 완료, 안전 위반 없음, 제한시간 내 완료
- 다음 교육: 티칭 포인트 보정

---

## 8. 문구 톤

교육기관 납품을 고려해 과장된 SF보다 실제 회사 교육 느낌을 유지합니다.

- 좋은 톤: 현장 교육, 안전, 품질, 절차, 로그, 데모, 납품
- 피할 톤: 전투, 판타지, 지나친 세계관 과장, 비현실적 해킹

NPC 대사는 짧고 목적이 분명해야 합니다.

예:

> “장비를 움직이기 전에 홈 위치와 E-STOP 상태를 먼저 확인하세요.”

> “오늘 목표는 웨이퍼를 빠르게 옮기는 게 아니라, 충돌 없이 절차대로 이송하는 것입니다.”

---

## 9. 다음 Codex 작업

1. `Assets/Onboarding/**`를 경험 레이어 전용 루트로 유지
2. `Assets/Scenes/Onboarding.unity` 전용 씬 기준으로 온보딩 빌더 유지
3. Company/Floor/Station/Mission 데이터 정의를 추가
4. `StationDefinition`과 `MissionDefinition`의 표시 문구와 진행 구조 설계
5. 장비 API는 Claude Code가 제공하는 래퍼만 사용

---

## 10. Claude Code 연동 규칙

Codex가 Claude Code에 넘기는 요청은 장비 구현 세부가 아니라 인터페이스 형태로 작성합니다. 장비 연동은 동결된 Station 계약만 사용합니다.

계약 위치:

```text
Assets/DigitalTwin/StationContract/
```

Codex가 읽는 타입:

- `StationDefinition`: `id`, `displayName`, `description`, `controlPrefab`, `thumbnail`, `missionIds`
- `StationRegistry`: `StationDefinition[] stations`
- `StationBase`: `Enter()`, `Exit()`, `GetStatus()`, `Command(string verb)`
- `StationStatus`: `busy`, `text`, `eStop`, `lastEvent`, `fault`, `progress`

Codex는 계약 파일을 직접 수정하지 않습니다. `Assets/DigitalTwin/**`의 장비 구현, 프리팹, 통신, 씬 검증은 Claude Code가 담당합니다.

현재 알려진 명령:

- 6축 로봇: `StartCycle`, `StopCycle`, `ResetEStop`, `Home`
- 얼라이너: `Align`, `Stop`

예:

```text
Robot Transfer Station에서 다음 상태를 읽을 수 있게 해줘:
- IsCycleRunning
- CurrentStatusText
- HasEStop
- LastSafetyEvent
- StartCycle()
- StopCycle()
- ResetEStop()
```

Claude Code는 실제 장비·로봇·통신 구현을 담당하고, Codex는 그 값을 이용해 교육 흐름과 피드백을 구성합니다.
