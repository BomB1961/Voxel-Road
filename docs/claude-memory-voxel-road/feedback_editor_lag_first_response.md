# 에디터 Lag 1순위 대응: 재시작

## 규칙

Unity 에디터에서 Play 모드 lag·hitch·이상 동작 보고가 들어오면 **코드 측정·수정 단계로 가기 전에 먼저** 다음을 시도:

1. 작업 저장 (Ctrl+S)
2. Unity 에디터 완전 종료
3. 다시 실행
4. 동일 시나리오로 lag 재현 시도
5. **재시작 후에도 lag 지속될 때만** Profiler·코드 instrumentation 단계로 진입

## 근거 (2026-05-05 세션 두 번 검증)

같은 세션 안에서 두 번 발생:

1. **첫 번째**: GPU Instancing·머티리얼 임포트 변경 직후 URP 셰이더 어레이 사이즈 경고 spam
   (`urp_ReflProbes_BoxMin/BoxMax`, `_AdditionalShadowParams` 64 vs 32)
   → 매 프레임 6+ 경고 → 메인 스레드 stall → 에디터 재시작 후 어레이 재할당으로 해소

2. **두 번째**: 사망 모션·중간 hitch 보고 → Profiler·코드 instrumentation plan 까지 만들었으나
   사용자가 에디터 재시작 → lag 사라짐 → 측정 작업 자체 불필요해짐

## 흔한 누적 원인

- 에디터 메모리 단편화 (장시간 작업 후)
- 셰이더/에셋 캐시 inconsistency
- Profiler·Console 잔류 데이터
- 도메인 리로드 후 dangling reference
- 머티리얼·임포트 설정을 여러 번 수정 → 캐시 꼬임
- URP RP Asset 의 어레이 사이즈가 첫 할당 후 fixed → 변경 시 Unity 재시작 필요

## 예외

- WebGL·Android 빌드된 결과물에서 lag 발생 → 에디터 재시작 무의미. 곧장 코드 측정 단계로.
- 에디터 재시작해도 lag 지속 → 진짜 코드 문제. Profiler 정공법.

## 효과

- 5분 측정·수정 작업을 하기 전 30초 재시작 한 번이 모든 추측을 무효화할 수 있음
- 사용자 시간·문맥 절약
