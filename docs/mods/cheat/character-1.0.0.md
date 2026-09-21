# Cheats Character 1.0.0 — Tab 인물창 인물 수정

2026-09-20 (KST). `Restitutor_Cheats_Character.dll` 신규. Interface 1.3.0 Panel API 사용, Order=17(경험치 16 아래, 여급 18 위). 다른 치트 DLL과 Interface는 바꾸지 않는다.

## 사용자 요구와 결정

- Tab 인물창에서만 보이는 치트창. 인물의 스킬, 스탯, 체력, 공격력을 표시하고 수정한다.
- 사용자 결정(2026-09-20): **체력 = HP**. 스탯 = 능력치 6종(Physical·Craft·Perceive·Charm·Knowledge·Lucky) 포함. 위치 = **통합 치트창 패널**(H 토글). 스킬 = **보유 스킬의 기본 레벨만, 0~스킬별 최대 레벨**. 빌드 = Claude가 컴퓨터 제어로 PC의 dotnet 실행.
- Claude가 정한 사항(사용자 미확인):
  - 대상은 **영웅만**. 목록에서 선원(`CharacterData.isSeaman`)을 고르면 패널을 숨긴다. 이유: 쓰기 전 확인 경로(`PlayerHoldRoleDB.FindHoldRole`로 같은 객체인지 확인)가 영웅에서만 성립한다.
  - 값 상한 **999,999,999**. Int32 최대보다 낮게 두어 게임이 기본값에 장비·버프 값을 더할 때 넘치지 않게 했다. HP 최대 하한 1, 나머지 하한 0. HP 현재 상한은 그 순간의 HP 최대 기본값.
  - 항목 이름은 게임의 `UICharacterModel` 정적 문구(TexTitleHp, TexTitleAttack, Physical, Craft, Perceive, Charm, Knowledge)를 `TextLibUtils.Text`로 한 번 더 번역해 쓴다. 실패하면 대체 이름(체력/공격력/체격/기교/감지/매력/지식)을 쓴다. 행운은 게임 문구 속성이 없어 "행운" 고정. **대체 이름은 한글 UI 명칭을 확인하지 않은 추정값이다.**

## 동작·계약

- 표시 조건: Interface 활성, Host.Player = PlayerDataManager.Data, `UIManager._alreadyOpenedUICtrls`에 `UICharacterCtrl`이 있고 뷰 `_state`가 있으며 `IsOpen()`이고 UIContent가 부모 체인까지 보임, `Model.SheetType == ESheetType.Character`, `Model.RoleIndex`가 목록 범위 안, 선택 항목이 선원이 아님, `PlayerData.PlayerRole.FindHoldRole(RoleId)`가 같은 객체. 조건 밖에서는 패널 전체를 숨긴다(취소선 없음). 판정은 프레임당 1회 캐시.
- 표시: 첫 줄 인물 이름·레벨. 행마다 "기본값" 또는 "기본값(게임 표시값)". 스킬 행은 "기본 Lv/최대 Lv".
- 입력: 행마다 `GTextInput.restrict=[0-9]`, maxLength 9, 숫자 외 문자 즉시 제거. [적용]을 누르면 범위 밖 값은 범위 끝으로 보정해 적용하고 메시지에 보정 사실을 붙인다. 빈 입력은 적용하지 않는다. 적용 순간 대상 인물이 표시 중인 인물과 다르면 거부한다.
- 쓰기(행당 최대 1회, 적용 후 읽어서 확인, 실패·불일치·예외에서 재시도 없음):

| 항목 | 속성 | 쓰는 방법 |
|---|---|---|
| 체력 현재 | ModifyProperty id 8 `CurData` | `SetCurrentData(v)` (원본이 0~MaxData로 자름) |
| 체력 최대 | ModifyProperty id 8 `MaxData` | `MaxData = v` 후 `LimitCurData()` |
| 공격력 | PointProperty id 7 `BaseData` | `UpdateBaseData(v)` |
| 능력치 6종 | PointProperty id 1~6 `BaseData` | `UpdateBaseData(v)` |
| 스킬 올림 | RoleSkillData id 23 `BaseData` | `PlayerRoleData.InnerUpdateSkillLevel(skillId, 차이)` (원본 경로) |
| 스킬 내림 | 같음 | `LevelProperty.UpdateBaseData(v)` (원본에 내리는 함수 없음) |

  값이 바뀐 경우 `PlayerHoldRoleDB.MarkDBDirty()`. 이어서 Tab Characters가 쓰는 갱신 순서 `InitSkillData → RefreshSheetCharacter → RefreshTipsRoleInfo → Model.MarkDirty`로 인물창을 다시 그린다(실패해도 값은 이미 기록됨, 경고 로그).
- Harmony 훅 0개. 강제 저장 없음. 로그: `role=<id> <항목> before= target= actual= shown=`.

## 확인된 근거 [본문]

GameAssembly SHA256 `50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA`. GNU objdump로 추출. [native.json](../../../Cheats/evidence/character-1.0.0/native.json)과 같은 폴더 asm 15개. 속성 번호는 interop `Client.Const.ERolePropertyType` 상수(Physical 1, Craft 2, Perceive 3, Charm 4, Knowledge 5, Lucky 6, Attack 7, Hp 8, SkillLevel 23).

- `get_Hp` 0x6770B0 / `get_MaxHp` 0x677130: `GetModifyProperty(8)`의 CurData(+0x18)/MaxData(+0x1C)에 BuffEffectProperty(+0x38)의 +0x14/+0x18을 더한다.
- `get_Attack` 0x677170, `get_Physical`·`Craft`·`Charisma`·`Knowledge`·`Perception`: `GetPointProperty(id)`의 가상 최종값(기본+효과). `GetLuckyValue` 0x6798A0: 최종값 + (Level−1)×RoleGrowthType(+0x38). 그래서 행운은 기본값과 표시값이 다르게 나온다.
- `PointProperty.UpdateBaseData` 0x118BC30: BaseData(+0x18) = max(0, v).
- `ModifyProperty.SetCurrentData` 0xD3E660: v<0 → 0, v>MaxData → MaxData, 아니면 v를 CurData에 대입.
- `ModifyProperty.LimitCurData` 0xD3E970: Cur+버프Cur > Max+버프Max일 때만 차이만큼 Cur를 줄인다.
- `UpdateMaxHpValue` 0x679E20 / `UpdateAttack` 0x679F70: round((Physical.BaseData/100 + 1) × RoleLevel 값)을 **저장된 HP 최대·공격력 기본값에 더한다**. 호출자(직접 호출 탐색): `AddRolePropPhysical`, `UpdateRoleLevelData`(레벨업), `RewardUtils.AddRoleProperty`. 따라서 HP 최대·공격력은 누적 저장값이며, Physical을 바꿔도 다시 계산되지 않는다. 치트로 바꾼 뒤 레벨업·체격 성장이 오면 그 증가분이 바꾼 값 위에 더해진다.
- `InnerUpdateSkillLevel` 0x67ACC0: 차이>0일 때만 동작, 기본 레벨을 올리고 최대 레벨(`HeroSkill.skillLevel`, 템플릿 +0x40)로 자른 뒤 `Messenger.Broadcast(…, roleId, skillId)` 호출. 직접 호출자에 `GameMasterManager.GetHeroAll`(개발용 명령)이 있어 같은 방식으로 스킬을 최대로 올린다.
- `GameMasterManager.ChangeRoleHp` 0x4E99A0: 개발용 명령도 `GetModifyProperty(8)`을 받아 필드를 직접 쓴다.
- `PlayerRoleData.InitObjectProperties` 0x677470: 속성 1~8, 21, 9~16, 22~25를 serializer 풀에서 받아 등록한다. 속성이 세이브에서 복원된다는 것은 이 구조에서 나온 **추론**이며 재로드 유지 여부는 실전 확인이 필요하다.

## 추론·미확인

- 스킬을 **내릴 때**는 원본 `Messenger.Broadcast`가 호출되지 않는다. 이 방송을 듣는 쪽(스킬 효과·직책 점수 등)이 무엇인지 조사하지 않았다 [미확인]. 내린 스킬의 부가 효과는 재로드 전까지 이전 상태로 남을 수 있다 [추론].
- 체격(Physical)을 바꿔도 HP 최대·공격력은 바뀌지 않는다 [본문 근거로부터의 추론]. 필요하면 두 값을 따로 고친다.
- 인물창 갱신 순서는 Tab Characters 0.5.x가 스킬 포인트 사용 뒤 쓰는 경로를 그대로 가져왔다. HP·능력치 변경 뒤에도 표시가 갱신되는지는 실전 미확인.

## 검증 상태

- 빌드: 사용자 PC dotnet으로 `Cheats/Character/compile.cmd` 실행(Claude가 컴퓨터 제어로 탐색기에서 실행). 경고 0, 오류 0. 게임이 실행 중이어서 `build.cmd`는 설계대로 거부됐고, 설치 단계 없는 `compile.cmd`로 빌드·검사했다.
- 관리 검사: `Cheats/character-tests`(Rules.cs 실제 코드) **PASS 30**.
- DLL SHA256 `1CE996250EBC96A450667D5BCDA8486984891DC2E084F4858CD67995933E956D`(24,064 B). `Cheats/releases/character-1.0.0/`와 `Mods/Cheats/`에 복사, 설치본 해시 일치 확인. 새 파일이므로 실행 중인 게임에는 로드되지 않았고 다음 실행부터 로드된다.
- 참조한 interop 이름은 빌드 성공으로 존재가 확인됐다. 게임/Steam 실행 없음. **실전 미검증.**

## 사용자 확인 항목

1. 세이브 사본을 먼저 만든다. DLL을 빼도 바뀐 값은 돌아가지 않는다.
2. Latest.log에 `Cheats Character 1.0.0 loaded` 줄.
3. Tab 인물창에서만 패널이 보이는지. 스킬/장비 등 다른 시트·다른 Tab 메뉴·선원 선택·인물창 닫힘에서 숨는지.
4. 항목 이름이 게임 한글 UI와 같게 나오는지(대체 이름이 보이면 알려 줄 것).
5. 각 항목 적용 뒤 인물창 표시가 바로 바뀌는지, 저장·재로드 후 유지되는지.
6. 스킬을 올리고 내린 뒤 스킬 효과(항해·전투 수치)가 기대대로인지. 특히 내린 경우.
7. 큰 값(예: 999,999,999) 적용 뒤 인물창·전투에서 표시·계산 이상이 없는지.
