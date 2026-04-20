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

### Step 0 — 스캐폴딩 & 모바일 설정 (진행 중, 2026-04-17)
- [x] 폴더 구조 생성
  - `Assets/Scripts/{Player, World, Game, Input, Data, Camera, Common}/`
  - `Assets/Prefabs/`, `Assets/Materials/`, `Assets/ThirdParty/`
- [x] `Memory.md` 생성
- [x] `Debugging.md` 생성
- [x] `Assets/ThirdParty/README.md` 생성 (CC0 에셋 출처 기록용)
- [ ] **(사용자 작업)** Android Build Target 전환
- [ ] **(사용자 작업)** Portrait 해상도 1080x1920 설정
- [ ] **(사용자 작업)** `Assets/Scenes/Game.unity` 새 씬 생성
- [ ] **(사용자 작업)** Kenney Voxel Pack 다운로드 → `Assets/ThirdParty/KenneyVoxelPack/`
- [ ] Play 실행, 에러 0건 확인

### Step 1 — 스와이프/탭 입력 + 그리드 이동 (대기)
### Step 2 — Cinemachine 쿼터뷰 카메라 (대기)
### Step 3 — Lane 시스템 + WorldGenerator (Grass/Road) (대기)
### Step 4 — 차량 이동 + 충돌 사망 (대기)
### Step 5 — River + 통나무 + 익사/탑승 (대기)
### Step 6 — Rail + 기차 (대기)
### Step 7 — 독수리 타임아웃 사망 (대기)
### Step 8 — 점수 + UI (대기)
### Step 9 — 난이도 커브 + 재시작 플로우 (대기)
### Step 10 — 오디오 일괄 (대기)

---

---

## MCP 연결 정보 (2026-04-20 기준)
- MCP For Unity v9.6.6, Transport: Stdio, Port 6401
- Client: **Claude Desktop** (NOT Claude Code CLI)
- Unity 에디터에서 `Session Active (Voxel Road)` 녹색 확인 필수
- MCP 도구가 로드되지 않을 경우: Claude Desktop **트레이 아이콘 → Quit → 재실행** 후 **새 대화** 시작
- manifest.json에 `com.coplaydev.unity-mcp` Git 패키지 등록 완료

---

## Step 0 재개 시 Claude MCP 실행 항목 (미완료)
다음 항목을 새 세션에서 MCP 도구로 순서대로 실행:
1. `manage_editor` — Android Build Target 전환 (Android Build Support 설치 확인 먼저)
2. `manage_editor` — Player Settings: Default Orientation = Portrait
3. `manage_editor` — Game View 해상도 프리셋 1080x1920 설정
4. `manage_scene` — `Assets/Scenes/Game.unity` 생성 (Main Camera + Directional Light)
5. `manage_editor` — Play 모드 진입 → `read_console` 에러 0건 검증 → Play 종료
6. Memory.md Step 0 체크박스 갱신

---

## 재개 방법
1. 이 파일(`Memory.md`)을 먼저 읽어 마지막으로 완료된 Step 확인
2. 미완료 [ ] 항목이 남은 Step부터 재개
3. 기획서(플랜 파일)를 함께 열어 세부 설계 참고
