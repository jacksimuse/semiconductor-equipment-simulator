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

설계 기준:

- 삼성전자, SK하이닉스, TSMC 같은 실제 제조사의 내부 구조를 직접 복제하지 않는다.
- 공개 자료와 반도체 산업의 일반적인 Fab 운영 개념을 기반으로 가상의 교육용 회사를 만든다.
- 실제 Recipe, 생산 라인 배치, 보안 구역, 장비 UI, 고객사 데이터는 사용하지 않는다.
- 교육용으로 설명 가능한 수준의 장비 역할, 상태, 안전 절차, HMI 흐름을 만든다.

회사 포지션:

- **장비 제조사 + 교육센터**: 실제 양산 Fab가 아니라, 장비 납품 전 교육과 PoC를 수행하는 회사.
- **고객군**: 반도체 특성화고/대학, 기업 교육팀, 연구소, 장비 운용 인력 양성 기관.
- **제품군**: 웨이퍼 이송 교육 장비, 로봇 셀, 얼라이너, 공정 챔버 모형, 계측 스테이션, 디지털 트윈 플랫폼.
- **수익 모델**: 오픈소스 기반 신뢰 확보 → 교육 패키지, 커스터마이징, 강의/워크숍, 유지보수, 실장비 연동 PoC.

회사의 핵심 시설:

- **Onboarding Lobby**: 신입 엔지니어 접수, 사원증, 교육 안내
- **Training Cleanroom**: 교육용 장비가 배치된 클린룸
- **Robot Transfer Lab**: 6축 로봇, FOUP, 챔버 이송 실습
- **Control Room**: HMI, 알람 로그, 장비 상태 모니터링
- **Maintenance Bay**: 센서 교체, 티칭 포인트 보정, E-STOP 복구
- **Customer Demo Hall**: 납품 전 고객사 데모와 PoC 시연
- **Twin Operations Room**: 실제 장비와 디지털 트윈 통신 상태 확인

### 3.1 회사 조직

| 부서 | 역할 | 플레이어가 배우는 것 |
| --- | --- | --- |
| Training Center | 신입/고객 교육 운영 | 장비 접근 절차, 기본 조작, 평가 |
| Equipment Engineering | 장비 구조, 로봇, 센서, 안전 인터락 | 장비가 왜 멈추고 어떻게 복구되는지 |
| Control Software Team | HMI, 로그, SECS/GEM, 디지털 트윈 | 상태값, 명령, 알람, 통신 흐름 |
| Process Demo Team | 고객사 공정 시나리오 구성 | 장비를 공정 흐름 안에서 설명하는 법 |
| Field Service Team | 설치, 유지보수, 현장 대응 | 티칭, 센서 점검, 부품 교체, 고객 대응 |
| Safety & Quality | 클린룸 규정, E-STOP, 작업 허가 | 안전 우선 절차와 품질 기록 |

### 3.2 시설 상세

#### Onboarding Lobby

- 첫 출근 장소.
- 사원증, 교육 일정, 오늘의 미션을 확인한다.
- 실제 장비 조작은 없고 이동/상호작용/카메라 조작을 익힌다.

#### Gowning Area

- 클린룸 입장 전 준비 구역.
- 방진복 착용, 장갑, 마스크, 안전 수칙을 간단한 체크리스트로 처리한다.
- 게임상으로는 “클린룸 입장 권한 해금” 역할.

#### Training Cleanroom

- 교육용 장비가 배치된 메인 공간.
- 실제 대기업 Fab의 복잡한 라인 대신 장비 학습에 필요한 모듈만 배치한다.
- 장비 주변에는 키오스크, 상태등, 안전 구역 표시, 바닥 동선이 있다.

#### Robot Transfer Lab

- FOUP, 로드포트, 얼라이너, 로봇, 챔버 모형이 연결된 웨이퍼 이송 실습 공간.
- 첫 번째 수직 슬라이스의 핵심 장소.
- 학습 목표는 “빠른 생산”이 아니라 “절차대로 안전하게 이송”이다.

#### Control Room

- 장비 상태, 알람 로그, 통신 상태를 확인하는 공간.
- 플레이어는 장비 앞에서 보지 못한 정보를 로그/HMI로 해석한다.
- 추후 강사용 대시보드와도 연결할 수 있다.

#### Maintenance Bay

- 장비를 멈춘 뒤 점검하는 공간.
- 센서 오염, 도어 인터락, 로봇 Home 위치, E-STOP 복구를 실습한다.
- “멈춘 장비를 무작정 재시작하지 않는다”는 절차 교육에 적합하다.

#### Twin Operations Room

- 가상 장비와 실제 장비 또는 외부 시뮬레이터의 상태를 비교한다.
- SECS/GEM, TCP, MQTT, OPC-UA 같은 통신 개념을 교육용으로 추상화한다.
- 실장비가 연결되면 디지털 트윈 모드, 없으면 시뮬레이터 백엔드 모드로 동작한다.

#### Customer Demo Hall

- 납품 전 고객에게 보여주는 최종 평가 장소.
- 플레이어가 배운 조작, 안전 대응, 로그 설명, 트윈 상태 설명을 종합한다.

### 3.3 회사 안의 하루 흐름

```text
출근
  ↓
Onboarding Lobby에서 오늘의 교육 확인
  ↓
Gowning Area에서 클린룸 입장 체크
  ↓
Training Cleanroom에서 장비 접근
  ↓
키오스크로 장비 설명 확인
  ↓
Station 제어 모드 진입
  ↓
미션 수행 / 알람 대응 / 로그 확인
  ↓
Control Room 또는 Debrief Panel에서 피드백 확인
  ↓
다음 장비 또는 다음 챕터 해금
```

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
- 목표: 프로필 생성, 회사 소개, 교육 과정 시작
- 장비 제어 없음
- 완료 조건: 사원증 발급, Training Cleanroom 안내 받기
- 핵심 대사:
  - 한서윤 매니저: “여기는 실제 양산 Fab가 아니라 교육용 장비센터입니다. 하지만 절차는 현장 기준으로 배웁니다.”
  - 최민아 책임: “장비보다 먼저 안전입니다. 이동, 정지, 복구 순서를 몸에 익히세요.”

### Chapter 1. 장비실 입장

- 장소: Training Cleanroom
- 목표: 캐릭터로 장비실에 들어가 키오스크 앞에서 장비 포커스 모드 진입
- 연결 스테이션: 웨이퍼 얼라이너 또는 Robot Transfer Lab
- 완료 조건: 장비 조작 패널 확인 후 다시 캐릭터 조작 모드로 복귀
- 학습 요소:
  - 키오스크 접근
  - 장비 설명 확인
  - `E`로 제어 모드 진입
  - `Esc` 또는 `Backspace`로 탐험 모드 복귀
  - `C`로 카메라 시점 전환

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
- 대표 시나리오:
  - “FOUP 슬롯 3번에 있는 웨이퍼를 챔버 입구까지 이송한다.”
  - “시작 전 Home 상태와 E-STOP 상태를 확인한다.”
  - “동작 중에는 불필요한 반복 명령을 보내지 않는다.”
  - “완료 후 상태가 Completed인지 확인한다.”

### Chapter 3. 티칭 포인트 보정

- 목표: 좌표가 어긋난 장비를 보정
- 학습 개념: 티칭, 반복 정밀도, 장비 설치 편차
- 평가: 보정 후 충돌 없이 이송 성공
- 대표 상황:
  - 로봇이 슬롯 중앙보다 약간 빗나간 위치로 접근한다.
  - 선배 엔지니어가 “생산 장비는 설치 후 기준점 보정이 필요하다”고 설명한다.
  - 플레이어는 Teaching Point를 조정하고 테스트 사이클을 수행한다.

### Chapter 4. 알람 대응

- 장소: Control Room
- 목표: 알람 로그를 보고 원인을 추론
- 예시 알람:
  - Wafer Not Detected
  - Robot Position Error
  - Chamber Door Interlock
  - Vacuum Grip Fail
  - Collision Risk Detected
- 대표 흐름:
  - 장비가 멈춘다.
  - 플레이어가 장비 앞에서 무작정 Reset을 누르면 실패 피드백.
  - Control Room에서 알람 로그를 확인한다.
  - 원인 확인 → 안전 상태 확인 → Reset → 재시작 순서로 해결한다.

### Chapter 5. 안전 인터락

- 장소: Maintenance Bay
- 목표: E-STOP 발생, 원인 확인, 복구 절차 수행
- 학습 개념: 충돌 감지, 세이프티존, 리셋 절차
- 대표 흐름:
  - 작업자가 안전 구역 안에 있거나 장애물이 감지된다.
  - 장비는 즉시 정지한다.
  - 플레이어는 장애물 제거, 안전 구역 확인, E-STOP Reset, Home 복귀 순서로 복구한다.

### Chapter 6. 디지털 트윈 운용

- 장소: Twin Operations Room
- 목표: 실제 장비 또는 가상 장비의 상태를 트윈에 반영
- Claude Code 담당: 통신 연결, 상태 미러링, 명령 송수신
- Codex 담당: 트윈 모드 설명, 운영 절차, 사용자 피드백
- 대표 흐름:
  - 시뮬레이터 백엔드와 연결한다.
  - 장비 상태값이 트윈 화면에 반영되는지 확인한다.
  - 명령을 보낸 뒤 실제 장비 상태와 트윈 상태가 일치하는지 확인한다.
  - 통신 끊김 상황에서 “명령 실패/상태 갱신 지연”을 해석한다.

### Chapter 7. 고객사 데모

- 장소: Customer Demo Hall
- 목표: 고객 앞에서 장비 이송, 안전, 알람 대응, 트윈 상태를 시연
- 완료 조건: 데모 점수, 납품 준비 리포트 생성
- 대표 흐름:
  - 고객 요구: “교육생이 10분 안에 웨이퍼 이송과 알람 대응을 이해할 수 있어야 합니다.”
  - 플레이어가 장비 사이클, 안전 정지, 로그 설명, 디지털 트윈 상태를 순서대로 시연한다.
  - 결과 리포트에는 완료 시간, 안전 위반, 알람 대응 정확도, 설명 품질이 기록된다.

---

## 5.1 장비군 설정

교육용 회사에는 다음 장비군을 둡니다. 실제 장비의 외형과 조작은 단순화하되, 역할과 절차는 현실적인 흐름을 유지합니다.

| 장비 | 역할 | 외형 방향 | 대표 조작 | 학습 포인트 |
| --- | --- | --- | --- | --- |
| Load Port / FOUP | 웨이퍼 캐리어 장착 | 전면 도어, FOUP 받침, 상태등 | `Clamp`, `DoorOpen`, `MapWafers` | 슬롯, 캐리어, 웨이퍼 존재 확인 |
| Wafer Aligner | 웨이퍼 노치/방향 정렬 | 작은 원형 스테이지, 센서 암 | `Align`, `Stop` | 방향 기준, 정렬 완료 상태 |
| EFEM / Transfer Robot | FOUP와 챔버 사이 이송 | 박스형 모듈, 투명 커버, 로봇 암 | `Home`, `StartCycle`, `StopCycle` | 이송 순서, Home, 충돌 방지 |
| Process Chamber | 공정 모듈 모형 | 원통/박스형 챔버, 게이트 밸브 | `OpenGate`, `ProcessStart`, `Vent` | 챔버 인터락, 공정 전 조건 |
| Metrology Station | 결과 검사/계측 | 측정 헤드, 스테이지, 모니터 | `Measure`, `ReviewResult` | 품질 확인, Pass/Fail |
| Stocker / OHT Mock | 자동 물류 흐름 | 천장 레일 또는 보관 랙 | `RequestCarrier`, `TransferComplete` | Fab 물류, Lot 흐름 |
| Safety Panel | 비상정지/인터락 | E-STOP 버튼, 안전등 | `ResetEStop`, `Acknowledge` | 안전 절차, 복구 순서 |

### 장비 외형 원칙

- 실제 특정 회사 장비를 그대로 복제하지 않는다.
- 교육용으로 알아보기 쉬운 형태를 우선한다.
- 장비마다 다음 시각 요소를 둔다:
  - 상태등: Idle/Run/Alarm
  - 장비명 라벨
  - 안전 구역 바닥 표시
  - 키오스크 또는 HMI 패널
  - 점검 도어 또는 커버

### HMI 추상화 원칙

실제 장비 UI를 복제하지 않고 교육용 공통 HMI를 사용합니다.

공통 상태:

- `Idle`: 대기
- `Ready`: 조건 만족
- `Running`: 동작 중
- `Completed`: 완료
- `Alarm`: 알람
- `Interlock`: 안전 조건 미충족
- `Manual`: 수동 조작
- `Remote`: 외부 제어/트윈 모드

공통 버튼:

- `Initialize`
- `Home`
- `StartCycle`
- `Stop`
- `ResetAlarm`
- `ResetEStop`
- `OpenLog`
- `ReturnToExplore`

공통 표시:

- 현재 장비 상태
- 최근 이벤트
- 진행도
- 안전 인터락
- 미션 목표
- 제한시간
- 성공/실패 피드백

---

## 5.2 주요 시나리오 세트

### Scenario A. 신입 엔지니어 첫 장비 접근

목적:

- 캐릭터 이동, 카메라 전환, 키오스크 접근, 제어 모드 진입/복귀를 학습한다.

순서:

1. 로비에서 한서윤 매니저와 대화한다.
2. 클린룸 입구로 이동한다.
3. 키오스크 앞에 서서 장비 설명을 확인한다.
4. `E`로 장비 포커스 모드에 진입한다.
5. 장비 상태가 Idle인지 확인한다.
6. `Esc` 또는 `Backspace`로 캐릭터 모드로 복귀한다.

성공 기준:

- 장비 모드 진입 1회
- 캐릭터 모드 복귀 1회
- 충돌/알람 없음

### Scenario B. 웨이퍼 얼라이너 기초

목적:

- 웨이퍼 노치 정렬의 의미와 장비 상태 확인을 학습한다.

순서:

1. 얼라이너 키오스크 앞에 선다.
2. 장비 설명에서 “노치 방향 기준”을 읽는다.
3. `Align` 명령을 실행한다.
4. 진행도 100%와 Completed 상태를 확인한다.
5. 결과 피드백을 확인한다.

성공 기준:

- `Command("Align")` 실행
- `progress >= 1`
- `fault == false`
- `eStop == false`

### Scenario C. 첫 웨이퍼 이송

목적:

- FOUP에서 챔버까지 웨이퍼가 이동하는 기본 흐름을 학습한다.

순서:

1. Load Port 상태가 Ready인지 확인한다.
2. Robot Station에서 Home 상태를 확인한다.
3. `StartCycle`을 실행한다.
4. 동작 중 상태와 진행도를 관찰한다.
5. 완료 후 웨이퍼 위치와 로그를 확인한다.

성공 기준:

- 사이클 완료
- E-STOP 없음
- 충돌 이벤트 없음
- 제한시간 내 완료

Claude Code에 필요한 향후 요청:

```text
RobotStation에서 다음 상태/명령을 제공해줘:
- CurrentSlotIndex 또는 CurrentWaferSlot
- SourceLocation / TargetLocation
- Command("MapWafers")
- Command("LoadWafer")
- Command("UnloadWafer")
```

### Scenario D. 알람 로그로 원인 찾기

목적:

- 알람 발생 시 즉시 재시작하지 않고 로그를 보고 원인을 찾는 절차를 학습한다.

순서:

1. 장비가 Alarm 상태로 멈춘다.
2. 플레이어는 Control Room으로 이동한다.
3. 최근 이벤트와 알람 코드를 확인한다.
4. 안전 상태를 확인한다.
5. 원인에 맞는 복구 명령을 실행한다.

성공 기준:

- 로그 확인 전 Reset 반복 입력 없음
- 올바른 복구 순서 수행
- 복구 후 Idle 또는 Ready 상태 도달

### Scenario E. 고객사 데모 리허설

목적:

- 납품 전 교육 패키지의 핵심 흐름을 하나의 데모로 수행한다.

순서:

1. 고객사 요구사항을 읽는다.
2. 얼라이너 정렬 미션을 수행한다.
3. 로봇 이송 미션을 수행한다.
4. 의도된 알람을 처리한다.
5. 디지털 트윈 상태 동기화를 설명한다.
6. 리포트를 제출한다.

성공 기준:

- 모든 미션 완료
- 안전 위반 없음
- 알람 대응 절차 통과
- 데모 리포트 생성

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
