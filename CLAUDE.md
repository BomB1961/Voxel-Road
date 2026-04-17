# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Voxel Road** is a Unity 6 game project (version 6000.4.0f1) targeting Windows 64-bit Standalone. It uses the Universal Render Pipeline (URP) and Unity's New Input System.

## Unity & Build

- Open in Unity 6000.4.0f1 or later
- Build via **File > Build Settings** — only `SampleScene` is in the build
- Play testing: use **Play Mode** in the Editor
- No CLI build scripts are present; builds are done through the Unity Editor UI

## Architecture

### Current State
The project is a **foundation/template** — no custom C# gameplay scripts exist yet. All game logic is yet to be written.

### Key Systems in Place

**Input System** (`Assets/InputSystem_Actions.inputactions`):
Defines two action maps:
- `Player`: Move, Look, Attack, Jump, Crouch, Sprint, Interact (hold), Previous/Next
- `UI`: Standard UI navigation

Control schemes: Keyboard & Mouse, Gamepad, Touch, Joystick, XR. When writing player code, generate a C# class from this asset (the `.inputactions` file) and use `PlayerInput` component or direct `InputAction` references — do not use legacy `Input.*` calls.

**Rendering** (`Assets/Settings/`):
- Separate URP pipeline assets for PC (`PC_RPAsset`) and Mobile (`Mobile_RPAsset`)
- Per-scene post-processing via Volume Profiles (e.g., `SampleSceneProfile.asset`)
- HDR + Linear color space enabled

**Navigation**: `com.unity.ai.navigation` 2.0.11 is installed — NavMesh baking can be done via the Navigation window.

### Packages (key ones)
| Package | Version | Purpose |
|---|---|---|
| `com.unity.inputsystem` | 1.19.0 | Player input |
| `com.unity.render-pipelines.universal` | 17.4.0 | URP rendering |
| `com.unity.ai.navigation` | 2.0.11 | NavMesh / AI |
| `com.unity.timeline` | 1.8.11 | Cutscenes / animation |
| `com.unity.test-framework` | 1.6.0 | Unit tests (EditMode/PlayMode) |

## Conventions to Follow When Writing Code

- Place scripts under `Assets/Scripts/` organized by system (e.g., `Player/`, `UI/`, `Enemies/`)
- Use `[SerializeField]` over `public` fields for Inspector exposure
- Reference input actions via generated C# wrapper from `InputSystem_Actions.inputactions`, not string lookups
- For URP shader/material work, use Shader Graph or URP-compatible shaders only — Built-in RP shaders will appear pink



### [필수 준수 규칙]
- 아래 규칙을 반드시 지켜야 합니다.
- SOLID 원칙을 준수한 OOP 기반 설계를 할 것
- 모든 코드에는 한글 주석을 필수로 포함할 것
- Unity 6.4 내장 기능이 있다면 코드로 직접 구현하지 말고 내장 기능 사용을 우선 검토할 것
- 게임 성능 최적화를 고려하여 코드를 작성할 것 (불필요한 가비지 생성 방지)
- 향후 DI Container 도입을 고려하여 인터페이스 기반으로 느슨한 결합을 유지할 것 (단, 이번 구현에서는 DI 컨테이너 자체를 구현하지는 말 것)
- 모든 구현 작업은 반드시 "구현 계획 제시 → 사용자 승인 → 구현 → 결과 보고" 순서로 진행할 것
- 작업 후에는 Unity 6.4에서 사용자가 직접 수행해야 할 작업을 클릭 순서로 명확히 안내할 것
- 반드시 단계적 상세화 할 것
- 디버깅한 핵심 내용들은 매번 Debugging.md 파일에 기록 저장할 것
- 디버깅 시 Unity 콘솔 대신 프로젝트 루트 `debug_filtered.log` 를 우선 읽을 것 (핵심 1줄 압축, 토큰 절약)
- 터미널을 재실행시에도 어디까지 작업을 했는지 알 수 있도록 어디서부터 어디까지 구현을 했는지 그리고 어디서부터 시작하면 될지에 대한 것을 Memory.md에 기록 저장 할 것
- CLAUDE.md와 베이스 코드 항상 일치
- CLAUDE.md파일 항상 300글자 이내로 유지및 프로젝트에 불필요한 내용 기입 금지, 검토 후 삭제할 것
- 추가되거나 수정된 코드들 기록 저장하는 .md파일 만들어서 관리할 것(코드 상단에 주석처리하여 날짜와 시간을 기록할 것)

### [커밋/푸시 자동 수행 규칙]
사용자가 "커밋", "푸시", "커밋하고 푸시", "commit", "push" 등을 지시하면 Claude는 아래 절차를 그대로 수행한다(별도 승인 없이 바로 실행 가능한 루틴 작업으로 취급):

1. **상태 조사**: `git status`, `git diff`(staged+unstaged), 최근 `git log --oneline -5` 를 병렬 확인
2. **보안 점검**: 스테이징 대상에 `.env`, `credentials.*`, `*.key`, `*.pem`, `Library/`, `Temp/`, `Logs/`, `*.csproj`, `Map.slnx` 등 금칙 경로가 포함되면 **중단 후 사용자에게 경고**
3. **커밋 메시지 작성**: 변경 목적을 1–2문장 한글로 요약. 접두사는 Conventional Commits 사용 — `feat:` 기능 추가, `fix:` 버그 수정, `chore:` 설정·도구, `docs:` 문서, `refactor:` 리팩터링, `test:` 테스트, `perf:` 성능
4. **스테이징**: 변경 파일 경로를 **명시적으로** `git add <path>` (절대 `git add -A` 또는 `git add .` 금지 — 우발 포함 방지)
5. **커밋**: `git commit -m "<메시지>"` (pre-commit 훅 실패 시 훅을 건너뛰지 말고 원인 수정 후 새 커밋 생성)
6. **푸시(사용자가 요청했을 때만)**: `git push` — 현재 브랜치로만. `main`/`master` 에 대한 `--force` push 는 **절대 금지**, 사용자가 명시적으로 요청해도 재확인
7. **결과 보고**: 커밋 해시, 메시지, 푸시 대상 브랜치, 원격 URL을 출력. 실패 시 오류 원문과 복구 방법 안내

예) 사용자: "커밋하고 푸시해줘" → 위 1~7을 한 번의 응답으로 수행 후 결과 보고.

### [/compact 권장 알림 규칙]
컨텍스트 부풀림 방지를 위해, AI 는 **하나의 "구현 단위"가 끝날 때마다** 사용자에게 `/compact` 권장을 한 줄로 알린다.

**"구현 단위" 정의** (아래 중 하나라도 해당):
- Memory.md 나 계획서에 명시된 **하나의 Step**(예: "Step 3 완료")
- 별도 관심사 묶음(예: 패키지 설치 → SO 생성 → 씬 구성 3단계 중 한 단계)
- 대량 파일 생성/수정(5개 이상) 직후
- 긴 빌드/테스트 실행 직후

**출력 형식** (응답 맨 끝에 한 줄):
> 💡 Step N 완료 — 컨텍스트 절약을 위해 지금 `/compact` 권장합니다.

**주의**:
- AI 스스로 `/compact` 를 호출할 수 없음 — 사용자만 실행 가능. 알림이 전부임.
- 토큰 여유가 충분해 보이면 생략 가능. 단, 판단 근거가 모호하면 알리는 쪽으로.
- 한 응답에 한 번만 출력. 단일 Step 내에서 여러 번 반복 알림 금지.


