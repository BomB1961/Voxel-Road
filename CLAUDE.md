# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

No CLI build scripts exist. All builds are manual via Unity Editor:

- **Play**: Open `Assets/Scenes/Game.unity` → press Play in Editor
- **Android build**: File → Build Settings → Android → Build
- **Restart in-game**: Press `R` key (handled in `GameManager.cs`)
- **Testing**: Manual play-test only — no automated test runner configured


# CLAUDE.md — 개발 규칙

> Unity 6000.4.x / URP / Android Portrait · C# · 1인 개발 · 모바일 엔드리스 아케이드 러너.
> 목표: 게임이 정상적으로 실행·구동·완성되는 것. 아트 확장성은 고려 대상 아님.

---

## 핵심 철학

- 과잉설계 금지. 지금 당장 필요한 것만 만들 것. "나중에 필요할지도 모르는" 추상화를 미리 만들지 말 것.
- 단순함이 기본값. 본질적으로 복잡한 문제는 복잡하게 풀되, 그 복잡함을 한 곳에 가둘 것.
- 주석 없이도 읽히는 코드를 목표로 할 것. 이름·구조·타입으로 의도를 드러낼 것.
- "나중에 누군가에게 설명할 수 있는가"를 설계·구현의 기준으로 삼을 것.
- 게임이 정상 구동되는 것이 최우선. 구조가 예뻐도 버그가 있으면 실패.

---

## 작업 방식

- 작업 전 Plan(수정/생성 파일 목록 · 각 파일의 책임 · 사용자가 에디터에서 할 일 · 검증 방법)을 먼저 제시할 것.
- Plan 사용자 승인 전에 코드 작성을 시작하지 말 것.
- **이미 구현된 코드를 변경·수정할 때는 반드시 사전에 변경 범위·사유·영향 파일을 제시하고 사용자 승인을 받은 후 진행할 것.** 버그 픽스·리팩토링·이름 변경·시그니처 변경 모두 포함. 새 파일 추가는 Plan 승인으로 대체 가능하지만 기존 파일 수정은 별도 승인 필요.
- **사용자 요청이 모호하거나 해석 여지가 있으면 임의로 추론해 작업을 시작하지 말 것.** 이해한 내용을 사용자에게 먼저 되물어 확인한 뒤 진행할 것. 확인 형식: "이 요청을 ○○로 이해했습니다. 맞습니까?" 또는 "A/B 두 가지 해석이 가능합니다 — 어느 쪽입니까?" 애매한 대명사("그거", "저번 그거"), 범위 불명확("전부", "적절히"), 복수 해석 가능 명사가 있으면 항상 확인 우선.
- 한 번에 한 가지 기능만 구현할 것. 여러 시스템을 동시에 손대지 말 것.
- 한 작업 단위는 "컴파일 성공 + 에디터에서 검증 가능한 최소 단위"일 것.
- 기존 유사 코드가 있으면 3~5개 훑어본 뒤 설계할 것. 기존 패턴과 어긋나는 새 패턴을 도입하지 말 것.
- 확실하지 않은 API나 Unity 메서드를 추측으로 쓰지 말 것. 사용자에게 묻거나 공식 문서를 확인할 것.
- 컴파일 에러가 남은 상태로 다음 파일로 넘어가지 말 것.
- 기능이 추가·변경되면 관련 설명서(Explain.md 등)를 함께 갱신할 것.
- 검증 없이 "될 것 같다"로 완료 처리하지 말 것. 사용자에게 에디터 플레이 검증을 요청할 것.
- 미구현 기능을 완료로 보고하지 말 것. TODO는 `// TODO:` 주석과 함께 명시적으로 보고할 것.
- 한 커밋 = 한 논리적 변경. 여러 작업을 한 커밋에 섞지 말 것.
- Unity 에디터는 Claude가 직접 조작하지 말 것.

---

## 에디터 작업 안내

- 각 구현 완료 후 Unity 에디터에서 사용자가 해야 할 작업을 **클릭 순서 중심**으로 제시할 것.
- "컴포넌트를 추가하세요" 같은 모호한 안내 금지. "Hierarchy에서 Player 오브젝트 선택 → Inspector 하단 Add Component 클릭 → 검색창에 PlayerController 입력 → 결과 클릭" 처럼 구체적으로 쓸 것.
- 메뉴 경로는 `>` 구분자로 쓸 것(`Assets > Create > ScriptableObject > LaneConfigSO`).
- 드래그앤드롭이 필요하면 출발지와 도착지를 명시할 것("Project 창의 PlayerPrefab을 Hierarchy의 GameManager 오브젝트 Inspector의 _playerPrefab 슬롯에 드래그").
- 값 입력이 필요하면 필드명과 입력값을 정확히 쓸 것("_jumpDurationSeconds 필드에 0.12 입력").
- 하나의 작업 단계는 한 줄로, 순서는 번호로 매길 것.
- 안내 끝에 "완료되면 Play 버튼을 눌러 ○○가 정상 동작하는지 확인해주세요" 같은 검증 방법을 붙일 것.

---

## 과잉설계 방지

- 인터페이스는 실제로 구현체가 2개 이상 생길 때만 만들 것. 한 구현체에 미래를 위한 인터페이스를 씌우지 말 것.
- 추상 클래스는 실제로 상속받는 클래스가 생길 때만 만들 것.
- 싱글턴은 기본적으로 쓰지 말 것. 씬에 하나만 존재하는 매니저는 `[SerializeField]` 참조로 연결할 것.
- 싱글턴이 정말 필요하다고 판단되면 사용자에게 이유를 설명하고 승인받을 것.
- DI 컨테이너(Zenject 등)를 도입하지 말 것.
- FSM(Finite State Machine) 라이브러리를 도입하지 말 것. `enum` + `switch`로 충분함.
- 디자인 패턴 이름을 근거로 설계하지 말 것. 문제가 먼저, 패턴은 그 해결책일 뿐.
- 3번 이상 반복되기 전에는 공통화·추상화하지 말 것. 두 번까지는 복붙이 낫다.
- 한 번밖에 안 쓸 헬퍼 클래스·유틸리티를 만들지 말 것.
- "나중에 확장하려고" 매개변수·필드·옵션을 미리 추가하지 말 것.

---

## 아키텍처

- 한 MonoBehaviour = 한 역할. 이름으로 그 역할이 드러날 것.
- "~Manager" 이름으로 여러 책임을 묶지 말 것. 관리 책임 하나만 있을 때만 Manager라 부를 것.
- 클래스 간 통신은 두 가지 중 하나로만 할 것: `[SerializeField]` 참조 주입, 또는 C# event 구독.
- `UnityEvent`를 쓰지 말 것. C# event로 대체할 것.
- `FindObjectOfType`·`GameObject.Find`를 쓰지 말 것. SerializeField 또는 이벤트로 대체할 것.
- 구체 클래스에 의존하지 말 것. 인터페이스가 있으면 인터페이스에 의존할 것.
- 프리팹 생성은 전용 스포너(또는 팩토리) 클래스를 경유할 것. 로직 클래스가 `Instantiate`를 직접 호출하지 말 것.
- 게임 로직(충돌·사망·이동 판정)은 float 좌표가 아닌 정수 그리드 좌표 기준으로 할 것.
- 정수 그리드 좌표는 `readonly struct`로 정의할 것. GC 부담을 피하기 위함.
- 비주얼 위치(`transform.position`)와 논리 위치(그리드)를 명확히 분리하고, 변환은 명시적 메서드를 통해서만 할 것.
- 새 기능을 구현할 때 네임스페이스를 부여할 것. 전역에 클래스를 풀어놓지 말 것.

---

## 타입 설계

- 원시값 여러 개를 함께 쓰는 개념은 의미 타입(struct/class)으로 감쌀 것(`int x, int z` → `GridPosition`).
- 유한한 값(타입·방향·상태)은 `enum`으로 정의할 것. `int`나 `string`으로 대체하지 말 것.
- 튜닝 수치(속도·간격·임계값·확률 등)는 코드 리터럴이 아닌 ScriptableObject에 넣을 것.
- 알고리즘 불변 상수는 `private const`로 이름을 부여할 것. 매직 넘버를 코드에 직접 박지 말 것.
- 예외: `Mathf.PI`, `Time.deltaTime`, `0`, `1` 같은 자명한 값은 그대로 써도 됨.
- 함수 매개변수가 4개를 초과하면 구조체로 묶을 것.
- 반환값이 "없을 수 있음"은 null이 아닌 `bool Try*(..., out T)` 패턴으로 표현할 것.
- public 필드를 만들지 말 것. `[SerializeField] private` 또는 프로퍼티를 쓸 것.

---

## 네이밍

- 클래스·메서드·프로퍼티·상수는 `PascalCase`.
- 로컬 변수·매개변수는 `camelCase`.
- private 필드는 `_camelCase` (언더스코어 접두).
- SerializeField 필드도 `_camelCase` + `[SerializeField]`.
- 불리언은 질문문 형태(`isMoving`, `canJump`, `hasLanded`).
- 부정형 이름을 쓰지 말 것(`isNotReady` 금지 → `isReady` 또는 `isPending`).
- 메서드는 동사로 시작(`MovePlayer`, `SpawnEnemy`).
- bool 반환 메서드는 `Try`/`Is`/`Has`/`Can` 접두어.
- 축약 금지(`plr`, `cfg`, `mgr` 금지 → `player`, `config`, `manager`).
- 단위가 애매한 이름을 피하고 단위를 이름에 포함할 것(`delay` → `delaySeconds`, `threshold` → `thresholdPixels`).
- 월드 좌표와 논리 좌표를 이름으로 구분할 것(`worldX` vs `gridX`).
- 이름은 한 줄 문장처럼 읽힐 것(`Process()`, `Handle()` 같은 모호한 동사 금지).

---

## 함수 · 구조

- 한 함수 안에 서로 다른 추상화 수준을 섞지 말 것.
- 함수 본체가 20줄을 넘으면 분할을 고려할 것. 단, 억지 분할로 파편화시키지 말 것.
- 들여쓰기가 3단을 넘으면 분할 또는 이른 반환으로 평탄화할 것.
- 중첩 `if`보다 이른 반환(early return)을 우선할 것.
- 한 식에 여러 연산을 몰아넣지 말 것. 중간 변수에 이름을 부여해 단계를 드러낼 것.
- 함수는 한 가지 일만 할 것. "처리하고 동시에 저장까지" 같은 이중 책임을 피할 것.
- 부수효과가 있는 함수는 이름으로 그것을 드러낼 것(`GetScore`는 순수 조회, `UpdateScore`는 변경).

---

## 주석

- 주석은 예외적으로 쓸 것. 기본은 이름과 구조로 의도를 드러내는 것.
- "무엇을 하는지"를 주석으로 설명하지 말 것.
- "왜 이렇게 했는지"만 주석으로 남길 것.
- 코드가 이미 말하는 내용을 반복하지 말 것(`count++; // 1 증가` 금지).
- 수치 리터럴이 불가피하면 그 값의 근거를 주석으로 남길 것.
- 비직관적 트릭·우회책이 있다면 그 배경을 주석으로 남길 것.
- 주석 언어는 한국어로 통일.

---

## 게임플레이 품질

- 사용자 입력은 받은 프레임에서 즉시 처리할 것. 다음 프레임으로 미루지 말 것.
- 입력을 무시해야 할 때(이동 중 등)는 무시하지 말고 1개까지 버퍼링해서 다음 틱에 처리할 것.
- 게임 오브젝트는 카메라 시야 밖으로 일정 거리 이상 벗어나면 파괴(또는 풀로 반환)할 것. 메모리에 무한히 쌓이게 두지 말 것.
- 사망·게임오버 판정은 로직 좌표(그리드) 기준으로 할 것. 물리 충돌 이벤트에 의존하지 말 것.
- 게임 상태는 `enum GameState`(Menu/Playing/GameOver 등) 하나로 관리할 것.
- 프레임률은 60fps 목표. `Application.targetFrameRate = 60`을 진입 시 1회 설정할 것.

---

## Unity · 성능

- `Update`/`FixedUpdate`/`LateUpdate` 경로에서 GC 할당을 유발하는 코드를 쓰지 말 것.
- 매 프레임 경로에서 `new List<>()`·`new T[]`·`new` 참조 타입을 하지 말 것.
- 매 프레임 경로에서 문자열 `+`·`string.Format`·문자열 보간(`$""`)을 쓰지 말 것.
- 매 프레임 경로에서 LINQ(`Where`, `Select`, `ToList` 등)를 쓰지 말 것.
- 재사용 가능한 버퍼는 필드로 선언하고 `Clear()` 후 재사용할 것.
- `GetComponent<T>()`·`Camera.main`은 매 프레임 호출하지 말 것. `Awake`/`Start`에서 캐싱할 것.
- `Instantiate`/`Destroy`는 매 프레임 루프에서 호출하지 말 것. 이벤트 시점에만 쓸 것.
- 반복 생성되는 프리팹은 실제 성능 문제가 관찰된 이후 오브젝트 풀링을 도입할 것.
- `Physics.OverlapBox`·Raycast는 매 프레임 수십 회 호출하지 말 것. 필요 시점에만 쓸 것.
- `Debug.Log`는 `#if UNITY_EDITOR` 가드 또는 `[Conditional("DEBUG_VERBOSE")]`로 감쌀 것.
- SerializeField 필드는 `Awake()`에서 null 체크할 것. null이면 `Debug.LogError` 출력하고 `enabled = false`로 자기 비활성화할 것.

---

## 외부 의존

- 새 외부 패키지(DOTween, UniRx, FSM 라이브러리 등)를 임의로 추가하지 말 것. 사유와 대안을 먼저 제시하고 승인받을 것.
- Unity 기본 기능으로 해결 가능한 문제는 기본 기능으로 해결할 것.
- 에셋 스토어 코드를 검증 없이 프로젝트 핵심 로직에 편입시키지 말 것.

---

*이 규칙은 프로젝트 초기 단계 기준이다. 규모가 커지거나 요구가 바뀌면 규칙도 함께 갱신한다.*