# Restitutor Rebalance Boarding

접현(두 배가 붙은 상태)에서 백병전이 더 빨리 일어나게 한다.

- **0.1.0 기준값 100 → 40**: 훅 없이 `BoatEntityBoardShoot.CheckProgressFull` 의 비교 상수 1바이트(RVA 0x25CD8D1)만 게임 실행 중 메모리에서 바꾸고 종료 때 되돌린다(Fleet Info 방식). GameAssembly SHA256 이 맞고 실행 중 함수 본문이 파일 바이트와 같을 때만 적용. `src/NativeThreshold.cs`, `src/Patch.cs`.
- **0.2.0 3초 유예**: 원본은 접촉이 끊기는 즉시 진행도 0. 같은 배에 3초 안에 다시 붙으면 끊기기 전 진행도를 되돌린다. 훅 2개(OnEnemyExitShooting Prefix 기록·BeginShooting Postfix 복원), 접촉 시작/끊김 때만 동작. `src/Grace.cs`(규칙), `src/EntryPoint.cs`.
- **0.3.0 진행도 게이지**: 플레이어 **소속 함선 전부**(기함 + 호위함)의 선원 게이지 바로 아래에 백병전 진행도 바를 추가한다(호박색). 값 = 두 배 진행도의 합 ÷ 기준값. 0 이면 0.5초 페이드로 숨기고, 진행도가 생기면 0.5초 페이드로 나타난다. 바는 배의 기존 체력 UI(`UICompBattleHp`)의 자식이라 게임이 이미 매 프레임 돌리는 위치 갱신을 그대로 따라간다 — 이 모드가 추가하는 매 프레임 작업은 없다. `src/Gauge.cs`(규칙), `src/ProgressBarUI.cs`.
- 아군 전부인 이유: 호위함도 백병전에 들어간다. `CheckProgressFull` 은 자기 또는 상대가 **플레이어 기함**일 때만 실제로 조작하는 백병전(`BoardShootManager.ShootingProgressIsFull`)으로 가고 그 외에는 AI 자동 시뮬레이션(`AIMeleeBattleSimulateHandler`)으로 갈 뿐, 진행도는 모든 배에서 똑같이 쌓인다. 판정은 `BoatEntityData.IsPlayer`.
- 영향: 플레이어 기함 백병전 + AI 끼리의 간이 백병전 모두. 세이브 변경 없음.
- 검사 `tests/`(설치된 GameAssembly 를 인자로). `evidence/`·`archive/` 는 로컬 전용.
- 빌드·설치: 게임 종료 후 `powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1` → `Mods\Rebalance\`.
- 필요: `UserLibs\Restitutor.Core.dll` 0.1.0 이상(3초 유예·게이지). Core 가 없으면 기준값 40 만 적용.
- 문서: `docs/mods/rebalance-boarding/`, 분석: `analysis/boarding-melee-duel/REPORT.md`(로컬), 프로젝트 `handoff/BOARDING_MELEE_DUEL.md`.
