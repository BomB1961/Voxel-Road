# CLAUDE.md
Voxel Road · Unity 6000.4.3f1 URP · Crossy Road 모작
기획서: .claude/plans/https-play-google-com-store-apps-details-parallel-locket.md

## 역할
Claude가 MCP For Unity로 코드·에디터·Play Test 전부 수행. 사용자는 승인만.
절차: 계획→승인→실행→보고→Memory.md·Debugging.md 기록.

## 코딩
SOLID·인터페이스·한글 주석·가비지 최소·[SerializeField] 우선
스크립트: Assets/Scripts/{Player,World,Game,Input,Data,Camera,Common}/
디버깅: debug_filtered.log 우선

## Git (커밋/푸시 트리거)
명시적 add, 한글 Conventional Commits, main 강제 푸시 금지
금칙: .env, *.key, *.pem, Library/, Temp/, Logs/, *.csproj, *.slnx

## /compact
Step·5+파일·빌드 후 한 줄 권장
