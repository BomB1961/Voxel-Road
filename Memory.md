# Voxel Road — 작업 진행 기록

> 터미널 재시작 후에도 어디까지 작업했고 어디서부터 시작할지 파악하기 위한 로그.
> 각 Step 완료 시 이 파일을 갱신한다.

## 프로젝트 개요
- Crossy Road 모작 (모바일 3D 러너)
- Unity 6000.4.0f1, URP, New Input System, Cinemachine 3.1.6
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
- **주의**: `mcpforunity://editor/state` 리소스가 Map 인스턴스를 반환하는 라우팅 불안정 현상 있음.
  `execute_code` 결과는 Voxel Road 정상. 매 세션 시작 시 `set_active_instance(6401)` 필수.

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
- [x] `Assets/Scripts/Camera/CameraFollower.cs` — LateUpdate Lerp 추적, Z 일방향(maxZ 기록), X 고정 옵션
- [x] Main Camera에 부착, Target=Player, Offset=(5, 9, -10), Rotation=(30, -25, 0), FollowSpeed=3.5
- [x] Play 테스트: Player.z 0→3 시 Cam.z -10→-7 추적 ✓, 후진(z=1) 시 maxZ=3 유지 ✓
- [x] Game View 커스텀 프리셋 `Mobile Portrait 1080x1920` (Android idx=18) 적용, Camera.aspect=0.563
- **설계 결정**: 기획서의 Cinemachine 대신 단순 MonoBehaviour. 이유: Crossy Road는 고정 오프셋+일방향 추적이라 vcam 파이프라인 불필요

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
- [x] `WorldGenerator` — `static Instance` 노출, `GetLaneTypeAt(z)` 공개 API, River 가중치 포함 3종 랜덤
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

### Step 6 — Rail + 기차 (대기)
### Step 7 — 독수리 타임아웃 사망 (대기)
### Step 8 — 점수 + UI (대기)
### Step 9 — 난이도 커브 + 재시작 플로우 (대기)
### Step 10 — 오디오 일괄 (대기)

---

## MCP 연결 정보 (2026-04-20 기준)
- MCP For Unity v9.6.6, Transport: Stdio
- Client: **Claude Desktop** (NOT Claude Code CLI)
- **포트 주의**: Voxel Road와 Map 프로젝트가 모두 실행 중이면 포트가 유동적(6400/6401이 서로 바뀜). 반드시 `mcpforunity://instances` 리소스로 현재 포트 확인 후 `set_active_instance("Voxel Road@<hash>")` 지정
- Unity 에디터에서 `Session Active (Voxel Road)` 녹색 확인 필수
- `execute_code`에서 `Application.dataPath`로 `C:/Users/admin/Unity/Voxel Road/Assets` 여부 반드시 검증
- MCP 도구가 로드되지 않을 경우: Claude Desktop **트레이 아이콘 → Quit → 재실행** 후 **새 대화** 시작

---

## 재개 방법
1. 이 파일(`Memory.md`)을 읽어 마지막 완료 Step 확인
2. `mcpforunity://instances` 리소스로 Voxel Road 포트 확인 후 `set_active_instance` 호출
3. `execute_code`로 `Application.dataPath` 검증 (Voxel Road 확인)
4. 미완료 Step부터 재개 — 현재 **Step 6 (Rail + 기차)**
