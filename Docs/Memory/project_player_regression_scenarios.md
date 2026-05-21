---
name: Player 모듈 회귀 테스트 13 시나리오
description: PlayerController/Movement/LogRider/DeathTriggers 분할 검증용. Player 코드 변경 시 매번 돌릴 수동 회귀 테스트 체크리스트
type: project
originSessionId: b834b12b-fef6-4d39-9b5f-3dbc7166da82
---
Player 모듈(PlayerController, PlayerMovement, PlayerLogRider, PlayerDeathTriggers) 코드를 변경했을 때 Play 모드에서 순차 검증할 시나리오.

**Why:** Phase 1 분할(PlayerController 326→오케스트레이터로 슬림화)에서 정한 골든 패스 + 엣지 케이스. Phase 2(스포너 기저 클래스)·Phase 3(통나무 탑승 통합) 진행 시도 동일한 시나리오로 회귀 검증 필요.

**How to apply:** Player 관련 파일을 수정·리팩토링한 후 사용자에게 이 13개 항목 순차 검증을 요청. 사용자가 OK/NG 보고 → NG면 해당 시나리오만 디버그 → 전 항목 PASS 시 커밋.

## 검증 이력
- 2026-05-04: Phase 1(`0cc848e` Player 모듈 분리) 직후 13/13 풀 통과
- 2026-05-04: Phase 2(`5ab1849` Spawner 베이스 추출) + Phase 3(`5ffce42` Log 진입점 통일) + 인근 변경(`295d9b5` X 한계 ±20, `582d4e3` 통나무 OOB 카메라 시야 기준) 후 영향권 7종(#4·#7·#9·#10·#11·#12·#13) 재검증 통과. 비영향 6종(#1·#2·#3·#5·#6·#8)은 Phase 1 통과 결과 유효

## 기본 이동 (1-5)
1. **4방향 이동**: ↑↓←→ 입력 시 점프 애니메이션 + 즉시 시선 회전
2. **z=0 후퇴 차단**: 시작 위치에서 ↓ 입력해도 이동 안 함
3. **후퇴 제한**: MaxZ 도달 후 5칸 이상 ↓ 차단
4. **좌우 경계 차단**: 맵 가장자리에서 더 이상 좌/우 못 감 (현재 기준은 PlayerMovement._playableHalfSpan=20 — cube 마커 ±23 안쪽에서 좌·우 각 3셀 여유. 커밋 295d9b5에서 ±25 → ±20 변경)
5. **장애물 차단**: 잔디 레인의 나무·바위 셀로 점프 시 차단

## 사망 (6-9)
6. **Idle 사망**: 5초 동안 입력 없으면 자동 사망 (DeathReason.Idle)
7. **차량 충돌 사망**: 도로에서 차에 치이면 사망 (DeathReason.Vehicle)
8. **기차 충돌 사망**: 레일에서 기차에 치이면 사망 (DeathReason.Train)
9. **익사**: 강 위에 통나무 없는 셀로 점프 시 사망 (DeathReason.Drown)

## 통나무 탑승 (10-12)
10. **통나무 탑승 + 좌우 점프**: 통나무 위에서 ←→ 점프 시 통나무 드리프트 추적 (자석처럼 끌려가는 느낌 X)
11. **통나무 위 전후 점프**: ↑↓ 점프 시 월드 X 고정 (통나무가 흘러도 도착 X 불변)
12. **통나무 OOB 익사**: 통나무가 맵 경계까지 흘러가면 자동 익사 (DeathReason.Drown)

## 연출 (13)
13. **사망 애니메이션**: 차량/기차 충돌 시 PlayerDeathAnimator가 LastImpactSource 방향으로 흡착·견인 모션 재생
