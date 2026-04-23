# Step 6 에디터 작업 (Rail + Train)

> 코드 구현 완료. 아래 에디터 작업을 이어서 진행하면 Step 6 마무리.

## 1) TrainConfig 에셋 생성
1. Project 창에서 `Assets/Data/` 폴더 선택 (없으면 `Assets` 우클릭 → Create → Folder → "Data")
2. Project 창 빈 공간 우클릭 → Create → VoxelRoad → TrainConfig
3. 생성된 에셋 이름을 `TrainConfig`로 설정
4. Inspector에서 기본값 확인: Speed=15, WarningSeconds=1.5, MinCycleInterval=4, MaxCycleInterval=8, SpawnScale=1, LengthScale=4 (기차 프리팹은 다음 단계에서 연결)

## 2) Train 플레이스홀더 프리팹 생성
1. Hierarchy에서 빈 공간 우클릭 → 3D Object → Cube → 이름 `Train_Placeholder`로 변경
2. Inspector에서 Transform Scale을 (1, 1, 1)로 유지 (SpawnScale/LengthScale이 월드 스케일 담당)
3. Inspector 하단 Add Component → "Train" 검색 → 선택 (`Train.cs` 스크립트가 BoxCollider 자동 요구)
4. Inspector의 Box Collider → Is Trigger 체크박스 활성화
5. (선택) Material을 검정/회색 계열로 교체해서 기차처럼 보이게
6. Hierarchy의 `Train_Placeholder`를 Project 창 `Assets/Prefabs/` 폴더로 드래그 → 프리팹화
7. Hierarchy에서 원본 `Train_Placeholder` 삭제
8. Project 창에서 `TrainConfig` 에셋 선택 → Inspector의 Train Prefabs 배열 Size를 1로 → Element 0 슬롯에 `Train_Placeholder` 프리팹 드래그

## 3) RailLane 프리팹 생성
1. Project 창에서 기존 `RiverLane` 프리팹 복제 (Ctrl+D) → 이름 `RailLane`으로 변경
2. `RailLane` 프리팹 더블클릭해 프리팹 모드 진입
3. 루트 GameObject의 Inspector에서 RiverLane 컴포넌트 우측 톱니바퀴 → Remove Component
4. Add Component → "RailLane" 검색 → 선택
5. 자식 중 `Water` (MeshRenderer)의 이름을 `Track`으로 변경, Material을 어두운 회색/갈색 계열로 교체 (선로 느낌)
6. 자식 `LogSpawner` 이름을 `TrainSpawner`로 변경 → 기존 LogSpawner 컴포넌트 Remove → Add Component → "TrainSpawner"
7. `TrainSpawner` GameObject 자식으로 빈 GameObject 추가 → 3D Object → Cube → 이름 `WarningLight` → Scale (0.3, 0.3, 0.3), Position을 레인 끝쪽(예: X=-8, Y=0.5, Z=0)으로 이동
8. 루트의 RailLane 컴포넌트 Inspector:
   - Track 슬롯 ← 자식 `Track` MeshRenderer 드래그
   - Spawner 슬롯 ← 자식 `TrainSpawner` 드래그
   - Train Config 슬롯 ← Project의 `TrainConfig` 에셋 드래그
9. `TrainSpawner` 컴포넌트 Inspector:
   - Warning Light 슬롯 ← 자식 `WarningLight`의 MeshRenderer 드래그
10. 프리팹 모드 저장 후 나가기

## 4) LaneConfig에 RailLane 연결
1. Project 창에서 `LaneConfig` 에셋 선택
2. Inspector에서 Rail Lane Prefab 슬롯에 `RailLane` 프리팹 드래그
3. Rail Quota는 1, Rail Chunk는 (1, 1) 기본값 유지

## 검증
Play 버튼 → 전진하면서 철길 레인이 간간이 등장하는지, 경고등이 깜빡인 뒤 기차가 X축으로 통과하는지, 기차에 부딪히면 사망(Death Reason: Train)하는지 확인.
