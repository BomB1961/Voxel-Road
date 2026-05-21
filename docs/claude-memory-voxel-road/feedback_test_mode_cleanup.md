# 테스트 모드 클린업 규칙

## 핵심 원칙
**테스트 모드 관련 구현은 일시적이다. 사용자가 "테스트 모드 삭제" 또는 유사 요청을 하면 관련 코드·참조·씬 데이터를 전부 제거해야 한다. 원 게임 기획·설계에 영향이 가면 안 된다.**

## 등록 시점
2026-04-29 사용자 지시. HUD 신기록 배너 테스트를 위해 `TestMode` 컴포넌트 + WorldGenerator 패치 도입 후 명시.

## 클린업 시 삭제 대상

### 파일 (삭제)
- `Assets/Scripts/Common/TestMode.cs` (+ .meta)
- 향후 추가될 모든 `TestMode*` / `*TestMode*` 명명 파일

### 코드 패치 (원복)
- `Assets/Scripts/World/WorldGenerator.cs`
  - `using VoxelRoad.Common;` 제거 (Common 네임스페이스 다른 사용 없으면)
  - `ChooseNextChunkType()` 첫 줄 `if (TestMode.ForceGrassOnly) return LaneType.Grass;` 제거

### 씬 데이터 (제거)
- `Assets/Scenes/Game.unity` 의 TestMode 컴포넌트 인스턴스
  - 사용자가 Hierarchy에서 직접 비활성·삭제하거나 빌더 메뉴로 정리

### PlayerPrefs (잔존 클린업)
- `VoxelRoad.BestScore` 키가 테스트 시드값(1 등)으로 남아있을 수 있음
- 클린업 시 사용자에게 "PlayerPrefs 초기화 원하세요?" 확인 후 `PlayerPrefs.DeleteAll()` 또는 해당 키만 `DeleteKey`

## 검증 체크리스트 (클린업 완료 후)
1. `grep -r "TestMode" Assets/Scripts/` → 결과 없음 (네임스페이스 포함)
2. `grep -r "ForceGrassOnly" Assets/Scripts/` → 결과 없음
3. 컴파일 에러 없음
4. Play → 청크가 정상적으로 Grass/Road/River/Rail 다양하게 스폰되는지 확인
5. 첫 플레이(BestScore PlayerPrefs 미존재) 시 배너 미발동 확인 (원 사양 복귀)
6. 게임 매뉴얼/문서(Explain.md 등)에 TestMode 흔적 없는지 확인

## Why
테스트 편의를 위한 디버그 토글이 운영 빌드에 새어 나가면 게임 디자인의 핵심(다양한 청크 스폰·신기록 가드 로직)이 깨진다. "잠시 켜둔 테스트 플래그가 출시 빌드에 남아 사고가 나는" 패턴은 1인 개발에서 특히 자주 발생하므로 명시적 클린업 항목으로 관리한다.

## 추가 시 갱신
새로운 테스트 전용 구현(테스트 컴포넌트, 매크로, 디버그 메뉴 등) 추가 시 이 파일 "클린업 시 삭제 대상" 섹션에 항목 추가.
