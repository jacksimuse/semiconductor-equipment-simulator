# 캐릭터 제작 파이프라인 — 경로 ② (이미지→3D AI + 오토리깅)

> 목표: ChatGPT 컨셉 시트 → **리깅·애니메이션된 Unity Humanoid 플레이어 캐릭터**.
> 로드맵상 **E2(탐험 모드)의 플레이어 에셋 생산**.
>
> 방진복 오퍼레이터는 얼굴·손·머리카락이 마스크/장갑/후드로 가려져 있고
> 매끈한 흰 커버올이라 **이미지→3D 생성에 유리**하다.

---

## 역할 분담

| 단계 | 어디서 | 누가 |
|------|--------|------|
| 1. 이미지 준비 | ChatGPT / 이미지 편집 | **직접** |
| 2. 이미지→3D 메시 | Meshy / Tripo / Rodin (웹) | **직접** |
| 3. 메시 정리 | Blender (선택) | **직접** |
| 4. 오토리깅 | Mixamo (웹) | **직접** |
| 5~7. Unity 임포트·Humanoid·Animator·PlayerController | Unity | **MCP 세션에서 Claude가** |

---

## Stage 1 — 이미지 준비 ★가장 중요한 팁

**현재 시트의 정면 포즈는 팔이 몸에 붙어 있어 오토리깅에 불리**하다(팔·몸통이 붙으면 리깅 실패).

→ ChatGPT로 **오토리깅 전용 정면 이미지**를 새로 뽑아라:
> "전신 정면, **A-포즈(팔을 몸에서 30° 벌림)**, 정투영(orthographic),
>  단색/무배경, 방진복 클린룸 오퍼레이터, 얼굴 마스크·후드 착용"

- 배경 제거(단색), 피사체 하나만, 전신이 잘리지 않게.
- (옵션) 앞/뒤 2장 지원 툴이면 뒷모습도 A-포즈로 준비.

## Stage 2 — 이미지→3D 메시 생성

- 툴: **Meshy.ai**(무료 티어, Image to 3D) · **Tripo3D** · **Rodin(Hyper3D)** · Kaedim(유료, 토폴로지 깔끔).
- 입력: Stage 1의 A-포즈 정면(툴에 따라 앞+뒤).
- 출력: **FBX 또는 GLB**(텍스처 포함)로 다운로드.
- 예상 이슈: 토폴로지 지저분, 팔·몸통 융합, 대칭 아티팩트, 텍스처가 단일 베이크.
  → 방진복은 디테일이 적어 상대적으로 양호.

## Stage 3 — 메시 정리 (Blender, 대개 필요)

- **스케일 확인: 키 ≈ 1.7m** (Unity 1 unit = 1m). Apply Transforms.
- 단일 메시, 팔이 몸통에서 분리됐는지 확인(리깅 필수 조건).
- 폴리곤 과다 시 Decimate로 감소(1만~3만 tris 목표).
- FBX로 재익스포트.

## Stage 4 — 오토리깅 (Mixamo, 무료)

1. mixamo.com 로그인(Adobe) → Upload Character (FBX).
2. 마커 배치: 턱·양 손목·양 팔꿈치·양 무릎·사타구니 → Auto-Rig.
3. 걷기 프리뷰 확인.
4. 다운로드:
   - **캐릭터 본체**: FBX, With Skin, T-pose.
   - **애니메이션**: Idle / Walk / Run 검색 → 각각 **Without Skin, In Place** FBX.
     (In Place = 제자리 애니 → 이동은 코드로 처리)

## Stage 5~7 — Unity 작업 (MCP 세션에서 Claude가 처리)

**5. 임포트 & Humanoid**
- 본체 FBX: Rig → Animation Type = **Humanoid**, Avatar = **Create From This Model** → Apply.
- 애니 FBX들: Rig → Humanoid, Avatar = **Copy From Other Avatar**(본체 아바타) → Mixamo 애니 리타깃.
- 머티리얼: URP/Lit로 변환(베이크 알베도 연결). 노멀 없으면 평평하지만 교육용 OK.

**6. Animator Controller (블렌드 트리)**
- float 파라미터 `Speed` 1D 블렌드: 0=Idle, ~2=Walk, ~5=Run.

**7. PlayerController 연결 (E2)**
- Input System 이동 + CharacterController. 이동 속력 → `animator.SetFloat("Speed", v)`.
- In-Place 애니 + 코드 이동(루트모션 off)이 제어 가장 단순.

## Stage 8 — 확장 (나중에)
- **모듈 On/Off**(후드·마스크·보안경): 부위별 GameObject/SkinnedMeshRenderer 토글.
- **색상(화이트/블루/그레이)**: 방진복 머티리얼 `_BaseColor` 스와핑.
- **특수 애니**(패널 조작·점검·가리키기): E3/E4에서 트리거하는 별도 클립.
- **LODGroup**: 규모 커지면 LOD0/1/2.

---

## 완료 기준
탐험 씬에서 방향키로 캐릭터가 **Idle↔Walk↔Run 자연 전환하며 이동**.
→ 이후 E3(상호작용)·E4(스테이션 진입)로 연결.

## 주의
- 시트의 "18,000 tris / 2048 텍스처 / LOD" 라벨은 AI가 그린 것 → **실제 스펙은 생성 결과에 따름**.
- "PBR 맵 예시" 이미지는 목업 → **실제 맵은 생성 툴 출력 텍스처를 사용**.
