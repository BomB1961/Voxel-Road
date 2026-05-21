---
name: Voxel Road 진행 상황·다음 작업
description: 다른 PC에서 이어서 작업할 때 참고할 현재 구현 상태와 우선순위 작업 목록
type: project
originSessionId: a0be4e0f-d583-45ea-99f0-a432b4c2a177
---
최신 커밋: `57baca9` (main, 2026-05-04) — Step 0~11 + 모듈화 3종 + 회귀 검증 모두 완료. **사용자 정의 스코프(Crossy Road 모작 프로토타입, 정상 구동·무버그) 기준 코어 완성**. 잔여 = **포트폴리오용 WebGL 빌드 + GitHub Pages 호스팅 진행 중**(2026-05-04 세션 중단, 2026-05-05 이어 진행).

## 2026-05-04 세션 중단점 (내일 이어 진행)
- **Android 빌드 폐기**: 사용자가 포트폴리오용·상업 출시 X 명확화. Android 실기 검증은 의미 없어 스킵 결정.
- **호스팅 방식 결정**: **방법 3 (Voxel-Road repo의 `gh-pages` 브랜치)**. URL = `https://bomb1961.github.io/Voxel-Road/`. 사이트 본체(`BomB1961.github.io`) repo는 안 건드림. 사이트 본체에 게임 페이지 링크/iframe 추가는 사용자가 별도 작업.
- **`57baca9` 커밋**: Android applicationIdentifier 를 `com.bomb1961.voxelroad` 로 변경. 포트폴리오엔 의미 없지만 미래 가능성 위해 유지(원복 안 함). push 완료.
- **현재 단계 = WebGL Switch Platform 진행 중**: `EditorUserBuildSettings.SwitchActiveBuildTargetAsync(WebGL)` 호출(2026-05-04 17:26 시작). 30분 폴링 timeout 도달했지만 실패가 아니라 단지 폴링 한도. Unity 30분간 응답 없음 = 재임포트 진행 중일 가능성 높음. **내일 Unity 에디터 켜고 우하단 진행률 바 확인 필수**:
  - Switch 완료(activeBuildTarget=WebGL) → Player Settings → 빌드 단계로
  - Switch 미완 → 추가 대기 또는 동기 방식 재시도
- **남은 단계 (재개 시 순서)**:
  1. `activeBuildTarget == WebGL` 확인
  2. WebGL Player Settings: `PlayerSettings.WebGL.compressionFormat = Gzip`, `memorySize = 256`
  3. Scenes In Build에 `Assets/Scenes/Game.unity` 등록 확인
  4. `BuildPipeline.BuildPlayer` 로 Release 빌드 (20~40분 첫 빌드, IL2CPP→WASM)
  5. Voxel-Road repo에 `gh-pages` orphan 브랜치 생성 → 산출물 push
  6. GitHub API로 Pages 활성화 (source = gh-pages branch)
  7. URL 사용자에게 전달, 회귀 항목 확인
  8. main 브랜치 working state 정리 (gh-pages 작업 후 main 복귀)
- **InputReader 정보**: 키보드(WASD/방향키) + 터치(EnhancedTouch swipe·tap) 모두 처리. PC 브라우저에서 키보드로 정상 플레이 가능.

**Why:** 다른 PC에서 이어서 작업할 수 있도록 현재까지 합의된 방향과 남은 일정을 보존.
**How to apply:** 새 세션 시작 시 git pull 후 아래 우선순위대로 진행. 세부 구현은 코드가 진실 — 이 메모는 방향·스펙만.

## 현재 구현된 기능 (Step 0~9 완료)
- 플레이어 그리드 이동·점프 연출(0.12초 사인 아크), 입력 1단계 큐, 차량 충돌/익사 사망
- 청크 기반 절차적 레인 생성 (Grass/Road/River/Rail), Quota-Deck 알고리즘, 안전 시작 구간
- 도로 차선 점선 (레인 경계·청크 경계 인식), 6종 차량 양방향 주행
- 차량·통나무·기차 스포너: 레인 속도 고정, 스폰마다 프리팹 랜덤, Prewarm 패턴
- 통나무 탑승: 그리드 3칸 폭 탑승 판정, TryBoardLog + OnTriggerEnter 이중 검사
- 통나무 위 좌우 점프: log-relative 좌표 매 프레임 보간 (드리프트 자동 추적)
- 통나무 출발 전진 점프: 월드 X 고정 (`to.x = from.x`) — 대각선·드리프트 영향 제거
- 지면 착지 후 X 그리드 정수 재스냅 (드리프트 누적 방지)
- BlobShadow (Sprites/Default 원형 그림자) — 모든 차량·통나무 적용
- GroundFill (대형 평면, y=-0.1) — 레인 경계 사이 배경 깜빡임 방지
- 맵 경계(halfSpan)에서 통나무 자동 하차·익사
- 기차 레인: URP 경고등(빨강), 즉시 스폰, 다차량 편성, 정해진 빈도로 통과
- 카메라: Cinemachine 3.1.6 PostPipelineStage Extension — 전진 전용 Z 추적(deadzone=5레인, **자동 전진 OFF**), 좌우 댐핑(`Lerp × dt×8`, 빠른 추적), X 클램프(`MapXLimit`), Y 고정
- `MapXLimit` static 노출 → 카메라 가시범위 = 플레이어 이동 가능 범위 항상 일치
- **ChasingWall 비활성화**: Step 7 재설계 산출물이지만 단조로운 압박이라 폐기. 씬에서 SetActive(false). 코드는 보존(향후 재활용 옵션).
- **Idle Death** (Step 9 부산물): 마지막 입력 시점부터 5초 누적 시 사망. 좌우 무빙도 reset 인정 (정당한 통나무·자동차 빈틈 대기 회피 행동).
- **후퇴 5그리드 제한**: MaxZ - 5 이내까지만 후퇴 허용. 카메라 deadzone과 일치 → 시야 범위 = 후퇴 범위. 시작 z=0 뒤로는 절대 이동 불가 (`target.Z < 0` 차단).
- **사망 애니메이션** (`PlayerDeathAnimator.cs`, 2026-04-29):
  - Vehicle/Train: PlayerController.LastImpactSource로 차량 Transform 캡처 후 분기
    - 플레이어가 정면(+Z) 상태 (`|forward.z| ≥ 0.7`): 월드 Z축 ±90° 회전 → 머리가 차량 진행 방향으로 옆으로 쓰러짐
    - 회전 상태 (±X 등): local right axis 90° → 자기 forward 방향으로 앞으로 쓰러짐
    - 공통: 차량 부착 안 함, y=0 흡착, 진행 방향 0.5유닛 밀려남, 스케일 X1.2/Y0.5/Z1.2
  - Drown: y -2.5 하강 + 회전 (1.5s), 머터리얼 페이드는 옵션
  - OutOfBounds: 앞으로 쓰러짐 (X+90°, 0.4s)
  - Idle: 뒤로 쓰러짐 (X-90°, 0.4s)
  - Animator/Clip 미사용, transform 조작만
- **Step 8 UI** (점수판·게임오버):
  - VoxelRoad.UI 네임스페이스: ScoreTracker, GameHUD, GameOverPanel, **PerDigitScoreDisplay** (2026-04-29)
  - HUD: **좌상단 SCORE 카드** — 어두운 9-slice 백패널 + 골드 좌측 스트라이프 + Outline 머터리얼 (Kenney Mini Square + LiberationSans SDF fallback)
  - HUD 구조: `[Stripe | "SCORE" 라벨(정적) | Digit0~Digit4 5개 TMP]` HorizontalLayoutGroup, ContentSizeFitter
  - **자리별 Pop 효과**: 점수 갱신 시 변경된 자리만 sin curve 펑 (peakScale 1.2, 0.15s). char 버퍼로 GC-free
  - HI 표시 제거 → SCORE 단일 표시. 이전 BestScore 첫 추월 시 `Best Record` 배너(노란색, 흰색 별 데코, fade-in/out) 1회
  - 별 글리프(★)는 Kenney·LiberationSans 모두 미보유 → `Assets/Art/UI/star_white.png` UI Image로 분리 처리
  - GameOverPanel: GAME OVER 깜빡임 + 최종 점수 + 신기록(NEW RECORD!) + TAP TO RESTART 펄스 + **사망 모션 후 표시 지연** (Vehicle/Train 0.5s, Drown 1.5s, FallOver 0.5s)
  - UI 효과: UIPulse(사인 알파), UIFlicker(주기 토글), UIScorePop (현재 비활성 — 자리별 Pop으로 대체)
  - Best Score PlayerPrefs 영속화
  - Canvas Scaler: 모바일 세로 1080×1920
  - 빌더 `Tools/Voxel Road/Build HUD Score Card` (HudCardBuilder.cs) — 멱등 재생성
- **Step 9 청크 난이도 시스템** (`BalanceCurve.cs`):
  - 거리 곡선 × 청크 단위 무작위 variance 곱 → 청크별 multiplier
  - DistanceBase: MaxZ 50/150/300 임계로 1.0→1.3→1.6 단계 증가 후 plateau
  - Variance: Easy×0.7 / Normal×1.0 / Hard×1.3 (확률 30/50/20)
  - 최종 multiplier `Clamp(0.7, 1.6)` — 인간 반응 한계 보호
  - 적용 대상: VehicleSpawner / LogSpawner / TrainSpawner의 speed×mul, interval/mul
  - 청크 내 모든 lane이 같은 multiplier 공유(일관성)
  - 절대 가드: 기차 속도 25 m/s 상한, 기차 cycle 2.5초 하한

## 합의된 튜닝 값 (기준점)
- 자동차 속도 5m/s 단일 (`VehicleConfig.asset`) — 청크 multiplier로 변동
- 통나무 속도 2~3.5m/s 레인별 랜덤 (`LogConfig.asset`, 2026-04-28 회복 — 0.5배 줄였다가 다시 상향)
- 스폰 간격 Vehicle 0.75~1.75초 / Log 1.5~2.5초 (Log 빈도↑)
- Prewarm: Vehicle 레인당 6개 / Log 레인당 **8개** (구 4 → 듬성듬성 해소)
- SpawnScale Vehicle 0.6 / Log 0.85
- LengthScale Log 4.5248868 → 월드 X = 3.0 유닛 정확 (3그리드)
- WidthScale Log 3.5 → 월드 Z ≈ 0.77 유닛
- Log BoxCollider size.x = 0.78 (메시 크기 기준)
- Idle Death 타임아웃: 5초 (PlayerController._idleTimeoutSeconds)
- 후퇴 한계: MaxZ - 5 (PlayerController._backwardLimitGrids)
- 카메라 좌우 댐핑: 8 (구 1.5 — 5배 빠른 추적)
- 카메라 forward deadzone: 5 (구 4 — 후퇴 한계와 일치)
- 카메라 자동 전진: **0 m/s** (시간 압박 OFF)
- 카메라 pitch: **65°** (2026-04-29, 구 60°), orthoSize **6** (구 5), followOffset **(0, 22, -4)** (구 (0, 20, -6)) — 시작 시 플레이어가 화면 하단에 위치, 진행 방향 가시 거리 확장
- LaneConfig: GrassQuota 3 (구 2), RiverChunk (3,5) (구 (2,3))

## 핵심 버그 수정 이력 (재발 방지)
- `Log.Launch()` BoxCollider 탑승 판정: `col.bounds.extents.x` 대신 `col.size.x * transform.lossyScale.x * 0.5f` 사용 (스폰 직후 bounds 미반영 Unity 타이밍 버그)
- 통나무 출발 대각선 점프: `prevParent != null` + `dx == 0` 일 때 `to.x = from.x`로 Z축 직선 고정
- Log BoxCollider size.z = 0.26 (구 0.9 → 레인 넘침 방지)
- 통나무 위 `_gridPos.X` 추적: `RoundToInt(x - 0.5f)` = 뱅커 반올림으로 홀수 그리드(1,3,5,-1,-3) 정수 위치에서 1칸 오차 → `RoundToInt(x)` 로 수정
- 통나무 위 좌우 이동: 사전 `VelocityX * _moveDuration` 보정은 코루틴 정수 프레임 오차로 실제 드리프트와 어긋남 → log-relative 좌표로 매 프레임 `prevParent.position.x + Lerp(fromRel, toRel, t)` 보간
- 전진 점프(dx==0)는 출발 통나무 드리프트를 추적하지 말 것 — 다른 레인으로 건너가는 의미이므로 월드 X 고정
- 지면 착지 후 X 정수 재스냅 (드리프트 누적 방지)

## 미해결 이슈
- (비어있음 — 2026-04-30 Step 11 부분 완료 후)

## 2026-04-30 작업 요약 (Step 11: 아트 폴리시)
- **GameOverPanel REPLAY/QUIT 강화** (`a5c11f4`): 380×160 (+12/+14%), fontSize 64→72, fontStyle Bold(1)로 SDF 다일레이트 굵기 활성화, 위치 ±200 (간격 20px 유지)
- **Rail 침목 + River 연잎 정적 데코** (`3b1356b`):
  - `M_Sleeper.mat` (#4D331F), `M_LilyPad.mat` (#478F40) 신규
  - Rail: 셀당 갈색 침목 1개(0.7×0.08×0.85)
  - River: 셀당 z=±0.32 두 위치 25% 확률, 0.34×0.02×0.26 (1.31 비율 — 정사각 인상). 회전 미적용(90° 회전 시 X 스케일이 Z로 돌아 인접 청크 침범)
- **Road 연석** (`2393a54`): `M_Curb.mat` (#9F9988). 청크 바깥 경계만(`!_drawBackEdge`/`!_drawFrontEdge`), 셀당 1m×0.10×0.08, z=±0.45. **점선 마커는 유지**(사용자 결정 — 도로 청크 응집감보다 차선 가독성 선호)
- **Grass/Road 셀 단위 컬러 패치** (`dc187f9`): MaterialPropertyBlock으로 베이스 컬러 ±0.08 RGB 지터를 셀별 오버라이드. 머티리얼 인스턴스 생성 안 해 GC 부담 0
- 외부 에셋 미사용 — 모두 procedural primitive(Cube). 향후 Kenney CC0 모델 교체 가능
- 데코·패치는 `_decorRoot`/_ground/_water/_track 트랜스폼 자식으로 SetParent → Despawn 시 자동 해제

## 2026-04-30 핵심 버그 수정 (재발 방지)
- **MaterialPropertyBlock 정적 필드 초기자 금지**: `private static readonly MaterialPropertyBlock PatchMpb = new();` 처럼 정적 필드 초기자에서 MPB를 생성하면 native CreateImpl이 MonoBehaviour 타입 로딩 컨텍스트에서 거부됨 → 타입 deserialize 실패 → 해당 프리팹의 컴포넌트가 LaneConfig 같은 외부 참조에서 런타임 null로 보임 → "[WorldGenerator] prefab 없음" 캐스케이드. **첫 사용 시 lazy 생성 패턴 필수** (`if (_mpb == null) _mpb = new MaterialPropertyBlock();`). Shader.PropertyToID는 정적 필드 초기자에서 OK (managed only).

## 2026-04-29 작업 요약
- **HUD 가독성 개편**: SCORE 카드(어두운 9-slice + 골드 스트라이프) + 신기록 배너 `Best Record` (별 데코레이션은 UI Image 분리)
- **자리별 점수 Pop**: PerDigitScoreDisplay — 변경된 자리만 펑 (5자리 분리 TMP)
- **카메라 프레이밍**: pitch 65°, orthoSize 6, followOffset (0,22,-4) — 시작 시 플레이어 화면 하단
- **사망 애니메이션**: PlayerDeathAnimator + GameOverPanel 모션 후 지연 표시
- **TestMode 컴포넌트**: BestScore 시드, 청크 강제 Grass (테스트 종료 후 제거 의무 — `feedback_test_mode_cleanup.md`)
- **버그**: GameHUD ScoreFormat `{0:D5}` → `{0:00000}` (TMP SetText는 C# string.Format과 다른 포매터)
- 커밋 9개 (`bd72310 → 932e768`), unity-cli v0.3.13 → v0.3.15

## 해결된 이슈 (재발 방지)
- **플레이어 좌우 이동이 맵 경계 전에 막힘 (2026-04-23)**: `HandleMoveInput`이 카메라 클램프 값(`MapXLimit ≈ 19`)을 플레이어 한계로 잘못 사용 → 실제 맵 경계(`LaneHalfSpan = 25`)로 변경. 카메라·플레이어 한계는 의미가 다름을 구분.
- **통나무 탑승 후 좌우 남는 간격 불균등 (2026-04-23)**: 점프 착지가 while 루프 1프레임 초과 종료로 인해 `vx × dt` 만큼 통나무만 더 흘러 상대 오프셋에 드리프트 고정 → `Log.SnapToSurface`에 X 정수 슬롯 스냅 추가 + `PlayerController.MoveRoutine` `fromRelX`를 `Mathf.Round`로 재스냅. 타이밍 오차를 이산화로 흡수.
- **Step 7 독수리 폐기 (2026-04-27)**: CC0 독수리 에셋 부재 + 시각적 표현 없는 추격자는 몰입 약화. 자동 전진 카메라(시간 누적)와 ChasingWall(화면 하단 따라가는 흰 벽)로 대체. 카메라가 시간 기반으로 자동 전진하므로 플레이어 정지 시 자연스럽게 압박.
- **Step 7 ChasingWall + 자동 전진 폐기 (2026-04-28)**: 통나무 속도와의 균형 문제(통나무 1.5m/s ≈ 자동 전진 1.5m/s → 강 위 정체) + 시각적 단조로움 → 폐기. 대신 **Idle Death + 후퇴 5그리드 제한**으로 대체. 통나무 좌우 끝 익사가 자연스러운 압박 역할.
- **Idle reset 기준 결정 (2026-04-28)**: 초기에 MaxZ 갱신 기준으로 했으나 사용자 통찰 — 통나무 스폰 대기·자동차 빈틈 대기는 정당한 회피 행동. 좌우 무빙 reset 허용으로 변경. 무한 후퇴는 5그리드 제한으로 별도 처리.
- **위험→Grass 강제 폐기 (2026-04-28)**: 강 끝나자마자 자동차 같은 연속 위험 차단을 위해 도입했으나 단조로움. 사용자 통찰 — 청크별 무작위 다양성이 지루함 방지의 핵심 → 강제 제거. 대신 Step 9 청크 난이도 시스템으로 동적 변화 부여.

## 디버깅 도구
- **unity-cli v0.3.15** (2026-04-29 업그레이드, `com.youngwoocho02.unity-cli-connector@v0.3.15`): 에디터 외부에서 콘솔·런타임 조회. 토큰 절약 핵심 도구.
  - `unity-cli status` — 연결된 프로젝트·포트·PID 확인
  - `unity-cli console --type error,warning --lines 20` — 에러/경고만 N줄
  - `echo 'return ...;' | unity-cli exec` — C# 런타임 동적 실행 (stdin 파이프 필수, PowerShell 5.1 stderr 함정 회피)
  - `unity-cli manage_editor --action play|stop --wait_for_completion true` — Play 사이클 자동화
  - `unity-cli screenshot` / `profiler` / `menu` / `run_tests` 등
- 핫패스 `Debug.Log`는 토큰·로그 노이즈 누적 위험 — 디버깅 끝나면 제거하거나 `#if DEBUG_VERBOSE` 가드 필수.

## 남은 우선순위 작업
- **모듈화 리팩토링 완료** (2026-05-04). 잔여 항목은 사용자 결정 사항(추가 폴리시·신규 기능). `project_modularity_status.md` 참조.

## 2026-05-04 작업 요약 (모듈화 리팩토링)
- **Player 모듈 분리 (`0cc848e`)**: PlayerController 328줄 비대 해소 → `PlayerMovement`/`PlayerLogRider`/`PlayerDeathTriggers` 3종 자식 컴포넌트 추출. PlayerController 는 그리드 상태·생명주기·이벤트 라우팅만 담당. `_playableHalfSpan = 23` SerializeField로 카메라 한계와 분리.
- **World 통행성 보강 (`0aaf3fb`/`afc3fe0`)**: cross-lane 2-스택 제거 + BFS 사후 보정 + 고립 빈 셀·대각선 z-stack 방지로 Grass 통행성 보장.
- **통나무 OOB 익사 카메라 시야 기준 (`582d4e3`)**: 통나무 끝 익사 판정을 카메라 시야 끝 기준으로 처리.
- **Spawner 베이스 추출 (`5ab1849`)**: VehicleSpawner·LogSpawner 공통 베이스 `PeriodicLaneSpawnerBase<TConfig>` 신규 (Common 네임스페이스). Initialize/Update/Prewarm + 6종 추상 hook(`HasPrefabs`/`PrewarmCount`/`FirstSpawnDelayMax`/`SpawnIntervalRange`/`ResolveBaseSpeed`/`SpawnAt`). VehicleSpawner 91→41줄, LogSpawner 92→43줄. **TrainSpawner 는 상태머신·다차량 편성으로 본질이 달라 미통합** (베이스에 dummy hooks 끼우면 추상이 약화됨 — 사용자와 합의된 결정).
- **Log 탑승 진입점 통일 (`5ffce42`)**: `Log.TryAttachPassenger(Transform)` 공개 메서드로 SetParent + SnapToSurface + `_passenger` 등록 일괄 처리. OnTriggerEnter(트리거 진입)와 PlayerLogRider.TryBoardLog(착지 능동 검사) 두 진입점이 모두 이걸 호출. **부수 효과: TryBoardLog 경로 탑승 시 `_passenger` 미등록으로 통나무 OOB destroy 때 unparent 누락되던 잠재 버그 해소.**
- **머터리얼 부동소수점 노이즈 안정화 (`d1fd7b1`)**: M_LilyPad/M_Sleeper `_Color` round-trip 노이즈(0.28→0.27999997 등) 정확한 표현으로 한 번 고정. 시각적 영향 0.

## 2026-05-04 작업 요약 (Step 10·11 잔여 완료)
- **Step 11 URP 라이팅 톤업 (`b7b8a8b`)**:
  - Directional Light 시원한 톤 #E8F0FF, pitch 40°, ShadowStrength 0.4 (약하게), Soft Shadows
  - Environment Lighting Gradient(Sky #B8D8F0 / Equator #D8E0E8 / Ground #707880), Intensity 0.75, Bounce 0.5
  - SampleSceneProfile: Bloom·Vignette OFF, Tonemapping=Neutral, ColorAdjustments(+5 sat / +3 contrast / 청색 필터 #EBF5FF) 추가
  - URP RP Asset(PC·Mobile): SoftShadows ON, ShadowDistance 30, Cascade 2
  - M_Asphalt #595A5F → #6B6B70 (그림자 콘트라스트 보강 시도)
  - 차량·통나무 그림자는 다크 아스팔트 위에서 식별 어려움 — 의도적으로 약한 그림자 유지(사용자 결정)
- **Step 10 오디오 (`3294efb`)**:
  - `VoxelRoad.Audio.AudioManager` + `SoundConfigSO` (Assets/Scripts/Audio/, Assets/Data/SoundConfig.asset)
  - 씬 GameObject `AudioManager` + 자식 BGM_Source/SFX_Source AudioSource
  - InputReader.OnMoveInput 구독 → 점프 SFX (피치 ±5% 랜덤). GameManager.OnPlayerDied 구독 → 사망 5종 분기 SFX + 0.5s BGM 페이드
  - 음원 전부 Kenney CC0: Music Jingles(NES01 BGM·HIT00 OOB) + Interface Sounds(click_005 점프·switch_005 Idle) + Impact Sounds(impactMetal_heavy_002 차량·impactSoft_heavy_001 익사·impactPlate_heavy_002 기차)
  - 기존 코드 수정 없음 — 이미 발화하던 이벤트만 신규 구독
- **신규 PC 셋업 체크리스트 메모리 추가**(`abe12eb`): Android 빌드 타깃 + Game 뷰 1080×1920 프리셋 — `feedback_new_pc_setup.md`
- **ProjectAuditor 베이스라인 추가 (`3015296`)**: Unity 6 자동 생성 파일 git 추적 시작

## 구조·컨벤션 메모
- 스크립트 경로: `Assets/Scripts/{Player,World,Game,Input,Data,Camera,Common,Vehicles,River,Rail,UI}/`
- SO 설정: `Assets/Data/*.asset`
- 한글 Conventional Commits, main 강제 푸시 금지, `*.csproj`·`*.slnx` 금칙
- 레인은 정수 그리드 X (플레이어 `GridPosition.ToWorldPosition` 규약)
- `WorldGenerator.LaneHalfSpan`이 좌우 경계, 입력·통나무 모두 이 값 기준
- 카메라 `MapXLimit` static — 카메라 가시범위 = 플레이어 이동 가능 범위 동기화
- `DeathReason` enum: Vehicle, Drown, OutOfBounds, Train, **Idle** (Eagle 제거됨)
- `ChunkDifficulty` enum (`VoxelRoad.Game.BalanceCurve`): Easy/Normal/Hard
- 폰트: Kenney Mini Square (CC0) `Assets/Fonts/`
- TextMeshPro 자산: `Assets/TextMesh Pro/` (TMP Essential Resources)
