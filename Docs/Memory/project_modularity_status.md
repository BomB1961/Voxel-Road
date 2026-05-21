---
name: Voxel Road 모듈화 현황 평가
description: 모듈화 리팩토링 후 평가. 2026-05-04 시점 — 우선순위 후보 3종 모두 해결됨
type: project
originSessionId: f0bc0c9b-293a-4812-a8c0-3556dbca45fa
---
2026-05-04 기준 평가: **모듈화 리팩토링 3종 완료. 유지보수 난이도 한 단계 낮아짐.**

**Why:** Step 0~11 완성 후 잔여 모듈화 작업을 한꺼번에 처리. 추후 신기능 추가 시 깔끔한 진입점 확보.
**How to apply:** 새 기능 추가 시 아래 "잘 된 부분" 패턴 따를 것. 새로운 약점이 보이면 기록을 갱신할 것.

## 잘 된 부분
- 10개 도메인 폴더·네임스페이스 분리 (`Player/World/River/Rail/Vehicles/Camera/Input/Data/Game/Common`)
- `ILane → BaseLane → {Grass/Road/River/Rail}Lane` 레인 확장성 (Step 6 RailLane 추가로 실증됨)
- ScriptableObject 5종으로 튜닝 수치 완전 분리
- 싱글턴 제거·C# event·DeathReason enum 리팩토링 완료 (커밋 6e08a98)
- `GridPosition` struct로 논리/비주얼 좌표 분리
- **Player 모듈 분리** (`0cc848e`): PlayerController(328줄) → Movement/LogRider/DeathTriggers 3종 자식 컴포넌트. PlayerController 는 그리드 상태·이벤트 라우팅만.
- **Spawner 베이스 추출** (`5ab1849`): `PeriodicLaneSpawnerBase<TConfig>` (Common 네임스페이스) → VehicleSpawner/LogSpawner 둘 다 상속. 추상 hook 6종으로 차이만 override.
- **Log 탑승 진입점 통일** (`5ffce42`): `Log.TryAttachPassenger` 한 곳으로 두 경로(트리거/착지) 모두 통과. _passenger 등록 보장.

## 의도적 미통합 (베이스에 끼우면 추상 약화)
- **TrainSpawner** — 상태머신(경고→스폰→대기)·다차량 편성으로 주기 스폰 패턴과 본질이 달라 `PeriodicLaneSpawnerBase` 미상속. 4번째 주기 스포너가 추가되면 그때 재검토.

## 취약 부분 (잔여)
- 현재 명확한 약점 없음. 새 기능 추가 시 재평가.
