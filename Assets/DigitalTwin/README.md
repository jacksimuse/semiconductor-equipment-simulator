# Digital Twin — 6축 로봇 시뮬레이터 (Phase 0-1)

반도체 장비 교육용 6축 로봇 디지털 트윈의 시작점.
**FK(순기구학) + 조인트 조그** 까지 구현된 스캐폴드입니다.

## 사용법
1. Unity 에디터에서 스크립트 컴파일이 끝날 때까지 대기.
2. 상단 메뉴 **Tools ▸ Digital Twin ▸ Build 6-Axis Robot** 클릭.
   → 씬에 `SixAxisRobot` 오브젝트(J1~J6 + TCP)가 생성됩니다.
3. **▶ Play** 를 누르면 좌상단에 **조그 슬라이더 6개 + TCP 좌표**가 표시됩니다.
   - 슬라이더로 각 축을 회전 → 홈 자세는 팔이 수직.
   - J1/J4/J6 = 롤(Y축), J2/J3/J5 = 피치(Z축).
   - `Home` 버튼으로 전부 0°.

## 구조
```
Assets/DigitalTwin/Scripts/
├─ Runtime/
│  ├─ SixAxisRobot.cs   # 관절 데이터 + FK 적용 (kinematic), TCP 기즈모
│  ├─ RobotIK.cs        # CCD 위치 IK 솔버 (드래그 타깃 추적)
│  ├─ RobotTeach.cs     # 티칭 & 재생 (웨이포인트 보간, JSON 저장)
│  ├─ WaferScenario.cs  # FOUP↔챔버 픽/플레이스 + 웨이퍼 매핑
│  └─ RobotJogUI.cs     # IMGUI 조그 패널 + IK/티칭/시나리오 UI (배선 불필요)
└─ Editor/
   └─ RobotBuilder.cs   # 메뉴 클릭으로 로봇 계층 코드 생성
```

## 다음 단계 (로드맵)
- **Phase 2 ✅**: IK(역기구학) — CCD 수치해, 드래그 타깃 위치 추적 (`RobotIK.cs`). 자세(방향) 추적은 이후 확장.
- **Phase 3 ✅**: 티칭 & 재생 — 웨이포인트 저장/삭제, SmoothStep 궤적 보간, 반복, JSON 저장·불러오기 (`RobotTeach.cs`)
- **Phase 4 ✅**: 반도체 시나리오 — FOUP(5슬롯)↔챔버 웨이퍼 픽/플레이스 자동 사이클, 웨이퍼 매핑 (`WaferScenario.cs`, 메뉴 `Build Wafer Scenario`)
- **Phase 5**: 디지털 트윈 통신 — OPC-UA / Modbus / SECS-GEM 양방향 동기화
- **Phase 6**: HMI 대시보드, 충돌감지, 세이프티존, 사이클타임 로깅

## 참고
- 현재는 URDF 대신 코드로 생성한 **제네릭 6축 팔**. 관절마다 모터 하우징(축 방향 배럴)
  + 관절 구로 링크가 매끄럽게 이어지도록 스타일링됨(`RobotBuilder.MakeJoint`).
  실기 정확도가 필요하면 URDF Importer(`com.unity.robotics.urdf-importer`)로
  UR5 등을 임포트해 교체 권장(외부 에셋·ArticulationBody 재배선 필요).
- 물리(ArticulationBody)가 필요한 단계(충돌/그리퍼 힘)부터는 kinematic → 물리 전환.
