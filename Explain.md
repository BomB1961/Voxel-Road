# Voxel Road — 개발 설명서

> **작성 기준**: 2026년 4월 28일 — Step 0~9 완료, Step 10 (오디오)·모듈화 리팩토링만 남음

---

## 1. 프로젝트 개요

**Voxel Road**는 글로벌 히트 모바일 게임 *Crossy Road* (2014, Hipster Whale, 누적 2억 다운로드)의 핵심 게임플레이를 레퍼런스로 삼아, 동일한 게임 경험을 **처음부터 직접 설계하고 구현**한 개인 프로젝트입니다.

| 항목 | 내용 |
|---|---|
| 장르 | 하이퍼캐주얼 아케이드 러너 |
| 플랫폼 | Android (Portrait 1080×1920) |
| 엔진 | Unity 6000.4.x / URP |
| 개발 인원 | 1인 (기획·프로그래밍·아트 파이프라인 전담) |
| 개발 방식 | 10단계 마일스톤 분할, 현재 Step 9 완료 |

---

## 2. 기획 의도와 목표

### 왜 Crossy Road인가?

Crossy Road는 단순한 터치 한 번으로 즐길 수 있는 **원터치 메커닉**과, 무한 생성되는 세계를 기반으로 한 **리플레이 가치**가 절묘하게 맞물린 타이틀입니다. 이 게임을 레퍼런스로 선택한 이유는 세 가지입니다.

1. **구현 복잡도 대비 완성도 검증이 가능**: 누구나 아는 게임이므로 "잘 만들었는가"를 즉시 판단할 수 있습니다.
2. **핵심 기술 스택 검증**: 그리드 이동, 절차적 맵 생성, 이동 플랫폼(통나무), 카메라 시스템 — 모바일 게임의 핵심 기술들이 모두 포함됩니다.
3. **확장 가능한 구조 설계 테스트**: 장애물 유형(차량 → 통나무 → 기차)을 계속 추가할 수 있는 아키텍처를 직접 설계해보는 것이 목표였습니다.

### 품질 기준

> "프로토타입이더라도 최종 목표는 **정품 Crossy Road 수준**의 조작감과 완성도"

이 기준 하에 타협하지 않은 부분들:
- 점프 연출의 사인 아크 곡선 (0.12초 내 완성되는 시각적 쾌감)
- 통나무 예측 착지 보정 (플레이어가 통나무를 향해 점프할 때 자연스럽게 올라타도록)
- 카메라의 전진 전용 추적 (뒤로 물러나도 카메라가 따라가지 않는 긴장감)
- **인간 반응 한계 보호** (Step 9 청크 난이도 시스템에서 multiplier 상한 1.6 + 절대 가드)

---

## 3. 개발 로드맵 (10단계 마일스톤)

```
Step 0  ✅  프로젝트 셋업 (URP, Input System, Cinemachine 패키지 구성)
Step 1  ✅  입력 + 그리드 이동 (스와이프/키보드 → 0.12초 점프)
Step 2  ✅  카메라 시스템 (Cinemachine 확장 — 전진 추적, 댐핑, X 클램프)
Step 3  ✅  레인 시스템 + 맵 생성기 (청크 기반 무한 생성, Quota-Deck 알고리즘)
Step 4  ✅  차량 + 도로 위험 요소 (6종 차량, 레인별 방향/속도 다양화)
Step 5  ✅  강 + 통나무 + 탑승 (이동 플랫폼, 드리프트 보정, 예측 착지)
Step 6  ✅  철길 + 기차 (다중 차량 편성, URP 경고등 선행 알림)
Step 7  ✅  압박 메커니즘 (독수리→ChasingWall→Idle Death+후퇴 제한, 3단 진화)
Step 8  ✅  UI + 점수판 (HUD, 게임오버, 신기록, 픽셀 폰트)
Step 9  ✅  난이도 곡선 (거리 base × 청크 무작위 variance, 인간 한계 가드)
Step 10 🔲  오디오 (BGM + SFX 풀링)
```

---

## 4. 아키텍처 설계

### 4-1. 핵심 원칙: 그리드 논리 vs. 비주얼 분리

모든 게임 로직은 **정수 그리드 좌표(GridPosition)**에서 동작합니다. 플레이어가 실제로 화면에서 이동하는 `transform.position`은 그리드 좌표를 월드 좌표로 변환한 **목표 지점을 향한 보간 결과물**일 뿐입니다.

```
GridPosition(int X, int Z)   ←  게임 로직 전용 (충돌 판정, 장애물 체크, 사망 판정)
        ↓  ToWorldPosition()
Vector3(float x, float y, float z)  ←  비주얼 전용 (Lerp, 점프 아크 연출)
```

이 분리 덕분에:
- 부동소수점 오차가 게임 로직에 침투하지 않습니다
- 통나무 위에서 떠내려가는 동안에도 그리드 좌표는 항상 정확합니다
- 미래에 네트워크 동기화나 리플레이 기록을 붙이기 쉽습니다

`GridPosition`은 GC(Garbage Collection)를 유발하지 않는 **readonly struct**로 구현되어, 모바일에서 매 프레임 생성해도 메모리 부담이 없습니다.

---

### 4-2. 입력 시스템

```
[InputReader]  ─── OnMoveInput(MoveDirection) ──►  [PlayerController]
  (터치/스와이프/키보드 통합)                         (이동 실행)
```

`InputReader`는 Unity New Input System의 **EnhancedTouch API**를 사용합니다. 스와이프 판정 로직:

- 터치 시작 좌표 기억 → 터치 종료 시 delta 계산
- delta.magnitude ≥ 50px: 더 큰 축 방향으로 스와이프 판정
- delta.magnitude < 50px: 단순 탭 → 전진으로 처리 (Crossy Road 동일 동작)

`IInputReader` 인터페이스를 통해 실제 입력 방식(터치/키보드/AI 봇)으로부터 `PlayerController`를 완전히 분리했습니다.

---

### 4-3. 플레이어 이동 — 입력 버퍼링과 점프 아크

```csharp
// 이동 중 연타 입력도 다음 이동으로 정확히 전달
GridPosition basePos = _isMoving ? _moveTarget : _gridPos;
GridPosition target = basePos.Move(dx, dz);
```

이동 중 들어온 입력을 `_queuedTarget`에 한 개 버퍼링하여, **연속 탭 시 2칸 점프가 발생하지 않으면서도** 다음 이동이 즉시 시작되는 반응성을 확보했습니다.

점프 연출은 사인 곡선 아크:
```csharp
float arc = Mathf.Sin(t * Mathf.PI) * _jumpHeight;   // t: 0→1 / height: 0.5m
```

이 곡선은 이륙과 착지 모두 자연스러운 가속/감속을 만들어냅니다.

**후퇴 5그리드 제한** (Step 7 최종 형태):
```csharp
// MaxZ - 5 이내까지만 후퇴 허용. 카메라 deadzone과 일치 → 시야 밖으로 안 나감.
if (target.Z < MaxZ - _backwardLimitGrids) return;
```

장애물 우회를 위한 단방향 제약을 풀되, 무한 후퇴는 막아 게임 진행을 강제합니다.

---

### 4-4. 맵 생성기 — Quota-Deck 알고리즘

단순 랜덤 가중치 방식 대신, **카드 덱 셔플 + 쿼터** 기법을 설계·구현했습니다.

**문제점 (순수 랜덤)**: 잔디 5개 연속, 강 1개, 잔디 3개... 처럼 특정 타입이 몰릴 수 있어 지루함과 불공평함이 발생합니다.

**해결책 (Quota-Deck)**:
```
덱 구성: Grass×3 + Road×2 + River×2 + Rail×1  →  Fisher-Yates 셔플
청크 결정: 덱 앞에서 꺼내되, 직전 타입과 같으면 뒤로 보냄 (교착 방지)
덱 소진 시: 자동 재충전 후 셔플
```

이 결과 **8 청크마다 반드시 각 타입이 정해진 비율로** 등장하면서도, 순서는 매 게임 다릅니다.

각 레인은 청크 단위로 묶입니다 (도로: 2~4레인, 강: 3~5레인, 잔디: 2~3레인, 철길: 1레인). 청크 경계에서만 타입이 바뀌므로 자연스러운 지형 구획이 형성됩니다.

**메모리 관리**: 플레이어 전방 25레인, 후방 8레인만 유지. 범위 벗어난 레인은 `Destroy()` — 무한 전진해도 메모리는 상수 수준을 유지합니다.

---

### 4-5. 레인 계층 구조 — 확장 가능한 설계

```
ILane (interface)  ── Initialize(zIndex, laneSpanX, difficultyMultiplier)
  └── BaseLane (abstract MonoBehaviour)
        ├── GrassLane   — 잔디 + 확률적 장애물 배치
        ├── RoadLane    — 아스팔트 + 차선 마커 + 차량 스포너
        ├── RiverLane   — 강물 + 통나무 스포너
        └── RailLane    — 선로 + URP 경고등 + 기차 스포너
```

새 레인 타입을 추가하려면 `BaseLane`을 상속하고 `Build()` 메서드만 구현하면 됩니다. `LaneType` enum과 `WorldGenerator`의 `SpawnLane()`에 한 줄씩만 추가하면 맵 생성기에 자동으로 통합됩니다.

**Step 9 통합 (2026-04-28)**: `Initialize` 시그니처에 `difficultyMultiplier`가 추가되어, 청크 단위 난이도가 자기 Spawner로 전달됩니다.

**GrassLane 연속 차단 방지 로직**: 잔디 데코(나무·바위)가 3칸 이상 연속 배치되면 플레이어가 통과할 수 없는 벽이 생깁니다. 이를 방지하기 위해 `consecutiveBlocked` 카운터를 사용, 2칸 연속 후에는 강제로 빈 칸을 만듭니다.

---

### 4-6. 통나무 탑승 시스템 — 이동 플랫폼의 핵심 기술

통나무 탑승은 이 프로젝트에서 가장 복잡한 기술적 과제였습니다. 통나무가 X축으로 계속 흐르는 상태에서 플레이어가 올라타야 하기 때문에, 세 가지 문제를 동시에 해결해야 했습니다.

**문제 1: 착지 지점 예측 (AdjustJumpTargetForLog)**

플레이어가 점프를 시작하는 순간(0.12초 소요)에 통나무는 계속 흐릅니다. 점프 목표를 현재 통나무 위치로 설정하면 착지 시점에 통나무가 이미 이동해 빗나갑니다.

해결: `Physics.OverlapBox`로 착지 셀 위의 통나무를 탐색, `log.VelocityX × moveDuration`으로 **착지 시점의 통나무 예측 위치**를 계산해 점프 목표 X를 보정합니다. 단, 보정 범위는 ±0.6m로 제한해 멀리 있는 통나무에 자석처럼 끌려가는 현상을 방지합니다.

**문제 2: 탑승 후 드리프트 동기화**

탑승 성공 후에는 플레이어를 통나무의 자식(child) Transform으로 설정합니다. Unity의 부모-자식 관계에 의해 통나무가 이동하면 플레이어도 자동으로 함께 이동합니다. `Update()`에서 통나무 월드 X를 읽어 그리드 X를 재동기화합니다.

**문제 3: 통나무 위에서의 좌우 점프**

통나무 위에서 좌우로 이동할 때는 월드 좌표 기준이 아닌 **통나무 상대 좌표** 기준으로 보간합니다. 그렇지 않으면 "통나무에서 1칸 오른쪽으로 이동"이 통나무 드리프트에 의해 실제로 다른 거리가 됩니다.

```csharp
// 좌우 점프: 통나무 상대 좌표로 보간
fromRelX = Mathf.Round((from.x - prevParent.position.x) / _tileSize) * _tileSize;
toRelX   = fromRelX + dx * _tileSize;

// 매 프레임 통나무 위치를 기준으로 X 계산 → 통나무가 흘러도 자동 추적
x = prevParent.position.x + Mathf.Lerp(fromRelX, toRelX, t);
```

`Mathf.Round`로 시작 오프셋을 정수 슬롯에 스냅하여 누적 드리프트를 흡수합니다.

**전진 점프 (dx==0) 분기**: 통나무에서 다른 레인으로 건너갈 때는 출발 통나무 드리프트를 추적하면 안 됩니다. `to.x = from.x`로 월드 X를 고정해 직선 점프를 보장합니다.

---

### 4-7. 카메라 시스템 — Cinemachine 확장

Unity Cinemachine의 **PostPipelineStage Extension**으로 구현했습니다. 기존 파이프라인을 통째로 교체하지 않고 Body 스테이지만 가로채어 위치를 직접 제어하는 방식입니다.

| 동작 | 구현 |
|---|---|
| 전진 시만 Z 추적 | `effectivePlayerZ = Max(startZ, playerZ - deadzone)` |
| 후퇴 시 Z 고정 | deadzone=5레인 (PlayerController의 후퇴 한계와 일치) |
| 좌우 댐핑 | `Lerp(pos.x, targetX, dt × 8)` — 빠른 추적 (구 1.5에서 5배 가속) |
| X 클램프 | `MapXLimit = mapHalfSpan - visibleHalfWidth` — 맵 끝 노출 방지 |
| Y 고정 | 플레이어 점프 아크가 카메라에 전이되지 않음 |
| 자동 전진 | **OFF** (Step 7 진화 결과 — 시간 압박은 Idle Death로 대체) |

`MapXLimit` 값은 static으로 노출되어 `PlayerController`의 좌우 이동 경계 판정에 그대로 재사용됩니다. 카메라 가시 범위 = 플레이어 이동 가능 범위가 항상 일치합니다.

---

### 4-8. Step 7 진화 — 압박 메커니즘 3단 변천

게임에 시간 압박을 부여하는 방법은 **세 번** 재설계됐습니다. 디자인 의사결정 과정 자체를 기록합니다.

**1단계 (폐기): 독수리 타임아웃**
- Crossy Road 원작 메커니즘 — 정지 시 독수리가 낚아채감
- 폐기 사유: CC0 독수리 에셋 부재, 시각적 표현 없는 추격자는 몰입 약함

**2단계 (폐기): 자동 전진 카메라 + ChasingWall**
- 카메라가 시간 누적으로 자동 전진(1.5 m/s) + 화면 하단을 따라가는 흰 벽
- 통나무 균형 문제: 통나무 1.5 m/s ≈ 자동 전진 1.5 m/s → 강 위 정체
- 단조로운 시각적 압박 — 매번 같은 속도의 벽

**3단계 (현재): Idle Death + 후퇴 5그리드 제한**
- 마지막 입력 시점부터 5초 → 사망 (`DeathReason.Idle`)
- 후퇴는 MaxZ - 5 이내까지만 허용
- 카메라 자동 전진은 OFF, 멈춰 있어도 카메라 그대로
- **사용자 통찰 반영**: 좌우 입력으로도 idle reset됨 → 통나무·자동차 빈틈 대기 같은 정당한 회피 인정

이 진화 과정은 "어떤 메커니즘이 최선인가"를 미리 결정하지 않고, **실제 플레이 검증과 사용자 피드백을 통해 점진적으로 개선**한 결과입니다.

---

### 4-9. 데이터 주도 설계 (ScriptableObject)

모든 수치 튜닝값을 ScriptableObject로 분리했습니다.

```
Assets/Data/
  LaneConfigSO    — 레인 폭, 룩어헤드/룩비하인드, 쿼터, 청크 길이, 데코 밀도
  VehicleConfigSO — 6종 프리팹, 속도 배열, 스폰 간격, 스케일
  LogConfigSO     — 통나무 프리팹, 속도 배열, 스폰 간격, 가로·세로 비율
  TrainConfigSO   — 기차 프리팹, 속도, 경고 시간, 차량 편성, cycle 간격
  InputConfigSO   — 스와이프 최소 거리(px)
```

이 구조 덕분에:
- 프로그래머 없이 디자이너가 직접 인스펙터에서 밸런스 조정 가능
- 청크 단위 multiplier(Step 9)는 SO 수치에 곱셈으로 적용되므로 base 값과 독립적
- 런타임에 코드 재컴파일 없이 즉각 반영

---

### 4-10. 차량 & 통나무 & 기차 스포너 — Prewarm + 청크 multiplier

게임 시작 직후 레인이 텅 비어 보이지 않도록 **Prewarm** 패턴을 적용했습니다. 스포너 초기화 시 레인 전체에 걸쳐 일정 간격으로 차량/통나무를 미리 배치합니다.

```csharp
// Prewarm: 레인을 N등분해 각 구간 중앙에 ±25% 지터로 배치
const int Count = 8;   // Vehicle=6, Log=8 (강은 더 빽빽)
float spacing = _laneSpanX / Count;
for (int i = 0; i < Count; i++)
{
    float baseX = -halfSpan + spacing * (i + 0.5f);
    float jitter = Random.Range(-spacing * 0.25f, spacing * 0.25f);
    SpawnAt(baseX + jitter);
}
```

레인별 속도는 초기화 시 1회만 결정하고 고정합니다(추월·관통 방지). 프리팹은 스폰마다 랜덤 선택하여 시각적 다양성을 확보합니다.

**Step 9 multiplier 적용**: `Initialize(config, dir, span, multiplier)`에서 multiplier를 받아 속도×mul, 스폰 간격÷mul로 최종값 결정. 같은 청크 안의 모든 lane은 같은 multiplier를 공유합니다.

**기차 스포너**는 단일 차량이 아닌 **편성**을 동시에 스폰합니다 (`CarsPerTrain`개의 차량을 carStride 간격으로 연결). URP 경고등이 6Hz로 깜빡인 후 `WarningSeconds` 후 본 기차가 등장합니다.

---

### 4-11. Step 9 청크 난이도 시스템 — `BalanceCurve`

단조로움 방지(매 청크마다 다른 난이도)와 거리 기반 점진 압박을 **단일 곱셈 시스템**으로 통합했습니다.

```
최종 multiplier = clamp(거리base × 청크variance, 0.7, 1.6)
```

**거리 곡선 (DistanceBase)**:
| MaxZ 구간 | base 값 |
|---|---|
| 0 ~ 50 | 1.00 (학습 구간) |
| 50 ~ 150 | 1.00 → 1.30 선형 |
| 150 ~ 300 | 1.30 → 1.60 선형 |
| 300 + | 1.60 plateau (인간 한계 보호) |

**청크 variance** (확률 추첨):
- Easy 30% → ×0.7
- Normal 50% → ×1.0
- Hard 20% → ×1.3

**적용 대상**:
- VehicleSpawner: speed × mul, spawnInterval / mul
- LogSpawner: 동일
- TrainSpawner: speed × mul (절대 상한 25 m/s), cycle / mul (절대 하한 2.5초)

**합리적 범위 가드**: 최종 multiplier를 `[0.7, 1.6]`으로 clamp하여, 거리(1.6) × Hard(1.3) = 2.08 같은 폭발적 곱이 회피 불가능 청크를 만드는 일을 차단합니다.

**같은 청크 내 일관성**: 청크 시작 시 한 번 추첨된 multiplier를 청크 내 모든 lane이 공유합니다. 도로 청크 4 lane 전체가 Hard 속도로 묶여 "이번 도로는 어렵다"가 시각적으로 일관되게 인식됩니다.

---

### 4-12. UI 시스템 — Step 8

```
VoxelRoad.UI/
  ScoreTracker     — PlayerController.MaxZ 폴링 → Score / BestScore 노출
  GameHUD          — 인게임 SCORE / HI-SCORE 표시
  GameOverPanel    — 사망 시 패널 + Restart 버튼 + 신기록 배지
  UIPulse          — 사인파 알파 펄스 (TAP TO RESTART)
  UIFlicker        — 주기 알파 토글 (GAME OVER, NEW RECORD!)
  UIScorePop       — 점수 갱신 시 RectTransform 스케일 펑
```

**디자인 의도**: 레트로 오락실 분위기를 폰트와 색상만으로 살리되, 게임 화면을 가리지 않는 미니멀 배치.

| 요소 | 위치 | 의도 |
|---|---|---|
| SCORE | 화면 상단 정중앙 (큰 글씨) | 메인 강조 (Crossy Road 스타일) |
| HI-SCORE | 우상단 (작은 황색) | 보조 정보, 시야 방해 X |
| GAME OVER | 화면 중앙, 빨강 깜빡임 | 사망 임팩트 |
| NEW RECORD! | 핫핑크 빠른 깜빡임 | 신기록 시만 표시 |
| TAP TO RESTART | 하단 펄스 | 재시작 유도 |

**폰트**: Kenney Mini Square (CC0) — 픽셀 비트맵 느낌. TextMeshPro Font Asset으로 변환해 외곽선 / 색상 / 크기 제어.

**Best Score 영속화**: `PlayerPrefs.SetInt("VoxelRoad.BestScore", v)` — 별도 저장 시스템 없이 단순 처리 (과잉설계 방지).

**EventBus 의존성 분리**:
- `ScoreTracker`는 `PlayerController.MaxZ`를 매 프레임 폴링 → 변경 시 `OnScoreChanged` C# event 발생
- `GameHUD`, `GameOverPanel`은 `OnPlayerDied` (GameManager) / `OnScoreChanged` (ScoreTracker) 구독
- `UnityEvent` 대신 C# event 사용 (CLAUDE.md 규칙)

---

## 5. 사용 기술 스택

| 분류 | 기술 |
|---|---|
| 엔진 | Unity 6000.4.x |
| 렌더 파이프라인 | URP (Universal Render Pipeline) |
| 입력 | Unity New Input System + EnhancedTouch API |
| 카메라 | Cinemachine 3.1.6 (Custom Extension) |
| 물리 | Unity PhysX (OverlapBox for 탑승 판정) |
| UI | uGUI (Canvas) + TextMeshPro |
| 폰트 | Kenney Mini Square (CC0) |
| 에셋 | Kenney.nl CC0 (blocky-characters, car-kit, nature-kit, fonts) |
| 언어 | C# (.NET Standard 2.1) |
| 디버깅 | unity-cli connector v0.3.13 (외부 콘솔·런타임 조회) |

---

## 6. 현재 구현 완료 기능 요약

- **이동**: 그리드 기반 4방향 이동 (후퇴는 MaxZ-5 이내), 0.12초 사인 아크 점프, 입력 1단계 버퍼링
- **맵 생성**: 쿼터-덱 무한 생성 (잔디/도로/강/철길), 청크 기반 타입 변환, 동적 스폰/디스폰
- **잔디**: 확률적 장애물(나무·바위·꽃), 연속 3칸 차단 방지, 안전 시작 구간
- **도로**: 6종 차량, 레인별 고정 속도, 짝/홀수 교대 방향, 차선 마커
- **강**: 4종 통나무, 드리프트 예측 착지, 상대 좌표 보간, 경계 이탈 익사, Prewarm 8개
- **철길**: 다차량 편성 기차, URP 경고등, cycle 4~8초 (multiplier 적용)
- **카메라**: 전진 전용 Z 추적, 5레인 데드존, 좌우 빠른 댐핑(8.0), X 클램프
- **압박**: Idle Death 5초 (입력 시 reset), 후퇴 5그리드 제한
- **UI**: SCORE / HI-SCORE HUD, 게임오버 패널, 신기록 배지, 재시작 버튼, PlayerPrefs 영속화
- **난이도**: 거리 base × 청크 variance multiplier, 인간 한계 가드 (0.7~1.6 clamp)
- **사망 처리**: Vehicle / Drown / OutOfBounds / Train / Idle (5종) → 즉시 전환, R키·버튼 재시작

---

## 7. 다음 개발 단계

### Step 10: 오디오 (남은 작업)
- BGM 재생 및 페이드 인/아웃
- SFX 풀링 (점프·착지·사망·점수·기차 경고·신기록)
- AudioSource 재사용 풀로 GC alloc 0 보장
- 모바일 환경 고려 — OGG 포맷 + 짧은 SFX는 PCM, BGM은 Streaming

### Step 10 이후: 모듈화 리팩토링
`project_modularity_status.md` 메모리 기준 — PlayerController 비대(290줄), 3종 Spawner의 중복 패턴, 통나무 탑승 로직 분산. Step 10 완료 후 본격 정리 예정.

---

## 8. 기술적 도전과 해결 과정

### 도전 1: 통나무 위 전진 점프 X 불안정 (해결 완료)

**증상**: 통나무에서 앞(dz=1) 방향으로 점프할 때 착지 X 좌표가 매번 조금씩 달라지는 드리프트.

**원인 분석**: 점프 시작 시 `from.x = transform.position.x`를 캡처하는데, 이 시점 transform.x는 통나무 드리프트 때문에 정수가 아닙니다. 점프할 때마다 basis가 다르므로 누적됩니다.

**해결**: 지면 출발 시 `from.x`를 그리드 정수로 스냅. 통나무 출발 전진 시 `to.x = from.x`로 월드 X 고정. 착지 후 비강 레인에서 그리드 X 재스냅. 좌우 점프 시작 시 `Mathf.Round`로 fromRelX 정수 슬롯 스냅.

### 도전 2: Z-fighting (레인 겹침) (해결 완료)

**증상**: 동일 Y 평면에 여러 레인 바닥이 겹치면 깜빡임.

**해결**: 레인 타입별로 Y 오프셋 분리 (Grass: -0.02, River: -0.01, Road: 0). 단 0.01~0.02m 차이지만 완전히 해소.

### 도전 3: 카드 덱의 연속 같은 타입 교착 (해결 완료)

**증상**: 덱 셔플 결과 앞 두 장이 같은 타입일 경우, 직전 청크와 합쳐 같은 타입 3연속 가능.

**해결**: 같은 타입이면 덱 뒤로 보내고 다음 카드 사용. 두 번 시도에도 실패하면 (단일 타입 덱 극단 케이스) 그냥 사용하여 무한 루프 방지.

### 도전 4: ChasingWall vs 통나무 속도 균형 (해결 — Idle Death로 대체)

**증상**: 자동 전진 카메라(1.5 m/s)와 통나무 최저 속도(1.5 m/s)가 같아 강 위에서 정체. 통나무 속도를 0.5배로 줄이면 ChasingWall에 잡힘.

**원인**: 두 동적 시스템이 같은 차원의 압박을 부여하면서 서로 간섭.

**해결**: 시스템 자체를 재설계. ChasingWall + 자동 전진 폐기 → Idle Death (시간 압박) + 후퇴 5그리드 (공간 압박). 통나무 속도와 압박 시스템이 독립.

### 도전 5: Idle Death의 좌우 흔들기 회피 (해결 — 입력 기반 reset)

**증상**: 초기 설계는 MaxZ 갱신만 idle reset. 사용자가 좌우 무빙으로 reset되지 않으니 통나무·자동차 빈틈 대기 시 5초 안에 사망.

**원인**: 디자이너 의도(전진 강제)와 플레이어 의도(정당한 회피 행동) 충돌.

**해결**: 좌우 입력도 idle reset. 무한 후퇴는 별도 5그리드 제한으로 차단. 두 메커니즘을 분리.

### 도전 6: 청크 무작위 vs 거리 곡선 충돌 (해결 — 통합 설계)

**증상**: Step 9 계획(거리 기반 점진 증가)과 사용자 요청(청크별 무작위 다양성)이 동시 적용 시 곱셈 폭발 가능.

**해결**: `BalanceCurve.Combined()` 단일 함수로 통합. 거리 base × variance 곱 결과를 `Clamp(0.7, 1.6)`. 두 시스템이 단일 multiplier로 합류하므로 정합성 자동 확보.

---

## 9. 코드 품질 지표

| 항목 | 수치 / 방식 |
|---|---|
| 총 C# 파일 수 | 약 35개 (UI 추가 후) |
| 스크립트 총 라인 | 약 2,000줄 |
| GC Alloc (프레임 루프) | `GridPosition` struct + `_despawnBuffer` 재사용으로 핵심 경로 0 alloc |
| 의존성 주입 방식 | SerializeField + 이벤트 (UnityEngine.Events 미사용, C# event 직접) |
| 데이터 / 로직 분리 | ScriptableObject 5종 + `BalanceCurve` 정적 클래스 |
| 네임스페이스 구조 | `VoxelRoad.World`, `.River`, `.Vehicles`, `.Rail`, `.Game`, `.CameraSystem`, `.UI` |
| 주석 언어 | 한국어 (1인 개발 / 한국어 통일) |
| 테스트 자동화 | 없음 (CLAUDE.md 명시 — Manual play-test only) |
| 디버깅 도구 | unity-cli connector — 외부에서 콘솔·런타임 C# 실행 |

---

## 10. 디자인 의사결정 원칙 (회고)

이 프로젝트를 진행하며 반복적으로 적용한 의사결정 원칙들:

1. **과잉설계 금지**: 인터페이스·추상 클래스·디자인 패턴은 실제로 두 번째 구현체가 생길 때만 도입. `BaseLane`은 4종 lane이 실재하므로 정당, 반면 단일 매니저는 인터페이스 없이 직접 참조.

2. **데이터-로직 분리**: 모든 튜닝 수치는 ScriptableObject로 빼냄. 코드는 알고리즘만 담당.

3. **점진적 검증**: 한 번에 한 시스템만 추가하고 즉시 플레이 테스트. Step 7 압박 메커니즘이 3번 재설계된 것은 실패가 아닌 **검증 기반 진화**의 결과.

4. **사용자 통찰 채택**: 디렉터 의견과 다르더라도 플레이어 관점이 더 옳을 때가 있음. "좌우 흔들기 회피"는 디자이너 의도 위반이지만 플레이어 입장에선 정당한 회피 행동 — 결국 채택.

5. **인간 한계 보호**: 거리 곡선이 무한히 증가하면 후반에 회피 불가. Plateau 1.6 + Clamp는 게임이 게임답기 위한 최후의 안전망.

6. **명시적 사망 원인**: `DeathReason` enum (Vehicle / Drown / OutOfBounds / Train / Idle)으로 죽은 이유를 명확히 구분. 게임오버 UI에서 추후 차별화 표시 가능 (Step 10 이후 옵션).

---

*이 문서는 Voxel Road 개발 과정 전체를 기술적 관점과 기획적 관점에서 통합하여 기술한 설명서입니다. 2026-04-28 Step 9 완료 시점 기준.*
