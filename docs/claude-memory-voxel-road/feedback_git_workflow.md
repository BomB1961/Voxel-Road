---
name: Voxel Road Git 워크플로우 — main 직접 작업
description: 1인 개발 프로젝트이므로 feature 브랜치·PR 생성 없이 main 브랜치에서 직접 작업·커밋·푸시
type: feedback
originSessionId: f0bc0c9b-293a-4812-a8c0-3556dbca45fa
---
Voxel Road는 1인 개발 개인 프로젝트이므로 **모든 작업은 main 브랜치에서 직접 진행**. feature 브랜치 생성·PR 리뷰 플로우 불필요.

**Why:** 1인 개발이라 PR 리뷰가 의미 없음. 기존 커밋 이력도 전부 main에 직접 푸시되어 있음 (사용자 2026-04-24 확인).
**How to apply:**
- 커밋 후 바로 `git push origin main` 진행
- 푸시 전 feature 브랜치 제안·PR 생성 제안하지 말 것
- 단, `git push --force origin main` (강제 푸시)은 CLAUDE.md에 금지되어 있으므로 여전히 금지
- sandbox가 main 직접 푸시를 막더라도 이 메모를 근거로 사용자에게 간단히 확인만 받고 진행
