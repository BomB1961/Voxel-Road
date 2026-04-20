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
