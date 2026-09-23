# Rebalance Boarding — 패치 계약

## 0.3.0
| 대상 | 종류 | 내용 | 조건 | 복원 |
|---|---|---|---|---|
| `BoatEntityBoardShoot.CheckProgressFull` RVA 0x25CD710 (+0x1C1 = RVA 0x25CD8D1) | 프로세스 메모리 코드 1바이트 | `cmp ecx, 0x64` → `cmp ecx, 0x28` (0.1.0 과 동일) | GameAssembly SHA256 50D53D17…EFFCA, 실행 중 함수 본문 0x44A 바이트 = 파일의 같은 위치 바이트(편집 위치 83 F9 64 0F 8E 확인) | `OnDeinitializeMelon`: 본문이 "기준 + 우리 1바이트" 그대로일 때만 0x64 로 되돌림 |
| `BoatEntityBoardShoot.OnEnemyExitShooting(BoatEntityBoardShoot)` | Harmony Prefix(void, 원본 항상 실행) | 떠나는 배가 마지막 대상이고 백병전 잠금 중이 아니며 진행도>0 이면 (자기, 대상, 진행도, Time.time) 기록 + 3.1초 뒤 게이지 정리 코루틴 예약. 기록 조건이 아니면 게이지 즉시 숨김 | Restitutor.Core 0.1.0+ | `hookSet.RemoveAll()` |
| `BoatEntityBoardShoot.BeginShooting()` | Harmony Postfix | 기록이 있으면 꺼내고(항상 삭제), 새 대상 == 기록 대상 이고 경과 ≤ 3초이며 백병전 잠금 아님 → `ShootProgress = max(현재, 기록값)` + 게이지 갱신. 아니면 게이지 숨김 | 〃 | 〃 |
| `BoatEntityBoardShoot.BoardShootTickForAccumulateProgress()` | Harmony Postfix | 자기 또는 대상이 아군 함선이면 `self.ShootProgress + target.ShootProgress` 를 게이지에 반영 | 〃 | 〃 |
| `BoatEntityBoardShoot.OnEnterMeleeBattle()` | Harmony Postfix | 게이지 숨김 | 〃 | 〃 |
| `BoatEntityHealthBar.ShowHPBar()` | Harmony Postfix | `boatData.IsPlayer`(아군 함선 전부) 이면 `UIproBattleSeaman` 바 1개를 만들어 그 배의 `UICompBattleHp` 에 자식으로 붙임(선원 게이지 바로 아래, 호박색, 투명·숨김 상태로 시작) | 〃 | 〃 + `OnDeinitializeMelon` 에서 만든 바 전부 Dispose |
| `BoatEntityHealthBar.HideHPBar()` | Harmony Prefix(void, 원본 항상 실행) | 그 배에 붙인 바를 부모에서 떼고 Dispose | 〃 | 〃 |

- 게임 객체에 쓰는 것: `BoatEntityBoardShoot.ShootProgress` 한 필드(0.2.0 유예, 원본이 방금 0 으로 만든 값을 되돌림)와 **우리가 만든 FairyGUI 바 자체**뿐. 게임이 만든 UI 객체의 값·색·위치는 건드리지 않는다(읽기만: `progSailor` 의 x/y/width/height).
- 매 프레임 실행되는 핸들러 없음. 게이지는 부모 컴포넌트의 자식이라 위치 갱신은 게임의 기존 `UpdateHPBar` 가 한다.
- 원본 호출을 막거나(bool prefix) 인자·반환값을 바꾸는 핸들러 없음. 모든 핸들러는 예외를 자체적으로 잡는다.
- 백병전 진입 경로 3곳(StartMeleeBattleForPlayer, OnEnterMeleeBattle, AIMeleeBattleSimulateHandler.StartMeleeBattle)은 모두 `isMeleeBattling=1` 을 먼저 세운 뒤 잠금 → 유예 기록 안 함 → 백병전 직후 진행도가 되살아나 재진입하는 일 없음.
- 시간 기준 `UnityEngine.Time.time` / `WaitForSeconds`(둘 다 게임 배속·일시정지 반영).
- 이 모드 외에 위 6개 함수를 훅하는 모드 없음(소스 검색).

## 0.2.0
| 대상 | 종류 | 내용 | 조건 | 복원 |
|---|---|---|---|---|
| `CheckProgressFull` RVA 0x25CD8D1 | 코드 1바이트 | 100 → 40 | 위와 같음 | 위와 같음 |
| `OnEnemyExitShooting` / `BeginShooting` | Harmony Prefix(void) / Postfix | 3초 유예 기록·복원 | Restitutor.Core 0.1.0+ | `hookSet.RemoveAll()` |

## 0.1.0
| 대상 | 종류 | 내용 | 조건 | 복원 |
|---|---|---|---|---|
| `CheckProgressFull` RVA 0x25CD8D1 | 코드 1바이트 | 100 → 40 | 위와 같음 | 위와 같음 |
