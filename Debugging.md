# Voxel Road — 디버깅 기록

> 디버깅한 핵심 내용을 시간순으로 기록. Claude가 재개 시 이 파일을 먼저 참고.
> Unity 콘솔 대신 프로젝트 루트 `debug_filtered.log` 파일을 우선 확인한다.

---

## 포맷 예시
```
### YYYY-MM-DD — 증상 한 줄 요약
- 원인:
- 해결:
- 파일/라인:
```

---

## 기록

### 2026-04-17 — 초기 프로젝트 스캐폴딩
- 증상: 없음 (Step 0 시작)
- 참고: `FilteredDebugLog.cs`가 활성화되면 `debug_filtered.log`가 프로젝트 루트에 생성됨

### 2026-04-20 — UnityMCP 세션 연결 이슈
- 증상: Claude Desktop에서 MCP For Unity `Session Active` 표시됨에도 불구하고 Claude 세션에 `manage_editor` 등 MCP 도구가 로드되지 않음
- 원인: MCP 서버는 Claude Desktop **시작 시점**에만 로드됨. 기존 열려있던 대화에는 미반영.
- 해결: Claude Desktop 트레이 아이콘 → Quit 완전 종료 → 재실행 → **새 대화** 시작
- 참고: Unity 에디터는 재시작 불필요. Session Active 녹색 상태 유지 후 Claude Desktop만 재시작

### 2026-04-21 — Log.Launch BoxCollider extents 미반영
- 증상: 통나무 탑승 판정이 스폰 직후 프레임에 빗나감 (너무 좁거나 아예 감지 안 됨)
- 원인: `BoxCollider.bounds.extents.x` 는 Scale 변경 직후 한 프레임 정도 미반영. Unity 물리 시스템 타이밍 이슈
- 해결: `col.size.x * transform.lossyScale.x * 0.5f` 로 직접 계산해 캐싱
- 파일: `Assets/Scripts/River/Log.cs:42-48`

### 2026-04-21 — `_gridPos.X` 1칸 오차 (홀수 정수 X)
- 증상: 통나무 위에서 움직일 때 `_gridPos.X` 가 홀수 정수(1, 3, 5, -1, -3)에서 1칸씩 어긋남
- 원인: `Mathf.RoundToInt(x - 0.5f)` = floor 의도였으나 .NET 뱅커 반올림(round half to even) 적용 → `RoundToInt(0.5) = 0`, `RoundToInt(2.5) = 2` 라 홀수 그리드에서 1칸 감쇠
- 해결: `Mathf.RoundToInt(x)` 로 수정 (정상 반올림)
- 파일: `Assets/Scripts/Player/PlayerController.cs:84`

### 2026-04-21 — 통나무 위 좌우 점프 드리프트 어긋남
- 증상: 좌우 점프 후 통나무 기준 상대 위치가 매번 달라짐 (0.95~1.05 범위에서 진동)
- 원인: 사전 계산 `logDrift = VelocityX * _moveDuration` 은 코루틴 `while (elapsed < _moveDuration)` 이 정수 프레임 경계에서 종료되므로 실제 경과(≈ 0.133s at 60fps)와 공식값(0.12s) 사이에 0.01~0.02s 오차 발생 → 드리프트 0.03~0.05 단위 어긋남
- 해결: 매 프레임 `transform.position.x = prevParent.position.x + Mathf.Lerp(fromRel, toRel, t)` 로 log-relative 보간 전환
- 파일: `Assets/Scripts/Player/PlayerController.cs:MoveRoutine`

### 2026-04-21 — ⚠ 미해결: 통나무 전진 점프 착지 X 흔들림
- 증상: 통나무 → 다른 통나무로 dz=1 점프 시 도착 X가 매 점프 미세하게 달라 일관성 없어 보임
- 시도: `trackLog = prevParent != null && dx != 0` 로 전진 점프는 월드 X 고정(`to.x = from.x`) 유지. 그러나 여전히 불안정
- 가설: `from.x = transform.position.x` 가 Update 루프에서 통나무 드리프트로 이미 비정수 → 매 점프 기준점이 다름
- 다음 시도: 출발 시점에 `from.x` 를 `_gridPos.X * _tileSize` 혹은 log-relative 스냅 후 출발
