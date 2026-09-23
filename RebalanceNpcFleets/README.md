# Restitutor Rebalance NpcFleets

대형 복선 제거의 NPC 쪽. NPC 배 구매 목록(GameConst `NPC_BUY_SHIP_LIST`)에서 대형 복선(300) 제외, 대형 복선을 쓰던 NPC 함대 7개의 배 교체(해적 = 간증선 기함 + 해창선·개랑선, 교역 선단 = 복선). 표를 불러올 때 한 번만 값을 바꾼다. 새 세이브용.

- 소스 `src/` (Rules.cs = 순수 규칙, EntryPoint.cs = 훅), 검사 `tests/`.
- 빌드·설치: 게임 종료 후 `powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1` → `Mods\Rebalance\`.
- 문서: `docs/mods/rebalance-npcfleets/`, 조사: 프로젝트 `handoff/BIG_FUCHUAN_REMOVAL.md`.
- 필요: `UserLibs\Restitutor.Core.dll` 0.1.0 이상.
