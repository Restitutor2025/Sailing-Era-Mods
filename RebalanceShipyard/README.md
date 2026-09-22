# Restitutor Rebalance Shipyard

조선소 '구매하기' 목록에 필요 기술 0 인 최하급 배(코그·슬루프·중국식 슬루프)만 나오게 한다. 제작(주문 건조)은 그대로. 새 세이브용.

- 소스 `src/` (Rules.cs = 순수 규칙, EntryPoint.cs = 훅), 검사 `tests/`.
- 빌드·설치: 게임 종료 후 `powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1` → `Mods\Rebalance\`.
- 문서: `docs/mods/rebalance-shipyard/`, 조사: 프로젝트 `handoff/SHIPYARD_REBALANCE.md`.
- 필요: `UserLibs\Restitutor.Core.dll` 0.1.0 이상.
