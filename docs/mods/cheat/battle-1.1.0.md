# Cheats Battle 1.1.0 — 백병전 즉시 돌입 · 함선 체력 고정 체크박스, 선택 유지 (2026-09-23)

상태: **구현됨-미확인**(VM 빌드 경고/오류 0, battle 검사 PASS 87(기존 49 중 초기화 검사 6개를 '유지'로 변경 + 신규 38), 게임 확인 전 — 사용자 테스트 보류 지시).
설치: `Mods/Cheats/Restitutor_Cheats_Battle.dll` SHA256 `8036C9BCB4E05241A1B17E728CC81DFD774E820288D16B759F1AE7011D6B3196` (사본 `Cheats/releases/battle-1.1.0/`).
이전: 1.0.1 = `Cheats/releases/1.5.1/Restitutor_Cheats_Battle.dll`(90125AA2…), 소스 `Cheats/archive/battle-1.0.1-source/`.
설계 근거: 프로젝트 문서 `handoff/CHEAT_BATTLE_TOGGLES_REVIEW.md`.

## 기능
패널(해전 중에만 보임) 포격전 줄 아래 체크박스 2개. 줄 전체가 클릭 영역, 켜면 ✓.

| 체크박스 | 동작 | 대상 |
|---|---|---|
| 백병전 즉시 돌입 | 기함이 접현을 시작하는 순간 원본 진행도 경로로 바로 백병전. 백병전 뒤 적 배가 살아 붙어 있으면 원본의 5초 사격 재개 때 다시 돌입(사용자 결정: 재돌입 허용) | 기함(`IsPlayerFlagship`). 호위함 접현(AI 모의전)은 원본 그대로 |
| 함선 체력 고정 | 선체 HP 가 줄지 않음(침몰 판정도 안 함). 피해 숫자 팝업은 원본대로 뜸 | 조종 중인 배(= 기함, 전투 중 전환 불가 — 사용자 확인) |

유지 규칙(사용자 결정 2026-09-23): 체크박스와 **백병전·포격 X1~X5 배율 모두** 해전이 끝나도 **유지**되고 다음 해전에 자동 적용(1.0.1 까지는 배율이 해전마다 X1 로 돌아갔음). 세이브 불러오기·타이틀·O(치트 끔)·패널 오류 때만 X1·해제.
해전이 끝나면(또는 바다 장면을 나가면) 적용된 효과는 전부 원래대로 돌린다: 항해사 능력치·발사 탄 계수 복원, 체력 잠금 원래 값, 구독 해제 → 항해 중에는 원본과 같음. 선택값만 남는다.
배율을 유지해도 바다 밖 프레임 조회 생략(1.0.1)은 그대로: 조기 반환 조건에서 '배율 = X1' 을 뺐고, 대신 '적용 중인 효과 없음' 만 본다(검사 'no per-frame lookups').

## 구현 — Harmony 훅 추가 0개 (7개 그대로)
- 체력 고정: 게임 내장 필드 `BoatEntityHitHandler.lockHealth`(+0x50). `BoatBodyHitHandler.Damage` 0xEC2DB0 가 true 면 `UpdateHp`·`CheckRemainingHP` 를 건너뜀(damageType 10 제외). 켤 때 원래 값 보관, 끌 때 우리가 쓴 true 가 그대로이고 원래 false 였을 때만 false 로.
- 즉시 돌입: 기함 `BoatEntityBoardShoot.BoardShootingBegin`(Action, +0xE0) 에 델리게이트 1개 등록. 원본 `BeginShooting` 끝에서 호출됨(null 이면 건너뜀). 원본 코드는 이 필드를 쓰지 않음(+0xE0 쓰기 전수 스캔 0건). 처리기는 표시만 세우고(물리 콜백 안에서 게임 함수 호출 안 함), 다음 프레임 기존 `BattleRuntime.Update` 에서 `AddProgress(101 − 내 진행도 − 적 진행도)` 1회 → 원본 `CheckProgressFull → ShootingProgressIsFull → StartMeleeBattleForPlayer`. 백병전이 묶여 있는 동안(melee != null)은 무시.
- 컴포넌트 찾기: `Entity._comps` 목록을 `TryCast` 로 1회 순회(`GameLinks.cs`). 배(엔티티)당 한 번만 찾고, 못 찾으면 그 배는 다시 찾지 않음. 매 프레임 추가 작업 = 기존 Update 안의 bool 비교뿐.
- 오류 격리: 링크 코드에서 예외 → 그 해전 동안 체크박스 기능만 멈추고(로그 1줄) 배율은 계속. 다음 해전에 다시 시도.
- 로그: `hull lock ON (controlled ship; was False)` / `hull lock OFF (restored False)` / `instant boarding ON (flagship BoardShootingBegin subscribed)` / `instant boarding: +N progress` / `instant boarding OFF (unsubscribed)`, 적용 불가 시 `... not applied`.

## 파일
- 배율 유지: `BattleRuntime.Reset`(해전 끝) 에서 `Choice.Reset` 제거 → `ResetSession` 으로 이동, `Update` 조기 반환 조건에서 X1 조건 제거.
`Cheats/Battle/`: Rules.cs(Toggles·BoardRule), BattleRuntime.cs(HullLease·BoardLink·SyncLinks·ResetSession), GameLinks.cs(신규, Il2Cpp 전용), BattlePanel.cs(높이 164→246, 체크박스 2줄), EntryPoint.cs(1.1.0, 세션 해제), csproj 1.1.0. 검사 `Cheats/battle-tests/`(GameLinks 관리 스텁).

## 게임 테스트(사용자, 보류 중)
1. 로그 `Cheats Battle 1.1.0 loaded (7 hooks, unchanged)`.
2. 해전 중 패널에 체크박스 2줄, 클릭 시 ✓ 토글, 로그 ON/OFF.
3. 체력 고정: 포격·충각·화재에 HP 불변(숫자 팝업은 정상). 전투 뒤 항해 중 HP 감소가 다시 들어오는지.
4. 즉시 돌입: 기함 접현 순간 백병전, 호위함 접현은 원본대로. 백병전 뒤 적이 살아 붙어 있으면 약 5초 뒤 재돌입.
5. 다음 해전에 체크·배율(X2~X5) 유지·자동 적용(패널 표시와 실제 효과 모두), 세이브 다시 불러오면 X1·해제. 해전 사이 항해 중 끊김 없음.
6. 오류 `Battle checkboxes suspended` 없음.
