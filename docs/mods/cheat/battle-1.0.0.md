# Cheats Battle 1.0.0

2026-09-19. `Restitutor_Cheats_Battle.dll` 신규 선택 기능. 구현·빌드·정적/관리 검사 완료, 게임 설치 및 실전 검증 전. 게임/Steam 실행 없음.

## 사용자 계약

- 기존 Interface 안에 `백병전` X1/X2/X3/X4/X5와 `포격전` X1/X2/X3/X4/X5 표시. 해상 전투(포격/백병전 상태) 밖에서는 패널과 점유 공간을 숨긴다.
- 두 배율은 독립 선택, 시작값 X1. **사용자가 바꾸거나 해상 전투 전체가 종료될 때만 선택을 변경한다.** 백병전·일기토 종료 후 같은 해상 전투로 돌아오면 둘 다 유지한다.
- 일시정지, 일시적 로딩/플레이어 조회 실패, 조종 대상 재확인, 패널 접힘/뷰 재생성은 선택값을 변경하지 않는다. 읽기 실패 시 표시/신규 적용을 보류한다. 전투 종료·해상 장면 이탈·게임 세션 초기화/로드·기능 제거 시 둘 다 X1.
- 백병전: 적용 대상 조종 함선의 별도 백병전에 참여하는 **아군 항해사**의 전투용 Attack/Craft/Perception/Physical에 배율 적용. 적과 일반 병사는 제외. 일기토에서도 같은 전투 유닛 사용. X1 변경 시 자기 변경분 복구.
- HP/최대 HP/시작 HP, 레벨, 이동 속도, 원본 항해사 데이터, 장비/저장 수치는 직접 변경하지 않는다. 능력치 배율은 최종 피해·승률의 정확한 배율이 아니다. 기존 전투 결과에 따라 발생하는 피해·부상·선원 손실은 원래 게임에서 정산한다.
- 포격전: **현재 조종 함선 한 척의 일반 대포 선체 피해 배율**. 다른 아군 함선과 적은 제외. 공성(isSiege) 제외. 선원·돛·키 피해, 어뢰·그리스화염 등 별도 무기, 근접 총격은 이 배율 대상으로 추가하지 않았다.
- 포격 배율은 공격 정보 생성 후 DelayFire 호출 시 해당 공격 건에 한 번 확정한다. 한 일제사격의 포문 수만큼 중복 곱하지 않는다. 사용자가 바꾼 값은 새 공격부터 적용되며 이미 발사한 공격은 발사 시 선택을 유지한다. 전체 전투 종료 시 남은 자기 포격 배율도 복구한다.
- 기존 공통 창의 접힘/X 닫힘 정책은 유지한다. 접힌 창은 펼쳐야 버튼이 보인다.

## 원본 근거와 구현 지점

설치 GameAssembly SHA256 `50d53d17829e3e77b9786ea42d998d1ad258f0653846524069f22e5e442effca` 대조 완료. [이전 항해사 분석](../../../analysis/navigator-combat/REPORT.md), [조종 함선 분석](../../../analysis/player-ship-damage/REPORT.md), [이번 본문/별칭 목록](../../../Cheats/Battle/evidence/targets.json).

`OceanScene.IsInBattle` 본문은 OceanBattle/MeleeBattle 두 상태를 포함한다. `GetFocusPlayer().Data.Guid`와 발사 함선 boatData.Guid 및 AttackInfo.sourceEntityGuid를 함께 비교한다. 변경된 focus에는 새 공격 기준만 갱신하며 **배율은 초기화하지 않는다.**

`StartEnterBattle` 후 생성된 ClientBattle.PlayerShip을 조종 함선의 BaseShipData.ShipGuid와 대조한다. PlayerUnits에서 IsPlayerUnit && !IsPawn 유닛만 기록한다. 전투용 네 개 속성은 최초 기준값에서 계산하고, 다른 주체가 현재 값을 변경하면 해당 속성 소유권을 포기해 덮어쓰지 않는다. 종료 때 자신이 쓴 값이 그대로 남아 있는 속성만 복구한다.

원본 `MeleeBattleController.OnUpdate`에 ClientBattle.Tick 본문이 인라인되어 있어 Tick에 패치하지 않고 실제 호출되는 OnUpdate를 관측한다. 원본 FSM/결과 계산을 건너뛰지 않는다. `Gun.Fire`에는 AttackInfo.Init/일부 Shoot 코드가 인라인되어 있으므로 실제 호출이 확인된 DelayFire를 사용한다. 조사한 DelayFire.MoveNext는 같은 AttackInfo를 탄환 Fire에 전달하고 gunDamageFactor를 다시 초기화하지 않는다. 일반 포탄 충돌 본문에서 gunDamageFactor가 선체 피해에 곱해진다.

### 패치 목록 (7개, 고유 RVA)

| 대상 | RVA | 계약 |
|---|---|---|
| OceanScene.ChangeInputState | 0x526F60 | Prefix: OceanBattle/MeleeBattle 외 상태로 나갈 때 X1 및 자기 효과 복구 |
| OceanScene.OnExit | 0x51F3D0 | Prefix: 장면 종료 초기화 |
| MeleeBattleController.StartEnterBattle | 0xF27E00 | Postfix: 대상 함선의 실제 전투 유닛 연결 |
| MeleeBattleController.OnUpdate | 0xF28AD0 | Prefix: 현재 선택으로 아군 전투 능력 갱신, 원본 실행 유지 |
| MeleeBattleController.ExitBattle | 0xF283A0 | Prefix: 해당 백병전 유닛 복구/해제. **선택값·포격 효과 유지** |
| MeleeBattleController.OnBattleEndedTipsClose | 0xF28750 | Prefix: 유닛 정리 누락 방어. 선택값 유지 |
| BoatEntityGun.DelayFire | 0x25DF020 | Prefix: 대상 공격 정보의 gunDamageFactor 한 번 적용 |

공유 generic setter/getter에는 후킹하지 않는다. 신규 입력 훅/우선순위/저장 쓰기 없음. Interface의 Panel.Visible/Refresh/Reset/입력 차단을 사용한다. 기능 OnUpdate에서도 상태를 확인하므로 UI를 닫아도 종료 처리를 수행한다.

공격 정보는 Pointer+attackGuid로 구분한다. 풀링된 객체가 다른 공격으로 재사용되면 이전 기준을 복구하지 않는다. 상한은 기록된 공격 정보 객체 4096개, 항해사 256명. 비정상 수치/예외에서는 자기 효과를 복구하고 로그를 남기되 사용자의 선택은 유지한다.

## 검증

- Release 빌드: 경고 0, 오류 0.
- [관리 검사](../../../Cheats/battle-tests/Program.cs): **49개 통과**. 적/동료 함선/병사 제외, X1~X5, HP 불변, 중복 적용 방지, 다른 수정 주체 보존, 풀링, 포격 선택 유지, 로딩 중단/복귀, 연속 백병전, 전체 종료/다음 전투 X1.
- 기존 Host 수명주기 검사 **77개 통과**.
- [원본 대조 스크립트](../../../Cheats/Battle/evidence/extract.py): 기준 해시, 정확한 7개 RVA·별칭 없음, 인라인/실제 호출 경로 대조 통과.
- [interop/IL 감사](../../../Cheats/Battle/evidence/interop-audit.txt): 후킹 인수 7개 일치, 참조 67개 해결, 게임 setter는 위 네 능력치와 gunDamageFactor뿐.
- 기존 Cheats 다른 기능 및 analysis/decompiled의 지정 심벌 검색에서 동일 신규 대상 직접 참조를 발견하지 못했다. 전체 외부 모드·실행 중 Harmony 순서·실전 무충돌 보장은 아니다.

관리 대역 검사는 네이티브 IL2CPP 후킹 성공, 실제 UI 위치/클릭, 실전 능력 변화, 모든 포탄 종류를 실행 검증한 것이 아니다. 사용자 실전 확인이 필요하다.

## 배포

- [단독 Battle DLL](../../../Restitutor_Cheats_Battle.dll)
- [릴리스 폴더](../../../Cheats/releases/battle-1.0.0)
- Interface 1.2.0 이상 필요(Panel.Visible). 배포 묶음에는 현재 Interface 1.2.1 포함. 설치 대상은 게임 `Mods/Cheats`, Interface는 기존 파일과 교체해 한 개만 둔다. 이번 작업은 게임 폴더에 복사하지 않았다.
- Battle SHA256: `9C268D49780BD7AB2A7570D7DFDD591F6AF1E7CB73C288E535AC60466ED58313`
- 포함 Interface SHA256: `502578ED33439AA283E11B101E1D01B79ACF36127E99B462F7D82203F8974F1A`

재현: `dotnet build Cheats/Battle/Restitutor_Cheats_Battle.csproj -c Release -p:NuGetAudit=false`, `dotnet run --project Cheats/battle-tests/Tests.csproj -c Release -p:NuGetAudit=false`, `python Cheats/Battle/evidence/extract.py`, `pwsh -NoProfile -File Cheats/Battle/evidence/audit.ps1`. 기존 프로젝트와 같이 로컬 APPDATA/NUGET_PACKAGES 빌드 환경 사용.
