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

## 재개 방법
1. 이 파일(`Memory.md`)을 먼저 읽어 마지막으로 완료된 Step 확인
2. 미완료 [ ] 항목이 남은 Step부터 재개
3. 기획서(플랜 파일)를 함께 열어 세부 설계 참고
