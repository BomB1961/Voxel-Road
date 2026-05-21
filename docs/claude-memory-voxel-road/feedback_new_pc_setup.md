# 새 PC 동기화 시 수동 셋업 필수 항목

> **상황**: `git pull`로 Voxel-Road를 새 PC에 동기화한 직후 한 번만 필요한 작업. 원본 PC에서 이미 했지만 PC별로 분리 저장되는 항목들이라 새 PC에서 다시 해야 함. 사용자는 원본 PC에서 했던 기억이 흐릿할 수 있으므로 Claude가 먼저 진단·안내할 것.

## 왜 매번 필요한가

다음 항목들은 `.gitignore` 또는 Unity 자체 설계로 **PC별 분리 저장**되어 git에 들어가지 않음:

| 항목 | 저장 위치 | 결과 |
|---|---|---|
| 빌드 타깃 (Android/Standalone) | `UserSettings/EditorUserBuildSettings.asset` | gitignore — 새 PC는 Unity 기본값 `StandaloneWindows64`로 시작 |
| Game 뷰 해상도 프리셋 | EditorPrefs (레지스트리) | 아예 PC 외부 저장 — 새 PC는 프리셋 0개 |
| unity-cli connector 캐시 | `Library/PackageCache/` | gitignore — Unity 첫 로드 시 자동 다운로드 (manifest.json 핀 기준) |

## 새 PC 셋업 체크리스트

`git pull` 직후 Unity 에디터 열기 전·후에 다음을 순서대로 처리:

### 1. unity-cli 글로벌 도구 (PC당 1회)

- `unity-cli --version` 확인 → 없으면 `~/Downloads/unity-cli-auto-install.md` 가이드의 Path A 진행
- `dotnet --list-sdks`로 .NET SDK 8.0+ 확인 → 없으면 https://dotnet.microsoft.com/download 에서 설치
- 둘 다 OK면 Path B 축약 경로

### 2. Unity 빌드 타깃 → Android 스위치 (필수)

1. `File > Build Profiles` (Unity 6) 또는 `File > Build Settings` (Unity 5)
2. 좌측 `Android` 선택 → 우하단 `Switch Platform` 클릭
3. **첫 스위치는 텍스처 재임포트로 5~30분 소요** — 진행률 바 끝까지 대기
4. 검증 (unity-cli):
   ```powershell
   $r = Invoke-RestMethod -Uri "http://127.0.0.1:8090/command" -Method Post -ContentType "application/json" -Body '{"command":"exec","params":{"code":"return UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString();"}}' -TimeoutSec 30
   # 기대: "Android"
   ```

> ⚠️ **순서 중요**: Android 스위치 **먼저**, Game 뷰 프리셋 추가 **나중**. 순서 바꾸면 프리셋이 Standalone 그룹에 들어가 안드로이드 빌드 시 안 보임.

### 3. Game 뷰 해상도 프리셋 추가 (필수)

Android 플랫폼 상태에서 Game 뷰 좌상단 드롭다운 → `+` 버튼:

| Type | Width × Height | Label |
|---|---|---|
| Fixed Resolution | **1080 × 1920** | `Mobile Portrait 1080x1920` ← **메인 작업용** |
| Fixed Resolution | 1080 × 2400 | `Phone 9:20 (1080x2400)` ← 검증용 (현행 플래그십) |
| Fixed Resolution | 1080 × 2340 | `Phone 9:19.5 (1080x2340)` ← 검증용 (선택) |

**메인은 반드시 1080×1920**. 카메라가 이 비율(`aspect=0.5625`)로 튜닝되어 있고 9:16이 가장 좁아 더 긴 폰에선 자동으로 잘 보이기 때문.

### 4. 검증 (unity-cli로 즉시 가능)

```powershell
$code = 'var c = UnityEngine.Camera.main; return $"screen={UnityEngine.Screen.width}x{UnityEngine.Screen.height} aspect={c.aspect:F4} size={c.orthographicSize}";'
$body = @{ command = "exec"; params = @{ code = $code } } | ConvertTo-Json
Invoke-RestMethod -Uri "http://127.0.0.1:8090/command" -Method Post -ContentType "application/json" -Body $body -TimeoutSec 30
```

기대: `screen=1080x1920 aspect=0.5625 size=6` — 모두 일치하면 셋업 완료.

## 사용자 보고 시 주의

사용자가 "원본 PC에서는 이렇게 안 한 거 같다"고 말하는 것은 정상 — **PC당 1회 작업이라 오래되면 기억에 남지 않음**. "원본 PC에서 과거에 1회 해 두었고, 그 후로 안 건드린 것"이라고 이해시킬 것. 의심·번복하지 말 것.

## Why

- 모바일 게임은 **개발 시작부터 Android 플랫폼에 두고 작업**해야 텍스처 메모리·셰이더 호환성 문제를 일찍 발견. PC에서 잘 돌다 빌드 직전 모바일에서 깨지는 사고 방지.
- Game 뷰 프리셋이 **Android 그룹에 등록되어 있어야** 빌드 시뮬레이션·QA에서 의미 있음.
- 카메라가 9:16 비율로 튜닝됐기 때문에 다른 비율을 메인으로 쓰면 차로 가시 범위·플레이어 위치·ChasingWall 거리가 모두 어긋남.
