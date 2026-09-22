# Rebalance Growth 패치 목록

## 0.1.0 (7)
| 대상 | 종류 | 목적 |
|---|---|---|
| Gyyx.Template.TemplateManager.InitGameConst(ByteBuffer) | Postfix | GameConst GetSkillNeedLevelUp=5, ROLE_PROP_MAX_COUNT=500 |
| Gyyx.Template.TemplateManager.InitRoleLevel(ByteBuffer) | Postfix | RoleLevel 100~200 행 추가 |
| Client.UILogic.UIHeroLevelUp.UIHeroLevelUpCtrl.OnClickBtnLevelUp() | Prefix | 대기 중 추가 지급 취소(기록만) |
| UIHeroLevelUpCtrl.OnClickBtnLevelUpFive() | Prefix+Postfix | 10레벨 버튼 포인트 수 계산·원본 진행 확인 |
| UIHeroLevelUpCtrl.ChangeRewardSkillState() | Prefix+Postfix | 원본 1포인트 확인 뒤 나머지 지급 |
| Client.UILogic.UIHeroLevelUp.UIHeroLevelUpView.RefreshRoleData() | Postfix | 99 하드코딩 "다음 포인트" 문구 보정 |
| Client.UILogic.UICharacter.UICharacterView.RefreshTipsRoleInfo() | Postfix | 99 하드코딩 MAX·필요 경험치 보정 |

공유 대상(모두 원본을 막지 않음): OnClickBtnLevelUp·OnClickBtnLevelUpFive — Cheats Skill(1.1.0 은 이 모드가 있으면 추가 지급 생략), Tab Characters(Prefix, 이전 보상 연출 완료), Devil Fruits(OnClickBtnLevelUp Prefix). ChangeRewardSkillState — Cheats Skill, Tab Characters(Prefix+Postfix). RefreshTipsRoleInfo — Devil Fruits(Prefix), Stat Rank(Postfix, 읽기 전용), Tab Characters(Postfix, 장비 패널). 이 모드가 쓰는 isMax·texNeedExp·texTitleSkillDesc 를 다른 모드는 쓰지 않음(소스 검색).

## 0.2.0 (+6 = 13)
| 대상 | 종류 | 목적 |
|---|---|---|
| Client.PlayerStore.PlayerRoleData.GetLuckyValue() | Postfix | 행운 최대 99 |
| PlayerRoleData.GetRolePropByID(int) | Postfix | id 6 = 성장 포함 행운(최대 99) |
| PlayerRoleData.UpdateMaxHpValue() | Postfix | HP 증가량 /100 → /250 차이 보정 |
| PlayerRoleData.UpdateAttack() | Postfix | 공격 증가량 차이 보정 |
| Client.UILogic.UILandExplore.UILandExploreCtrl.InitPlayerModelList(PlayerHoldRoleDB) | Postfix | 운반량 최대 30 |
| UILandExploreCtrl.InitSeamenModelList(PlayerHoldRoleDB) | Postfix | 운반량 최대 30 |
공유: 다른 Restitutor 모드는 이 6개를 훅하지 않음(소스 검색). Cheats Character 는 능력치를 직접 쓰므로 HP/공격 보정과 무관.

## 0.3.0 (+7 본체, 레벨표 4는 별도 Harmony)
| 대상 | 종류 | 목적 |
|---|---|---|
| TemplateManager.InitRoleLevel | Postfix (tables) | 0.1.0 과 같음, 해시 검사와 분리 |
| PlayerRoleData.UpdateMaxHpValue / UpdateAttack | Prefix (tables) | 로그 전용: 레벨 행 없음 경고, 원본 그대로 실행 |
| PlayerHoldRoleDB.Deserialize | Postfix (tables) | 로그 전용: 세이브 로드 시 행 없는 레벨 |
| UIHeroLevelUpCtrl.OnClickBtnLevelUp | Postfix 추가 | 1레벨 결과 → 누적 % 결과 |
| UIHeroLevelUpCtrl.OnClickBtnLevelUpFive | 기존 Prefix/Postfix 확장 | 슬라이더 값 기록, n레벨 결과 → 누적 % 결과 |
| UIHeroLevelUpCtrl.AniResult | Postfix | 누적값 저장(<Stat>_Exp) |
| UIHeroLevelUpCtrl.InitMaxContinueLevel | Postfix | 슬라이더 단계·모델 값(MaxContinueLevel/NeedExpFive/AfterEffect) |
| CurRoleData.GetNeedExpAfterEffect | Postfix | 부분 경험치 뺀 다음 레벨 비용 |
| UIHeroLevelUpView.RefreshNotMax / RefreshMax | Postfix | 슬라이더 패널 생성·표시, BtnUp 숨김, BtnUpFive 제목·회색 |
