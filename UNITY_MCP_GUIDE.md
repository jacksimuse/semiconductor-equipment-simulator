# Unity-MCP 작업 가이드 (에이전트 공용)

> 이 프로젝트는 **Claude Code**와 **Codex** 두 에이전트가 함께 작업한다.
> 이 문서는 **누가 Unity-MCP로 Unity 에디터를 몰든** 지켜야 하는 공용 규칙이다.
> (Claude의 개인 메모리는 Codex가 못 읽으므로, 공유 규칙은 반드시 이 리포 문서에 둔다.)

---

## 0. 역할 경계

| 영역 | 담당 | 작업 루트 |
|------|------|-----------|
| 장비·로봇·제어·통신(디지털 트윈) | **Claude** | `Assets/DigitalTwin/**`, `RobotBuilder.cs`, `Assets/Scenes/DigitalTwin.unity` |
| 세계관·온보딩·미션·내러티브 | **Codex** | `Assets/Onboarding/**`, `Assets/Scenes/Onboarding.unity`, 데이터/문구 |

- 상대 영역의 파일과 **`Assets/DigitalTwin/StationContract/`(동결 계약)** 은 수정하지 않는다.
- 온보딩 오브젝트를 `DigitalTwin.unity`에 넣지 않는다(그 반대도 동일).

---

## 1. Unity-MCP는 한 번에 한 명만 (번갈아 사용)

기본은 **Claude가 Unity-MCP를 담당**한다. Claude 한도 소진 등으로 못 쓸 때만 **Codex가 임시 대행**한다.

- **절대 동시 사용 금지.** 두 에이전트가 같은 에디터를 동시에 몰면 씬/컴파일/Play가 충돌한다.
- 넘길 때는 아래 **핸드오프 체크리스트**를 통과한 "깨끗한 상태"에서만 넘긴다.

### 핸드오프 체크리스트 (넘기기 직전)
1. 컴파일 에러 **0** (`read_console` 확인)
2. **Play 모드 정지** 상태
3. 반쪽 편집 없음 (파일 저장 완료)
4. 씬 변경 시 **씬 저장** 완료
5. **git 커밋** (핸드오프 기준점) + `PROJECT_STATUS.md` 갱신

---

## 2. 스크립트/씬 작업 규칙

- **스크립트 편집은 Play 정지 상태에서.** 편집 후 컴파일 대기 → `read_console(types=["error"])` 로 에러 0 확인 후 진행.
- 디스크에서 새 `.cs`를 만들면 Unity가 자동 임포트 안 할 수 있음 → `refresh_unity(scope="all", compile="request")` 로 임포트+컴파일.
- 씬을 바꿨으면 저장. **로봇/씬 재생성(Build 메뉴)이나 씬 저장 전에는 상대에게 알린다**(공유 위험 작업).
- **빌드/Play는 Unity-MCP를 든 쪽만.** 상대가 Play 중이면 `.cs` 저장을 멈춘다(재컴파일이 상대 Play를 끊음).

## 3. 헤드리스/MCP 제어 시 Time 멈춤 주의

- MCP로 제어할 때 Play 모드에서 **`Time.deltaTime`이 0으로 멈추는** 경우가 있다. 코루틴·시간 기반 로직은 이 상태에서 진행이 안 될 수 있다.
- 따라서 **검증은 결정론적으로**: 시간에 의존하지 않는 1-스텝 메서드나 `execute_code`로 직접 상태를 구동/조회한다. (예: IK는 `Solve()` 반복 호출, 얼라이너는 `StepAlign()` 반복)
- `Update()`는 timeScale=0에서도 프레임마다 돌지만 `FixedUpdate`/물리 스텝은 멈춘다. 물리 질의는 `Physics.SyncTransforms()` 후 `Physics.ComputePenetration` 같은 직접 질의로.

## 4. 좌표/단위

- 단위 **미터(m)**, Unity 좌표계 **Y-up**. 실제 로봇/URDF는 Z-up → 가져올 때 축 변환 주의.

---

## 5. 장비 연동 계약 (Codex가 소비)

`Assets/DigitalTwin/StationContract/` — **동결, 수정 금지**(변경은 Claude에 요청).

- `StationDefinition`(SO): id·displayName·description·controlPrefab·thumbnail·missionIds
- `StationRegistry`(SO): StationDefinition[] 목록
- `StationBase`(추상): `Enter()` · `Exit()` · `GetStatus()` · `Command(verb)`
- `StationStatus`: `busy`(IsCycleRunning) · `text`(CurrentStatusText) · `eStop`(HasEStop) · `lastEvent`(LastSafetyEvent) · `fault` · `progress`(0..1)

셸 사용: Registry 열거 → `controlPrefab` 인스턴스화 → `Enter/Exit` → `GetStatus()` 폴링 → `Command(verb)`.
verb 예) 로봇: `StartCycle`/`StopCycle`/`ResetEStop`/`Home`, 얼라이너: `Align`/`Stop`.
