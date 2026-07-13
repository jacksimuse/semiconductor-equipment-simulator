# 6축 로봇 디지털 트윈 — 실행 계획서 (MCP 세션용)

> 반도체 장비과 수업용. 이 문서는 **UnityMCP 도구가 연결된 Claude 세션**에서
> 단계별로 실행하기 위한 청사진입니다. 각 Phase 는 순서대로 진행하며,
> 완료 기준(Acceptance)을 만족한 뒤 다음 단계로 넘어갑니다.

---

## 0. MCP 세션 운영 규칙 (매 단계 공통)

- **Unity 에디터를 항상 켜 둔다.** 스크립트 편집 중에는 **Play 모드 해제** 상태로.
- 스크립트 생성/수정 후에는 **`read_console` 로 컴파일 에러 확인** → 에러 0 확인 후 진행.
- 씬 조작 후 **`manage_scene` 로 hierarchy 재확인**, 변경 시 **씬 저장**.
- 사용 예상 MCP 도구: `manage_script`(C# CRUD) · `manage_gameobject`(오브젝트/컴포넌트)
  · `manage_scene`(씬/계층) · `manage_asset`(머티리얼/프리팹/SO) · `execute_menu_item`(메뉴 실행)
  · `manage_editor`(Play/Pause/상태) · `read_console`(로그).
- 좌표/단위: **미터(m), Unity 좌표계 Y-up**. 실제 로봇/URDF 는 보통 Z-up → **가져올 때 축 변환** 주의.

---

## Phase 0 — 토대 (일부 완료)

**상태:** `Assets/DigitalTwin/Scripts` 스캐폴드 + 빌더 메뉴 이미 존재.

**할 일**
1. 전용 씬 생성: `manage_scene` → `Assets/DigitalTwin/Scenes/DigitalTwin.unity`.
2. 바닥/조명/카메라 정리: `manage_gameobject` 로 Ground(Plane) 추가, 카메라를 로봇(약 1.1m)이
   잘 보이도록 배치(예: pos (1.2, 0.9, -1.6), 로봇 바라보게).
3. 로봇 생성: `execute_menu_item("Tools/Digital Twin/Build 6-Axis Robot")`.
4. 검증: `manage_editor` 로 Play → `read_console` 에러 0, 조그 UI 표시 확인.

**MCP 프롬프트 예시**
> "DigitalTwin.unity 씬을 만들고, 바닥 Plane 과 조명·카메라를 로봇이 보이게 배치한 뒤,
>  Tools/Digital Twin/Build 6-Axis Robot 메뉴를 실행하고 콘솔 에러를 확인해줘."

**완료 기준:** Play 시 6축 로봇 + 조그 슬라이더 표시, 콘솔 에러 없음.

---

## Phase 1 — FK + 조인트 조그 (코드 완료, 검증/보정 단계)

**할 일**
1. 조그 슬라이더로 각 축 회전 확인. **회전 방향/축이 어색하면** `SixAxisRobot.Joint.axis`
   부호 또는 `min/max` 를 `manage_script` 로 보정.
2. **TCP 좌표계 기즈모** 추가(엔드이펙터에 X/Y/Z 화살표) — 시각적 디버깅.
3. (선택) 조그를 "연속(hold)" 방식 버튼으로도 추가: +/- 버튼 누르는 동안 각도 증가.

**완료 기준:** 6축 모두 의도한 방향으로 회전, TCP 좌표가 실시간 갱신.

---

## Phase 2 — IK (역기구학)

**목표:** 화면의 목표점을 지정하면 로봇이 스스로 자세를 잡는다.

**설계**
- 알고리즘: **DLS(Damped Least Squares) + 수치 야코비안(유한차분)**.
  DH 파라미터 없이 현재 Transform 체인에 바로 적용 가능, 특이점 근처에서 안정적.
  (대안: CCD — 더 단순, 정밀도 낮음 / 해석해 — 구형 손목 가정 시 정확하지만 로봇 특화)
- 반복: 목표와 TCP 오차 → 야코비안 J → Δθ = Jᵀ(JJᵀ+λ²I)⁻¹·e, 관절 리밋 클램프, N회 반복.
- 모드 전환: **Jog(FK) ↔ IK-Follow** 토글.

**할 일 (MCP)**
1. `manage_script`: `IKSolver.cs`(DLS 반복), `IKTarget.cs`(목표 핸들).
2. `manage_gameobject`: 이동 가능한 목표 Sphere 생성, `IKTarget` 부착.
3. UI: IK 모드에서 목표를 드래그하면 TCP 추종. 위치 오차 표시.

**완료 기준:** 목표 구를 움직이면 TCP 가 허용오차(예 1mm) 내로 추종, 리밋 초과 시 자연스럽게 정지.

---

## Phase 3 — 티칭 & 재생 (궤적)

**목표:** 여러 포즈를 저장하고 부드럽게 순차 재생 (PTP/Linear).

**설계**
- 데이터: `Waypoint`(관절각 또는 TCP 포즈), `RobotProgram`(ScriptableObject, 웨이포인트 리스트).
- 모션: **관절공간 보간 + 사다리꼴 속도 프로파일**(가속/등속/감속). 옵션으로 **직선(Cartesian)**
  이동 = TCP 포즈 보간 후 각 스텝 IK.
- 속도/가속 상한, 루프 재생, 일시정지.

**할 일 (MCP)**
1. `manage_script`: `RobotProgram.cs`(SO), `MotionPlanner.cs`(보간/속도프로파일), `TeachPendantUI.cs`.
2. `manage_asset`: 샘플 `RobotProgram` 애셋 생성.
3. UI: **Teach**(현재 포즈 기록) · **Play** · 속도 슬라이더 · Loop 토글.

**완료 기준:** 포즈 3~4개 티칭 후 재생 → 끊김 없는 PTP 이동, 사이클 반복.

---

## Phase 4 — 반도체 시나리오 (웨이퍼 이송)

**목표:** FOUP ↔ 프로세스 챔버 웨이퍼 픽/플레이스 자동 사이클.

**설계**
- 오브젝트: **FOUP/로드포트**, **카세트(슬롯 N개)**, **웨이퍼(얇은 원판)**, **프로세스 챔버**,
  (선택) 얼라이너.
- 그리퍼: 엔드이펙터에 attach point. **Pick = 웨이퍼를 TCP 의 자식으로 reparent**,
  **Place = 목표 슬롯의 자식으로 reparent**.
- **웨이퍼 매핑**: 슬롯별 유/무 감지 시뮬레이션.
- 시퀀스(상태머신): Home → FOUP 접근 → 슬롯N 픽 → 챔버 이동 → 플레이스 → 후퇴 → 복귀.
  Phase 3 의 MotionPlanner + Program 재사용.
- **인터락**: 도어 열림 시 모션 정지.

**할 일 (MCP)**
1. `manage_gameobject`/`manage_asset`: FOUP·카세트·웨이퍼·챔버 프리팹 생성/배치.
2. `manage_script`: `WaferHandler.cs`(pick/place reparent), `Slot.cs`, `EquipmentSequencer.cs`(상태머신).
3. 시퀀스 실행 버튼 + 진행 표시.

**완료 기준:** 버튼 한 번으로 FOUP→챔버 이송 사이클 완주, 웨이퍼가 그리퍼에 붙었다 슬롯에 안착,
사이클 타임 표시.

---

## Phase 5 — 디지털 트윈 통신 (핵심)

**목표:** 외부 컨트롤러(가상 PLC/로봇제어기)와 **양방향 실시간 동기화**.

**설계**
- 프로토콜 로드맵:
  1) **PoC — MQTT 또는 TCP+JSON** (구현 쉬움, 빠른 검증)
  2) **표준화 — OPC-UA** (스마트팩토리 표준)
  3) **반도체 특화 — SECS/GEM** (SEMI 표준, 차별화 포인트)
- 데이터 흐름:
  - **Inbound**(실물→트윈): 컨트롤러의 조인트 각 스트림 → `SixAxisRobot.SetJoint` 미러링.
  - **Outbound**(트윈→상위): TCP 포즈·상태·웨이퍼 이벤트 → 컨트롤러/HMI.
- **스레딩**: 통신은 **백그라운드 스레드**, Unity API 는 메인 스레드 전용 →
  **`ConcurrentQueue` 로 메인 스레드에 마샬링**. (중요)
- 메시지 스키마: JSON `{ t, joints[6], tcp{p,r}, status, event }`.

**할 일 (MCP + 외부)**
1. `manage_script`: `TwinBridge.cs`(전송 추상 인터페이스), `MqttBridge.cs`(또는 `TcpBridge.cs`),
   `MainThreadDispatcher.cs`(큐 마샬링).
2. 외부 테스트용 **`sim_controller.py`**(조인트 스트림 송출) 작성 — 프로젝트 밖 `tools/` 에.
3. 패키지: MQTT 는 **MQTTnet/M2Mqtt**, OPC-UA 는 **UA-.NETStandard** 등 →
   Unity 호환성/임포트 방식(NuGetForUnity 또는 DLL) 확인 필요.

**완료 기준:** `sim_controller.py` 실행 시 Unity 로봇이 외부 조인트 스트림을 실시간 미러링,
지연시간(latency) 표시. Unity 조작이 상위로도 송출됨.

---

## Phase 6 — HMI / 안전 / 로깅

**목표:** 운전 화면 + 안전 인터락 + 데이터 로깅.

**설계**
- **HMI 대시보드**(uGUI 또는 UI Toolkit): 조인트각·TCP·웨이퍼수·WPH·사이클타임·알람 배너.
- **안전**: 소프트리밋(완료) · **충돌 감지**(콜라이더 + OnTriggerEnter → E-stop) ·
  **세이프티존**(트리거 볼륨) · 라이트커튼 시뮬.
- **로깅**: 조인트/TCP/이벤트 **CSV 기록** + 재생.

**할 일 (MCP)**
1. `manage_script`: `HmiDashboard.cs`, `SafetyMonitor.cs`(충돌/존/E-stop), `DataLogger.cs`(CSV).
2. `manage_gameobject`: 충돌용 콜라이더·세이프티존 트리거 배치.

**완료 기준:** 대시보드 실시간 갱신, 강제 충돌 시 E-stop 발동·알람, 로그 파일 생성.

---

## 교차 관심사 (전 단계 공통)

- **버전관리:** 프로젝트에 Unity VCS(collab-proxy) 설치됨. Git 사용 시 `.gitignore`(Library/Temp 제외) 추가 권장.
- **테스트:** Unity Test Framework 설치됨 → IK/보간/픽플레이스 로직에 EditMode 테스트 작성 가능.
- **URDF 이관(선택):** 실기 정확도가 필요하면 `com.unity.robotics.urdf-importer` 로 UR5 등 임포트,
  현재 제네릭 팔을 교체(축·링크 길이 정확).
- **단계 전환:** kinematic → 물리(ArticulationBody)는 그리퍼 힘·충돌이 실제로 필요한 Phase 4~5부터 검토.

---

## 진행 순서 요약

```
Phase 0 (토대) → 1 (FK/Jog) → 2 (IK) → 3 (티칭/재생)
   → 4 (웨이퍼 시나리오) → 5 (트윈 통신) → 6 (HMI/안전/로깅)
```
각 Phase 완료 기준 충족 → 씬/스크립트 커밋 → 다음 Phase.
