# Voxel Road — 작업 진행 기록

> 터미널 재시작 후에도 어디까지 작업했고 어디서부터 시작할지 파악하기 위한 로그.
> 각 Step 완료 시 이 파일을 갱신한다.

## 프로젝트 개요
- Crossy Road 모작 (모바일 3D 러너)
- Unity 6000.4.3f1, URP, New Input System, Cinemachine 3.1.6
- 타깃: Android (Portrait, 1080x1920), 추후 iOS 확장 가능
- 비주얼: CC0 복셀 에셋 (Kenney Voxel Pack 권장)
- 기획서: `C:\Users\admin\.claude\plans\https-play-google-com-store-apps-details-parallel-locket.md`

---

## 진행 상황

### Step 0 — 스캐폴딩 & 모바일 설정 ✅ (완료, 2026-04-20)
- [x] 폴더 구조 생성 (`Assets/Scripts/{Player,World,Game,Input,Data,Camera,Common}/`, `Assets/Prefabs/`, `Assets/Materials/`, `Assets/ThirdParty/`)
- [x] Android Build Target 전환 (`SwitchActiveBuildTarget(Android)`)
- [x] Player Settings: Default Orientation = Portrait
- [x] Game View 해상도 프리셋 `Mobile Portrait 1080x1920` 추가
- [x] `Assets/Scenes/Game.unity` 생성 (Main Camera + Directional Light, Build Settings 등록)
- [x] Play 실행, 게임 코드 에러 0건 확인
- [x] `Memory.md`, `Debugging.md`, `Assets/ThirdParty/README.md` 생성
- [ ] **(사용자 작업)** Kenney Voxel Pack 다운로드 → `Assets/ThirdParty/KenneyVoxelPack/`

### Step 1 — 스와이프/탭 입력 + 그리드 이동 ✅ (완료, 2026-04-20)
- [x] `Assets/Scripts/Common/GridPosition.cs` — readonly struct, ToWorldPosition(), Move()
- [x] `Assets/Scripts/Common/MoveDirection.cs` — Forward/Backward/Left/Right enum
- [x] `Assets/Scripts/Input/IInputReader.cs` — 이동 입력 인터페이스
- [x] `Assets/Scripts/Input/InputConfigSO.cs` — 스와이프 임계값 ScriptableObject
- [x] `Assets/Scripts/Input/InputReader.cs` — EnhancedTouch 스와이프 + 키보드 폴백
- [x] `Assets/Scripts/Player/PlayerController.cs` — 그리드 이동, 0.12s 점프 코루틴, 큐잉(1개)
- [x] `Assets/Data/InputConfig.asset` — InputConfigSO 에셋 생성
- [x] Game.unity 씬에 Player + GrassGround 배치
- [x] 컴파일 에러 0건, Play 테스트 통과
- **비고**: `create_script`의 검증기 오탐(duplicate method) → `execute_code` + `File.WriteAllText`로 우회

### Art Assets 통합 ✅ (Step 1과 병행, 2026-04-20)
모든 에셋 **CC0 라이선스**, 출처: Kenney.nl. 총 42MB.

| 팩 | 용도 | 주요 파일 |
|---|---|---|
| `blocky-characters_20/` | Player 캐릭터 (20종) | `character-a.fbx` ~ `character-t.fbx` |
| `car-kit/` | Road 레인 차량 | ambulance, sedan, truck, taxi, police, van 등 |
| `city-kit-roads/` | 도로 타일 | road-crossroad, road-bend, road-intersection 등 |
| `nature-kit/` | 환경 데코·River 통나무 | tree_default, rock_smallA, log, flower, plant_bush 등 |
| `platformer-kit/` | 백업 모델 | 블록·건물·캐릭터 |
| `mini-arcade/` | 배경 소품 | (선택적) |
| `ui-pack/PNG/` | Step 8 UGUI | 버튼·패널·슬라이더 PNG |
| `ui-pack/Font/` | Kenney 픽셀 폰트 | .ttf |
| `interface-sounds/Audio/` | Step 10 UI SFX | click, back 등 .ogg |
| `impact-sounds/Audio/` | Step 10 충돌·점프 SFX | footstep 등 .ogg |

- [x] 8개 ZIP `curl --parallel` 다운로드
- [x] 중복 포맷(OBJ/GLB/DAE/STL), 프리뷰, URL, SVG 정리 → FBX + PNG 텍스처 + License만 유지
- [x] `Assets/ThirdParty/README.md` 라이선스 출처 기록
- [x] `Assets/Prefabs/Player/Player.prefab` 저장 (character-a.fbx 기반, InputReader+PlayerController 포함)
- [x] Game.unity에 Player 교체 + Nature Kit 데코(트리·바위·꽃·부시) 14개 랜덤 배치
- [x] Play 모드 시각 확인: Crossy Road 감성 확보 (스크린샷 Assets/Screenshots/)

### Step 2 — 쿼터뷰 추적 카메라 ✅ (완료, 2026-04-20)
- [x] `Assets/Scripts/Camera/` — Cinemachine 3.1.6 **PostPipelineStage Extension**으로 Body 스테이지 가로채어 카메라 위치 직접 제어
- [x] 전진 시만 Z 추적: `effectivePlayerZ = Max(startZ, playerZ - deadzone)`, deadzone=4레인 (후퇴해도 effectivePlayerZ 불변)
- [x] 좌우 댐핑: `Lerp(pos.x, targetX, dt × 1.5f)` — 약간 딜레이로 무게감
- [x] X 클램프: `MapXLimit = mapHalfSpan - visibleHalfWidth` — 맵 끝 노출 방지
- [x] Y 고정: 플레이어 점프 아크가 카메라에 전이되지 않음
- [x] `MapXLimit` static 노출 → PlayerController 좌우 이동 경계 판정에 재사용 (카메라 가시범위 = 플레이어 이동 가능 범위 항상 일치)
- [x] Game View 커스텀 프리셋 `Mobile Portrait 1080x1920` (Android idx=18) 적용, Camera.aspect=0.563
- [x] Play 테스트: 전진 추적 ✓, 후진 시 maxZ 유지 ✓, X 클램프 ✓
- **설계 결정**: Cinemachine PostPipelineStage Extension 사용. 기존 파이프라인 교체 없이 Body 스테이지만 가로채어 전진 전용 추적·좌우 댐핑·X 클램프·Y 고정 구현

### Step 3 — Lane 시스템 + WorldGenerator ✅ (완료, 2026-04-20)
- [x] `LaneType` enum (Grass/Road/River/Rail), `ILane` 인터페이스, `BaseLane` 추상
- [x] `GrassLane` — 녹색 바닥 + Kenney nature-kit 데코(나무/바위/꽃/부시/버섯) 확률 배치, BlockedCells 반환
- [x] `RoadLane` — 아스팔트 바닥 + 중앙 점선 마커
- [x] `LaneConfigSO` — laneSpanX=16, lookahead=14, lookbehind=4, safeStart=6, grassDecorDensity=0.35, 가중치(0.45/0.55), 연속제한=3
- [x] `WorldGenerator` — Player Z 기반 선행 스폰·후행 디스폰, 시작 안전지대 Grass
- [x] Materials: `M_Grass`(연녹), `M_Asphalt`(진회색), `M_LaneMarker`(흰색)
- [x] Prefabs: `GrassLane.prefab`, `RoadLane.prefab`
- [x] `Assets/Data/LaneConfig.asset` — 10종 데코 참조, 프리팹·가중치 지정
- [x] Game.unity에 WorldRoot 추가, 기존 GrassGround/DecorRoot 제거
- [x] Play 테스트: Grass/Road 레인 교차 생성, 플레이어 전방 14칸 유지, Crossy Road 스타일 ✓
- **비고**: create_script 검증기 오탐 지속, float→int 암시 변환 에러는 Unity 콘솔에 표출 안됨. 반드시 `Assembly-CSharp` 어셈블리에서 타입 로드 검증 후 프리팹 생성 필요
- **설계**: River/Rail 레인은 Step 5/6에서 BaseLane 상속으로 확장, LaneType enum 이미 준비

### Step 4 — 차량 이동 + 충돌 사망 ✅ (완료, 2026-04-20)
- [x] `Vehicle.cs` — 속도·방향 주행, 레인 밖 이탈 시 자동 소멸, BoxCollider(trigger)로 Player OnTrigger 감지
- [x] `VehicleSpawner.cs` — RoadLane 자식, 간격·첫 지연 랜덤, 방향=짝수 zIndex +X / 홀수 -X
- [x] `VehicleConfigSO` — 차량 프리팹 6종(sedan/suv/taxi/truck/ambulance/police), 속도 2.5–5, 간격 1.5–3.5
- [x] `GameManager` — OnPlayerDied 정적 이벤트, IsAlive 상태, R키 씬 리로드
- [x] Vehicle 프리팹 6종 (Kenney car-kit FBX + BoxCollider + Vehicle)
- [x] RoadLane.prefab에 Spawner 자식 + VehicleConfig 와이어
- [x] Player 프리팹: Player 태그, BoxCollider(trigger), Kinematic Rigidbody 추가 (Trigger 검출용)
- [x] Scene에 GameManager 추가
- [x] PlayerController 사망 핸들링: InputReader 비활성, 코루틴 정지
- [x] Play 검증: 차량 자동 스폰 ✓, 양방향 주행 ✓, 충돌 시 IsAlive=False 전환 ✓
- **비고**: `Thread.Sleep` 메인 스레드 블로킹 → `Time.time` 정지. 검증은 `screenshot` 연속 호출로 프레임 진행시킴

### Step 5 — River + 통나무 + 익사/탑승 ✅ (완료, 2026-04-20)
- [x] `Assets/Scripts/River/LogConfigSO.cs` — 통나무 프리팹·속도·간격 설정
- [x] `Assets/Scripts/River/Log.cs` — 강물 흐름 이동 + OnTriggerEnter 시 Player를 parent로 탑승, Exit/Destroy 시 언패런트
- [x] `Assets/Scripts/River/LogSpawner.cs` — 레인 가장자리에서 통나무 주기 생성
- [x] `Assets/Scripts/World/RiverLane.cs` — 파란 물 바닥 + Spawner 호스팅, BaseLane 상속
- [x] `WorldGenerator` — `GetLaneTypeAt(z)` 공개 API, River 가중치 포함 3종 랜덤 (SerializeField 주입, 싱글턴 미사용)
- [x] `PlayerController` — 이동 시작 시 통나무 언패런트, 도착 후 River인데 parent 없으면 익사(`KillPlayer("drown")`), Update에서 탑승 중 worldX를 gridPos에 재동기화
- [x] `LaneConfigSO` — RiverLanePrefab + RiverWeight(0.25) 추가, Grass 0.4 / Road 0.35 재조정
- [x] `Assets/Materials/M_Water.mat` (파란색 URP Lit)
- [x] `Assets/Prefabs/Logs/Log_Small|Large|Stack|StackLarge.prefab` (Kenney nature-kit log 4종 + Trigger BoxCollider + Log 컴포넌트, Y 90° 회전으로 X축 정렬)
- [x] `Assets/Prefabs/Lanes/RiverLane.prefab` (Water Plane + Spawner 자식)
- [x] `Assets/Data/LogConfig.asset` (4개 통나무 프리팹 참조, 속도 1.5~3, 간격 2~3.5)
- [x] Play 검증: 2개 River 레인 자동 생성(z=6, z=17), 통나무 양방향 스폰·이동 ✓, Player 탑승 시 parent=Log(Clone) 및 x 좌표 함께 이동 ✓, 통나무 없는 River 셀 도착 시 즉시 익사 전환 ✓
- **주의**: Play 모드 중 execute_code 호출 사이에 에디터가 자동 일시정지되는 현상. `UnityEditor.EditorApplication.isPaused=false` 호출 + Time.timeScale=8로 프레임 진행 강제 필요. batch_execute로 screenshot을 연속 호출하면 프레임이 진행됨.
- **설계 결정**: 통나무 탑승을 parent transform로 처리 → Player 이동 시 언패런트, Log 소멸 시 OnDestroy에서 parent 해제 → 다음 프레임 CheckRiverArrival가 익사 판정
- **향후 확장**: River에 연꽃잎(lily pad) 블로킹 셀 추가 시 GrassLane의 BlockedCells 패턴 재사용 가능

### 스케일 비례 조정 ✅ (2026-04-20)
- [x] Player 자식 모델 0.6배 축소, BoxCollider size=(0.45, 0.6, 0.45) 조정
- [x] Vehicle 6종 자식 모델 1.4배 확대, BoxCollider size=(2.4, 1.1, 1.1) 조정
- [x] Log 4종 자식 모델 1.4배 확대, BoxCollider 폭 3~4m로 확장
- [x] LaneConfigSO `_grassDecorScale=1.4` 추가, GrassLane.Build에서 instantiate 직후 스케일 적용
- **설계 결정**: 스케일을 **자식 visual에만** 적용. Root Transform은 1.0 유지 → Player를 Log에 parent 시 스케일 전파 방지
- **비고**: Crossy Road 정품의 "장애물이 플레이어보다 크다" 비주얼 룩 달성

### 품질 개선 라운드 1 ✅ (2026-04-20)
- [x] **장애물 충돌**: `BaseLane.IsBlockedAt(x)` virtual 추가, `GrassLane` 오버라이드, `WorldGenerator.IsCellBlocked(x,z)` 공개. `PlayerController.HandleMoveInput`에서 이동 시작 전 체크 → 나무·바위 셀로는 이동 불가
- [x] **스케일 재조정**: 차량 자식 1.4→1.0 (도로 너비 대비 정상), 통나무 자식 1.4→1.1, 데코 1.4→1.2. BoxCollider도 대응해 축소
- [x] **청크 기반 패턴**: `WorldGenerator.SpawnNextChunk()` — 청크 단위로 같은 타입 N칸 연속 생성, 직전 청크 타입은 가중치 0으로 제외해 반드시 교차. `LaneConfigSO._grassChunk/_roadChunk/_riverChunk = Vector2Int(min,max)` 범위(1-2, 2-4, 2-3). SafeStart 6→3
- [x] **카메라 배경**: Main Camera `clearFlags=SolidColor`, `backgroundColor=(0.66,0.85,0.95)` 하늘색. Skybox·무관한 배경 제거
- [x] Play 검증: Grass(3)→Road(2)→Grass(2)→River(3)→Grass(2)→Road(2)→Grass(1)→Road(3) 패턴 확인, blocked cell 이동 차단(reflection로 gridX 변화 없음) 검증 ✓

### River 레인 정밀 튜닝 ✅ (2026-04-21)
- [x] **통나무 길이 3그리드 고정**: `LogConfig.lengthScale = 4.5248868` → 월드 X 정확히 3.0 유닛 [계산: 3.0 / (0.85 × 0.78)]
- [x] **스폰 밀도 감소**: Log 간격 3~5초, Prewarm 레인당 4개, SpawnScale 0.85
- [x] **통나무 너비 정상화**: `WidthScale=3.5`, BoxCollider `size.z = 0.26` (구 0.9 → 레인 넘침 방지)
- [x] **탑승 판정 타이밍 버그**: `Log.Launch()`에서 `col.bounds.extents.x` 는 스폰 직후 미반영 → `col.size.x * transform.lossyScale.x * 0.5f` 직접 계산
- [x] **Z축 직선 점프**: 통나무 출발 시 `dx == 0` 이면 `to.x = from.x`로 월드 X 고정 (대각선 점프 방지)
- [x] **좌우 이동 허용**: 기존 `prevParent != null && dx != 0 return` 제거
- [x] **`_gridPos.X` 뱅커 반올림 버그**: `RoundToInt(x - 0.5f)` 는 홀수 정수 X(1,3,5,-1)에서 1칸 오차 → `RoundToInt(x)` 로 수정
- [x] **log-relative 좌우 점프**: 사전 `VelocityX * _moveDuration` 보정은 코루틴 정수 프레임 오차로 드리프트 어긋남 → `prevParent.position.x + Lerp(fromRel, toRel, t)` 매 프레임 보간으로 전환
- [x] **맵 경계 자동 하차·익사**: halfSpan 초과 시 언패런트 → clamp → drown
- [x] **착지 후 X 그리드 재스냅**: 지면 착지 시 드리프트 누적 방지
- [x] **BlobShadow**: Sprites/Default 원형 그림자, 모든 차량·통나무 적용
- [x] **GroundFill**: 대형 평면(y=-0.1) 레인 사이 깜빡임 방지

### ⚠ 미해결 이슈 (커밋 `368f836` 기준)
- **통나무 전진 점프 착지 X 불안정**: 통나무 → 다른 통나무로 dz=1 점프 시 매 점프 도착 X가 미세하게 다름. `to.x = from.x`로 고정했음에도 여전함.
  - 가설: `from.x = transform.position.x`가 Update에서 통나무 드리프트로 이미 비정수 상태 → 매번 다른 기준점
  - 다음 세션 시도 방향: `from.x`를 출발 시점 log-relative 기준으로 계산하거나, `_gridPos.X` 정수로 스냅 후 출발

### Step 6 — Rail + 기차 (대기)
### Step 7 — 독수리 타임아웃 사망 (대기)
### Step 8 — 점수 + UI (대기)
### Step 9 — 난이도 커브 + 재시작 플로우 (대기)
### Step 10 — 오디오 일괄 (대기)

### CLAUDE.md 준수 리팩토링 ✅ (2026-04-23)
- [x] 싱글턴 4종 제거 (`GameManager`, `PlayerController`, `WorldGenerator`, `CameraShakeController`) → `[SerializeField]` 주입
- [x] `UnityEvent` → C# `event Action` (`PlayerJumpShakeTrigger.OnLanded`)
- [x] 사망 사유 매직 문자열 제거 → `enum DeathReason { Vehicle, Drown, OutOfBounds, Eagle }` (`Assets/Scripts/Game/DeathReason.cs` 신규)
- [x] `GameManager.OnPlayerDied` 시그니처: `Action<DeathReason>`
- [x] `Vehicle` 충돌 판정 역전: `Vehicle.OnTriggerEnter` 제거 → `PlayerController.OnTriggerEnter`가 `GetComponentInParent<Vehicle>()`로 감지. 런타임 스폰 오브젝트에 싱글턴 없이 주입 어려운 문제 해결.
- [x] `WorldGenerator.Update` GC 제거 — `_despawnBuffer` 필드 재사용(`Clear/Add`)
- [x] `GridPosition.X/Z` public readonly 필드 → 프로퍼티
- [x] `CrossyRoadCameraSetup` — 태그+`FindGameObjectWithTag` 제거 → `_playerTransform` SerializeField
- [x] 모든 SerializeField 의존성에 Awake null 체크 + `LogError` + `enabled=false` 가드
- [x] `Debug.Log` 정보성 로그 `#if UNITY_EDITOR` 가드
- **Unity Editor 재연결 필요** (아래 "에디터 재연결 체크리스트" 참조)

#### 에디터 재연결 체크리스트
1. Hierarchy → Player 선택 → Inspector `PlayerController` 에서:
   - `_inputReader` 슬롯에 InputReader 보유 오브젝트 드래그
   - `_gameManager` 슬롯에 Hierarchy의 GameManager 드래그
   - `_worldGenerator` 슬롯에 Hierarchy의 WorldGenerator 드래그
2. Hierarchy → vcam 선택 → Inspector `CrossyRoadCameraSetup` 에서:
   - `_playerTransform` 슬롯에 Player 드래그 (기존 `_playerTag` 필드는 삭제됨)
3. Hierarchy → vcam(또는 Player) 의 `PlayerJumpShakeTrigger` 에서:
   - `_shaker` 슬롯에 `CameraShakeController` 가 붙은 오브젝트 드래그
4. Player 오브젝트에 BoxCollider(Is Trigger=ON) + Kinematic Rigidbody 확인 (Vehicle 충돌 감지용)
5. Play → 차량 충돌 사망, 익사, 통나무 탑승, 카메라 흔들림 각각 검증

---

## 버그 수정 일지

### 2026-04-23 — 통나무 탑승 후 좌우 남는 간격이 매번 다름
**증상**: Log 너비 3그리드, Player 이동 1그리드 단위 → 좌끝/우끝 탑승 시 양쪽 남는 간격이 대칭이어야 하는데, 탑승·이동할 때마다 간격이 제각각.

**원인**: 점프 착지 타이밍 오차로 인한 상대 오프셋 드리프트.
- `AdjustJumpTargetForLog`는 `to.x = logX + vx × _moveDuration` 로 통나무 예측 중심에 착지 계산
- `MoveRoutine` while 루프는 `elapsed ≥ _moveDuration` 에서 종료 → 실제 경과시간이 항상 1프레임(≈16ms) 초과
- 그 초과 구간 동안 통나무만 `vx × overshoot` 더 흘러감 → 플레이어 최종 X는 예측값 그대로인데 통나무 중심은 더 앞 → 상대 오프셋 `-vx × overshoot` 가 탑승 순간부터 고정
- `SnapToSurface`는 Y만 맞추고 X는 그대로 → 드리프트 영구 유지
- 이후 좌우 점프는 `fromRelX + dx`로 드리프트 값 유지한 채 슬롯 이동 → 매 탑승마다 `vx × dt` 만큼 다른 편차

**해결**:
1. `Log.SnapToSurface` — X도 `Mathf.Round(relX)` 로 정수 슬롯(통나무 중심 ±tileSize)에 스냅 → 탑승 즉시 드리프트 제거
2. `PlayerController.MoveRoutine` 통나무 좌우 점프 — `fromRelX = Mathf.Round((from.x - parent.x) / tileSize) * tileSize` 로 점프 시작 시 재스냅 → 드리프트 재발 방지

**커밋**: (이 커밋에서 수정)

---

## 재개 방법
1. 이 파일(`Memory.md`)을 읽어 마지막 완료 Step 확인
2. **Step 6 (Rail + 기차)** 부터 진행 (통나무 착지 불안정 이슈 해결 완료)
