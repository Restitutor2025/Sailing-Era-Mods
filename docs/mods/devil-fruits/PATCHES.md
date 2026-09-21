# Devil Fruits 0.1.0 패치 계약

11개 훅(Harmony, GameAssembly SHA256 50D53D17… 아니면 전부 비활성).

| 대상 | 종류 | 동작 |
|---|---|---|
| TemplateManager.InitTextLib / _English / _Japanese / _ChineseTraditional (ByteBuffer) | Postfix | 없는 키만 추가: Item_Name/Des/LongDes_990001~990005, ItemType_Name_DevilFruit, Item_Tips_DevilFruit (4개 언어 표 모두 한국어 문구) |
| TemplateManager.InitItemType (ByteBuffer) | Postfix | `_itemType[9900]` 추가: code devil_fruit, parent 1000, canUseInBag false, icon = 1000행 icon |
| TemplateManager.InitItem (ByteBuffer) | Postfix | `_item[990001~990005]` 추가 (필드는 0.1.0.md 표) |
| PlayerHoldRoleDB.Deserialize (PlayerDataSerializer) | Postfix | Resync: 추적 중 영웅 원래값 복원 후 Heros+LeaveHeroes의 열매 기록만큼 growth 감소 |
| PlayerHoldRoleDB.InitHook | Postfix | Resync |
| UIHeroLevelUpCtrl.OnClickBtnLevelUp | Prefix(차단 안 함) | Resync (레벨업 판정이 템플릿 등급을 읽기 전) |
| UICharacterView.RefreshTipsRoleInfo | Prefix(차단 안 함) | Resync(Stat Rank Postfix보다 먼저) + btnReturn 리스너 연결 |
| UICharacterView.HideHook | Postfix | 확인창 닫기, 리스너 해제 |

훅 외: `UISheetCharacter.btnReturn`에 onTouchBegin/onTouchEnd 리스너(Tab Characters 0.6.8 언어 캐처와 같은 방식). Stat Rank 글자(`RestitutorStatRank`) 안에서 누르고 같은 글자에서 뗀 경우 + 해당 열매 보유 + 항해사(선원 아님)일 때만 CaptureTouch/StopPropagation/CancelClick. 그 외 원본. 확인창: GRoot 자식 sortingOrder 32100, 전체 화면 반투명 차단막(클릭 = 취소). 입력 시스템(OnEventCaptureInput) 차단 없음 → 창이 열린 동안 키보드·패드 입력은 원본 화면으로 간다.
