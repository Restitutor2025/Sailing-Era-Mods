# Rebalance Growth

항해사·고용 선원 성장 리밸런스 (0.3.0): 스킬 포인트 5레벨마다(원본 15), 만렙 200(원본 99), 능력치 상한 500(원본 99),
능력치 성장 = 레벨당 누적 %(S 100 · A 77.5 · B 55 · C 32.5 · D 10 %), 레벨업 = 경험치 사용량 슬라이더(남는 양은 인물 부분 경험치),
행운 ≤ 99(이벤트 판정 포함), HP/공격 증가 신체/250, 운반량 ≤ 30.
설치: `Mods/Rebalance/Restitutor_Rebalance_Growth.dll`, 필요 `UserLibs/Restitutor.Core.dll` 0.1.0+.

- 소스: `src/` — `Rules.cs`(순수 규칙, 검사 대상), `Tables.cs`(GameConst·RoleLevel 표), `Levels.cs`(레벨표 확장 로그 검사, 해시와 별도),
  `LevelUp.cs`(연속 레벨업 추가 포인트), `Growth.cs`(누적 %), `Slider.cs`(경험치 슬라이더), `Display.cs`(99 하드코딩 표시 보정),
  `Luck.cs`·`Stats.cs`·`Weight.cs`(0.2.0), `EntryPoint.cs`(설치·훅).
- 검사: `tests/` (`dotnet run --project tests/Tests.csproj -c Release`).
- 빌드·설치: `build.ps1` (게임 종료 후).
- 문서: `docs/mods/rebalance-growth/` (CURRENT, PATCHES, 0.1.0~0.3.0).
- 다른 모드가 읽는 공개 값: `EntryPoint.GrowthActive`(Stat Rank), `EntryPoint.ExtraGrantActive`(Cheats Skill).
