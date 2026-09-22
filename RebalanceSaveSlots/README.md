# Restitutor Rebalance SaveSlots

세이브 칸 10 → 101 (자동 2 + 수동 99). 세이브 파일 형식은 그대로(`GameData\PlayerData\<번호>`, 목록 `PlayerDataHistory\0`).

- 소스 `src/` (Rules.cs = 순수 규칙, EntryPoint.cs = Postfix 3개), 검사 `tests/`.
- 빌드·설치: 게임 종료 후 `powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1` → `Mods\Rebalance\`.
- 문서: `docs/mods/rebalance-saveslots/0.1.0.md`.
- 필요: `UserLibs\Restitutor.Core.dll` 0.1.0 이상.
- 모드를 빼면 게임은 목록 파일의 앞 10칸만 읽는다(파일은 남음). 모드를 다시 넣으면 11번째 이후 칸이 돌아온다.
