# Voxel Road — 세부 구현 계획

> 상위 로드맵(`.claude/plans/...md`)의 각 Step을 **클래스·메서드 시그니처·에디터 세팅·테스트**까지 드릴다운한 문서.
> 실제 구현 직전에 해당 Step 섹션을 다시 읽고 차이가 있으면 갱신한다.

---

## 공통 규약

### 네임스페이스
- `VoxelRoad.Common.Interfaces` — 모든 인터페이스 (`IHazard`, `IRideable`, `IGridMovable`, `ILane`, `IInputReader`)
- `VoxelRoad.Common` — 공용 구조체/헬퍼 (`GridPosition`, `TweenHelper`, `ObjectPool<T>`)
- `VoxelRoad.Player` — 플레이어 관련
- `VoxelRoad.Camera` — 카메라 추적
- `VoxelRoad.World` — Lane, WorldGenerator, 풀
- `VoxelRoad.Input` — 입력 처리
- `VoxelRoad.Data` — ScriptableObject 클래스 정의
- `VoxelRoad.Game` — 매니저·UI·상태

### 코딩 규칙
- 모든 public/protected 멤버에 **한글 XML 주석** `/// <summary>`
- 모든 `MonoBehaviour`는 `private void Awake()` 에서 `SerializeField` 참조 null 체크 → `Debug.LogError` 후 `enabled = false`
- Update 내 **new, LINQ, string concat 금지** — 필요 시 캐시된 `StringBuilder`
- 좌표는 `float` 대신 **`GridPosition`(int x, int z) struct** 사용
- 이동 trans는 트윈 코루틴 or `Mathf.Lerp`만 사용 (DoTween 미도입)

### 상수 단위
- 1 타일 = **1 Unity 단위 (1m)**
- 플레이어 점프 duration = **0.12s**
- 플레이어 점프 피크 높이 = **0.35m**
- 기본 Lane 폭(X) = **17칸** (-8 ~ +8), 맵 경계

### 폴더·에셋 배치 규약
- 프리팹: `Assets/Prefabs/{Player,Lanes,Vehicles,Props,UI}/`
- 머티리얼: `Assets/Materials/{Tiles,Vehicles,Characters}/`
- ScriptableObject 인스턴스: `Assets/Data/{Balance,Lanes,Vehicles,Input}/`

---

## Step 1 — 스와이프/탭 입력 + 평면 그리드 이동

### 목적
평평한 Grass 평면 위에서 플레이어가 스와이프로 1칸씩 점프. Lane 생성이나 카메라는 아직 없음.

### 새 파일
| 경로 | 역할 |
|---|---|
| `Assets/Scripts/Common/GridPosition.cs` | 그리드 좌표 struct (int x, int z) |
| `Assets/Scripts/Common/Interfaces/IInputReader.cs` | 입력 이벤트 공급자 인터페이스 |
| `Assets/Scripts/Input/InputReader.cs` | 스와이프 인식, 이벤트 발행 |
| `Assets/Scripts/Input/InputConfigSO.cs` | 스와이프 거리 임계값 등 |
| `Assets/Scripts/Player/PlayerController.cs` | 그리드 점프 이동, 입력 구독 |
| `Assets/Scripts/Game/GameManager.cs` (골격) | 임시 — `Instance` 싱글턴과 상태 enum만 |

### 핵심 설계

**`GridPosition`** (struct, Assets/Scripts/Common/)
```csharp
public readonly struct GridPosition : IEquatable<GridPosition>
{
    public readonly int X;
    public readonly int Z;
    public GridPosition(int x, int z);
    public Vector3 ToWorld();            // (X, 0, Z)
    public static GridPosition FromWorld(Vector3 w);
    public GridPosition Offset(int dx, int dz);
    public bool Equals(GridPosition other);
    public override int GetHashCode();
}
```

**`IInputReader`** (interface)
```csharp
public interface IInputReader
{
    event Action OnSwipeUp;     // +Z
    event Action OnSwipeDown;   // -Z
    event Action OnSwipeLeft;   // -X
    event Action OnSwipeRight;  // +X
    event Action OnTap;         // 화면 터치(전진 단축)
    void Enable();
    void Disable();
}
```

**`InputConfigSO`** (ScriptableObject)
- `float MinSwipeDistancePx = 50f`
- `float MaxSwipeDurationSec = 0.5f`
- `float EditorMouseDragMinPx = 30f`  *(에디터 편의)*

**`InputReader`** (MonoBehaviour, IInputReader)
- `InputSystem_Actions.inputactions` 에서 **Touch Press** (Pointer/Primary) 구독
- 터치 시작 시점/위치 저장 → 해제 시점/위치 차이로 스와이프 판정
- 에디터에서는 마우스 좌클릭 드래그를 같은 로직으로 처리 (Input System이 자동 시뮬레이션)
- 스와이프 판정 후 각도에 따라 상/하/좌/우 결정 (`Mathf.Atan2`)

**`PlayerController`** (MonoBehaviour)
- `[SerializeField] InputReader _inputReader;`
- `[SerializeField] float _hopDuration = 0.12f;`
- `[SerializeField] float _hopHeight = 0.35f;`
- 상태: `GridPosition _currentCell`, `bool _isMoving`, `GridPosition? _queuedMove` (1개만)
- `OnEnable`/`OnDisable`에서 InputReader 이벤트 구독/해제
- `TryMove(int dx, int dz)`:
  - `_isMoving` 이면 `_queuedMove` 에 저장 후 return
  - 맵 경계 체크 (현재는 -8~+8)
  - `StartCoroutine(HopCoroutine(target))`
- `HopCoroutine` — 0.12초 동안 시작→끝 위치 Lerp + 포물선 Y 오프셋
- 이동 완료 후 큐 소비

**`GameManager`** (골격)
- `public enum GameState { Menu, Playing, GameOver }`
- `public static GameManager Instance { get; private set; }`
- `Awake`에서 싱글턴 설정 (중복 시 Destroy)
- `State` 프로퍼티만, 현재는 항상 `Playing`

### InputSystem_Actions.inputactions 수정
기존 `Player` 액션 맵에 **Point**, **Press** 액션을 추가 (Touch/Mouse Binding). 구체적 바인딩:
- `Point`: `<Pointer>/position`
- `Press`: `<Pointer>/press`

> ⚠️ 사용자가 에디터에서 `.inputactions` 파일을 열어 추가해야 함 — Step 1 실행 시 클릭 순서 별도 제공.

### Unity 에디터 세팅 (Step 1 실행 시 클릭 순서)
1. Hierarchy 우클릭 → **Create Empty** → 이름 `_Systems`
2. `_Systems` 자식으로 **Create Empty** → 이름 `InputReader` → `InputReader.cs` Add Component
3. `_Systems` 자식으로 **Create Empty** → 이름 `GameManager` → `GameManager.cs` Add Component
4. Hierarchy에 **3D Object > Cube** → 이름 `Player` → Scale (0.8, 0.8, 0.8) → Position (0, 0.4, 0)
5. `Player`에 `PlayerController.cs` Add Component → Inspector에서 InputReader 드래그 연결
6. Hierarchy에 **3D Object > Plane** → 이름 `GrassFloor` → Scale (2, 1, 2) (20x20 단위) → Material 녹색
7. Play → 마우스 드래그(스와이프 모사)로 Cube가 1칸씩 점프하는지 확인

### 테스트 체크리스트
- [ ] 마우스 상단 드래그 → Player가 +Z 1칸 점프 (점프 애니 0.12s)
- [ ] 좌/우/하단 드래그 동작
- [ ] 연타 시 두 번째 입력이 큐잉되고 첫 번째 완료 후 자동 실행
- [ ] 맵 경계 (-8 ~ +8) 밖으로 이동 불가
- [ ] Console 에러 0건
- [ ] `debug_filtered.log` 정상 갱신

### 예상 리스크
| 리스크 | 대응 |
|---|---|
| Input System 마우스/터치 시뮬레이션 미동작 | **Project Settings > Input System Package**에서 "Use Old Input Manager" 꺼져 있는지 확인 |
| 점프 트윈 튕김 (연타 시 위치 어긋남) | 이동 시작 전 `_currentCell`를 즉시 갱신, 트랜스폼 위치는 트윈이 담당 |

---

## Step 2 — Cinemachine 쿼터뷰 추적 카메라

### 목적
플레이어를 비스듬히(쿼터뷰) 따라가는 카메라. 뒤로 되돌아오지는 않음.

### 새 파일
| 경로 | 역할 |
|---|---|
| `Assets/Scripts/Camera/CameraFollower.cs` | 플레이어 최대 Z를 추적하는 Follow Target 갱신 |

### 핵심 설계
- Cinemachine Virtual Camera (CM vcam)를 Main Camera 자식 아닌 씬 최상위에 배치
- `CameraFollower`는 빈 GameObject `CameraTarget`의 Transform을 플레이어 위치 기준으로 갱신
  - X = 플레이어.X
  - Y = 0 (고정)
  - Z = max(현재 CameraTarget.Z, 플레이어.Z)  ← **단방향(뒤로 안 감)**
- vcam의 Follow 대상 = `CameraTarget`
- vcam Body = **Position Composer**, Aim = **Composer** (혹은 Do Nothing)
- vcam Rotation: Y축 45°, X축 30~35° 하향
- vcam FOV: 원근이면 25~35 / 또는 Orthographic size 4.5~5

### Unity 에디터 세팅 (Step 2 실행 시)
1. 상단 **GameObject > Cinemachine > Virtual Camera** (또는 **Cinemachine Camera**, 3.x 버전)
2. vcam Inspector에서 **Follow**에 새 GameObject `CameraTarget` 드래그
3. `CameraTarget`에 `CameraFollower.cs` 추가 → Player Transform 드래그
4. vcam **Rotation**: X=30, Y=45, Z=0
5. vcam **FOV**: 28 (또는 Orthographic size 5)
6. vcam **Body > Tracked Object Offset**: (0, 0, -6) — 타깃 뒤쪽에서 촬영
7. 기존 Main Camera에 **CinemachineBrain** 컴포넌트 자동 추가됨
8. Play → Player가 전진해도 카메라가 따라오고 뒤로 가도 카메라는 고정

### 테스트
- [ ] 전진 시 카메라 함께 전진
- [ ] 후진 시 카메라 고정 (뒤로 안 감)
- [ ] 쿼터뷰 각도 Crossy Road 유사

---

## Step 3 — Lane 시스템 + WorldGenerator (Grass/Road)

### 목적
플레이어 전방 20칸, 후방 5칸의 Lane이 항상 존재. 전진 시 뒤쪽 Lane 회수 후 앞쪽 재활용(풀링).

### 새 파일
| 경로 | 역할 |
|---|---|
| `Assets/Scripts/Common/Interfaces/ILane.cs` | Lane 공통 계약 |
| `Assets/Scripts/Common/Interfaces/ILaneFactory.cs` | Lane 생성 팩토리 |
| `Assets/Scripts/Common/ObjectPool.cs` | 제네릭 풀 (`Queue<T>` 기반) |
| `Assets/Scripts/World/LaneBase.cs` | MonoBehaviour 기저, `ILane` 구현 |
| `Assets/Scripts/World/GrassLane.cs` | 잔디 Lane |
| `Assets/Scripts/World/RoadLane.cs` | 도로 Lane (차량은 Step 4) |
| `Assets/Scripts/World/WorldGenerator.cs` | 전방 Lane 스폰, 후방 회수 |
| `Assets/Scripts/Data/LaneConfigSO.cs` | Lane 스폰 규칙 |

### 핵심 설계

**`ILane`**
```csharp
public interface ILane
{
    int ZIndex { get; }
    LaneType Type { get; }
    bool IsWalkable(int x);            // 나무/바위 등 차단
    bool IsDeadlyAt(int x);            // 해당 칸 사망 여부 (Road/Rail/River)
    void OnActivate(int zIndex);       // 풀에서 꺼낼 때
    void OnDeactivate();               // 풀로 반환할 때
}
public enum LaneType { Grass, Road, River, Rail }
```

**`LaneConfigSO`**
- `float[] WeightsByDistance` — 멀리 갈수록 Road/Rail 가중치 증가
- `int MinConsecutiveGrass = 0`, `MaxConsecutiveRoad = 3`, `MaxConsecutiveRiver = 3`

**`WorldGenerator`**
- `[SerializeField] int _visibleAhead = 20, _visibleBehind = 5`
- `[SerializeField] LaneConfigSO _config`
- `Dictionary<int, ILane> _activeLanes`
- `Update`:
  - 플레이어의 최대 Z 읽음
  - `spawnUpTo = maxZ + _visibleAhead`
  - 미스폰 Lane을 팩토리에서 꺼내 배치
  - `despawnBelow = maxZ - _visibleBehind` 이하는 풀로 반환
- Lane 선택 알고리즘: 가중치 + 최근 Lane 종류 카운트로 연속 제한 적용

**`ObjectPool<T> where T : Component`**
```csharp
public class ObjectPool<T> where T : Component
{
    public ObjectPool(T prefab, Transform parent, int prewarm);
    public T Get();
    public void Return(T item);
}
```

### Unity 에디터 세팅
1. 프리팹: `GrassLane.prefab`, `RoadLane.prefab` (각각 Mesh는 우선 큐브 늘린 것)
2. Hierarchy에 `_World` 빈 GameObject → `WorldGenerator.cs` 추가
3. `LaneConfigSO` 생성 — Project 창 우클릭 > **Create > VoxelRoad > Data > Lane Config**

### 테스트
- [ ] 시작 시 -5 ~ +20 범위에 Lane 존재
- [ ] 전진 시 앞쪽 Lane 자동 생성, 뒤쪽 자동 회수
- [ ] Road Lane은 시각적으로 회색, Grass는 녹색
- [ ] 100칸 전진 후 GC Alloc 0 (Profiler 확인)

---

## Step 4 — 차량 이동 & 충돌 사망

### 목적
Road Lane에 차량이 지정 속도로 등속 이동. 플레이어와 같은 그리드 칸에 있으면 사망.

### 새 파일
| 경로 | 역할 |
|---|---|
| `Assets/Scripts/Common/Interfaces/IHazard.cs` | 사망 판정 인터페이스 |
| `Assets/Scripts/World/Vehicle.cs` | 차량 이동, Hazard |
| `Assets/Scripts/World/VehicleSpawner.cs` | RoadLane에 차량 스폰 |
| `Assets/Scripts/Data/VehicleDefinitionSO.cs` | 차량 프리팹/속도 정의 |

### 핵심 설계
- `Vehicle`: `Update`에서 `transform.position += direction * speed * Time.deltaTime`
- Lane 경계(-9, +9) 넘어가면 풀로 반환
- 충돌: `Vehicle.GetGridPosition()` vs `Player.GetGridPosition()` 정수 비교 (PlayerController가 매 프레임 확인)
- 또는 Vehicle이 `RoadLane.RegisterVehicle()` 해서 RoadLane이 `IsDeadlyAt(x)` 구현

**차량-플레이어 충돌 검사 위치**: `PlayerController.LateUpdate`
```csharp
void LateUpdate() {
    var lane = WorldGenerator.Instance.GetLaneAt(_currentCell.Z);
    if (lane != null && lane.IsDeadlyAt(_currentCell.X)) {
        GameManager.Instance.TriggerGameOver(DeathCause.Vehicle);
    }
}
```

### Unity 에디터
- `Vehicle.prefab` (차 모양, 임시 큐브 길게) → `VehicleDefinitionSO` 3~5종 생성
- `RoadLane.prefab`에 `VehicleSpawner.cs` 추가

### 테스트
- [ ] 도로에 차량 이동
- [ ] 플레이어가 차량과 같은 칸 → 즉시 GameOver 호출
- [ ] 차량이 맵 끝 도달 후 반대편에서 재등장 (순환)

---

## Step 5 — River + 통나무 + 익사/탑승

### 새 파일
- `Assets/Scripts/Common/Interfaces/IRideable.cs`
- `Assets/Scripts/World/RiverLane.cs`
- `Assets/Scripts/World/Log.cs`

### 핵심 설계
- `RiverLane.IsDeadlyAt(x)`: **통나무 없으면 true**
- `Log`가 플레이어 그리드 위에 있으면 플레이어 `SetRidingLog(log)` → 매 프레임 log 위치 따라 X 보정
- 플레이어 점프 시 `_currentLog = null`, 착지 후 다시 Lane 체크
- 물 Lane에 통나무 없이 착지 시 익사

### 테스트
- [ ] 통나무 위에서 x축 자동 이동
- [ ] 통나무에서 내리면 즉시 물에 빠짐
- [ ] 통나무 맵 밖 도달 시 플레이어 함께 이탈 → GameOver

---

## Step 6 — Rail + 기차

### 새 파일
- `Assets/Scripts/World/RailLane.cs`
- `Assets/Scripts/World/Train.cs`

### 핵심 설계
- `RailLane.IsDeadlyAt(x)`: 기차 통과 중 true
- 기차는 차량보다 훨씬 빠름 (20~30/s)
- 통과 1.5초 전 경고등 Emission 깜빡임, `AudioSource.PlayOneShot(warningBell)`
- 기차는 Lane 여러 칸(예: 폭 5칸) 점유

### 테스트
- [ ] 경고 후 기차 통과
- [ ] 경고 중에 레일 위로 올라가도 죽지 않음 (경고는 시각만)
- [ ] 기차와 같은 칸 → 즉시 사망

---

## Step 7 — 독수리 타임아웃

### 새 파일
- `Assets/Scripts/Game/EagleTimeoutSystem.cs`

### 핵심 설계
- `GameBalanceSO.IdleTimeoutSec = 3.0f`
- PlayerController가 전진할 때마다 `_lastForwardTime = Time.time`
- EagleTimeoutSystem Update: `Time.time - _lastForwardTime > timeout` → 독수리 연출 + GameOver
- 또는 **카메라 뒷경계 밖으로 플레이어 Z 이탈 시** 즉시 독수리 트리거

### 테스트
- [ ] 가만히 3초 → 독수리 연출 + GameOver
- [ ] 카메라보다 5칸 뒤로 후진 → 독수리
- [ ] 계속 전진 중이면 타임아웃 리셋

---

## Step 8 — 점수 + UI

### 새 파일
- `Assets/Scripts/Game/ScoreView.cs`
- `Assets/Scripts/Game/GameOverView.cs`
- `Assets/Scripts/Game/StartView.cs`
- `Assets/Scripts/Game/ScoreService.cs` (PlayerPrefs 래핑)

### 핵심 설계
- `ScoreService.BestScoreKey = "VoxelRoad.BestScore"`
- `GameManager`가 `CurrentScore` 프로퍼티 제공 (= 플레이어 최대 Z)
- UGUI Canvas (Screen Space - Camera or Overlay)
- Start → 버튼 → `GameManager.StartGame()` → Playing
- GameOver → 최종 점수 + Retry 버튼 → 씬 재로드 또는 Reset

### 테스트
- [ ] 점수가 실시간 갱신
- [ ] 게임오버 시 최고점수 저장
- [ ] Retry 누르면 새 게임 시작

---

## Step 9 — 난이도 커브 + 재시작 플로우

### 핵심 설계
- `GameBalanceSO`에 거리별 차량 속도/간격 curve (`AnimationCurve`)
- WorldGenerator가 플레이어 Z 기반으로 LaneConfigSO의 가중치를 동적으로 보정
- 페이드 전환 (단색 이미지 `CanvasGroup.alpha` 페이드)
- Reset 시: 모든 Lane 풀로 회수, 플레이어 (0,0)으로, 점수 0

### 테스트
- [ ] 100칸 전진 시 차량 속도 증가
- [ ] Retry 버튼 여러 번 눌러도 메모리 리크 없음 (Profiler)

---

## Step 10 — 오디오 일괄

### 새 파일
- `Assets/Scripts/Game/AudioManager.cs`
- `Assets/Scripts/Data/AudioConfigSO.cs`

### 핵심 설계
- AudioManager 싱글턴
- BGM (루프) + SFX 풀 (여러 AudioSource 돌려쓰기, **GC 방지**)
- 이벤트 훅: 점프, 충돌, 기차 경고, 게임오버, 버튼 클릭
- Freesound.org CC0 사운드 `Assets/ThirdParty/Sounds/` 에 배치

### 테스트
- [ ] 점프 시 SFX
- [ ] 사망 시 SFX
- [ ] BGM 루프 정상

---

## 일정 예상 (참고)

| Step | 예상 대화 턴 수 (구현 + 테스트) |
|---|---|
| 1 | 3~5 |
| 2 | 2~3 |
| 3 | 5~7 |
| 4 | 4~5 |
| 5 | 4~5 |
| 6 | 3~4 |
| 7 | 2~3 |
| 8 | 4~6 |
| 9 | 3~4 |
| 10 | 3~4 |

> 각 Step 완료 시 Memory.md 갱신 + 사용자에게 `/compact` 권장.
