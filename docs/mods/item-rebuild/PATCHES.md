# Item Rebuild 0.1.21 패치 계약

훅 48개: 0.1.20의 KnowledgeDB.Deserialize·PlayerLaneDB.Deserialize Postfix 제거. Exchange Prefix(ExchangeBegin)는 RefuseOwnedPurchases 후 후속 플래그만 세우고, Finalizer는 예외가 없으면 HideOwnedGoods만 호출한다. 항로도 자동 사용·UnlockPrefabLane·RemoveItemByGuid·ShowOpenLine·AddRouteMapData 호출 없음. 공유 상태: 상점 4개 목록, Slot.isSelect(구매 선택 해제), PlayerLaneDB.CheckLaneIsUnlockByLaneId 읽기. [근거](0.1.21.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.20 패치 계약

신규 훅 2개(총 50): KnowledgeDB.Deserialize Postfix(KnowledgeLoaded), PlayerLaneDB.Deserialize Postfix(LanesLoaded) — 인스턴스 기록만, 반환·차단 없음. 둘 다 호출된 뒤 다음 OnUpdate에서 1회 130001~130056 해금 항로를 KnowledgeDB.AddRouteMapData로 보충. UseBoughtRoute는 UnlockPrefabLane 뒤 AddRouteMapData를 호출하고 항로를 대기열에 넣는다. 대기열은 UIOpenLineCtrl.Instance가 열림을 한 번 보고 IsOpen·IsLoading이 모두 false가 된 뒤에만 다음 ShowOpenLine을 호출(무관측 600프레임 후 포기). 공유 상태: UIOpenLineCtrl 모델(원본 함수가 씀), UIMenuHelper 메뉴 금지 상태(원본 ShowOpenLine/CloseHook), KnowledgeDB 항로도 사전·dirty. [근거](0.1.20.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.19 패치 계약

신규 훅 1개(48개): `UIPropStoreCtrl.SetStoreGoods` Postfix `HideOwnedGoods` — 모델 목록 listPropStore/listCollege/listBlackMarket/listGuild에서 `Rules.UnlockItem` 중 `Owned`(선실: 가방 보유, 항로도: 가방 보유 또는 PlayerLaneDB 해금)인 Slot 제거, MarkDirty. 기존 `Exchange` Prefix `ExchangeBegin` 맨 앞 `RefuseOwnedPurchases`(보유·중복 선택 해제, CalculateMoney, 거래 중단), 통과 시 `RememberRoutes`. Finalizer `ExchangeFinal(__instance, …)`는 예외 없을 때 `AfterExchange`: 늘어난 130001~130056 기록만 `PlayerLaneDB.UnlockPrefabLane` → `PlayerBagDB.RemoveItemByGuid`(추가 연출 호출 없음). 가방: `GroupEquipment`가 CabinUnlock 행을 UI 목록에서 제거, `Occupied`/`CapacityAllowsAdd`/`ProjectedSpace`가 해금 아이템을 논리 용량에서 제외. 공유 상태: 상점 모델 Slot 목록, PlayerLaneDB 해금 목록(세이브), 가방 기록 삭제(구매한 항로도만). [근거](0.1.19.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.18 패치 계약

신규 훅 없음(47개 유지). Rules.Book = SkillBook ∪ LanguageBook(71101~71130). Rules.Stack·GroupedRecord·GroupableRecord가 Book을 사용한다. 0.1.16 스킬북 계약(Compact/AddBegin/AddEnd/RemoveGuidBegin/판매 어댑터, 대표 GUID 유지, 백업·롤백)이 언어 서적 저장 행·수량에도 적용된다. 원본 OnClickBtnBook 언어 분기도 같은 InnerRemoveItemByGuid를 호출한다. [근거](0.1.18.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.17 패치 계약

신규 훅 없음(47개 유지). Rules.Equipment = 기존 equipment ∪ clothes(59개). clothes는 설치 type 33 68행에서 무역 통계 수첩 33062~33070을 뺀 집합이며 10111을 포함한다. GroupedRecord/GroupableRecord/Unworn 경로가 자동으로 의상에 적용된다. Rules.Stack·Eligible·저장 병합은 변경 없음. 공유 상태는 0.1.15와 같다: 가방 UI 목록/equipmentCounts, 논리 Space, 판매 Slot.isSelect. [근거](0.1.17.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.16 패치 계약

신규 훅 없음(47개). Rules.Stack에 type 72 스킬북 108종을 포함한다. 기존 Compact/AddBegin/AddEnd/RemoveGuidBegin/RemoveIndexBegin/판매 수량 어댑터가 실제 책 기록과 Number를 처리한다. 비무역 동일 ItemKey만 병합, 대표 GUID 유지 및 중복 행 제거. 최초 백업·수량 보존·실패 롤백은 기존 계약 유지.

공유 상태가 0.1.15의 표시/용량에 더해 스킬북 저장 행·수량으로 확장된다. 원본 OnClickBtnBook은 같은 대표 GUID를 사용하며 수량 2 이상은 한 권 감소, 마지막 권은 원본 삭제다. 가방/판매 준비 시점에 병합하므로 이미 열린 외부 UI가 이전 개별 GUID를 보관하는 병용은 실행 확인이 필요하다. UI 입력·포커스·수명 훅 추가 없음. [근거](0.1.16.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.15 패치 계약

신규 훅 없음(47개 유지). Rules.SkillBook은 설치 type 72의 정확한 108개 ID, GroupedRecord는 장비 또는 스킬북 분류다. GroupableRecord는 비무역 스킬북 또는 기존 Unworn 장비다. 저장 병합 Rules.Stack/Eligible 및 장비 전용 Unworn 판정은 확장하지 않는다.

기존 UIBagCtrl.RefreshDataModel Postfix GroupEquipment에서 스킬북 대표 행과 합산 수량을 생성한다. All/분류 목록 필터링, GlobalIndex 보존, SlotIndex 재생성, BagLabel 배지 재사용. 기존 용량/추가/동시 구매·판매 검사와 Sales/QuickQuantity에 같은 분류를 적용한다. 서로 다른 ItemKey와 판매 조건은 분리한다. 목록·배지는 기존 Close/Reset 수명으로 정리한다.

공유 상태는 가방 UI 목록/equipmentCounts, 논리 Space, 판매 실제 Slot.isSelect뿐이다. 스킬 습득 UI/저장 행/개별 GUID/Number는 변경하지 않는다. 원본 OnClickBtnBook → InnerRemoveItemByGuid 경로를 유지한다. [근거](0.1.15.md) · [훅 서명](../../../ItemRebuild/evidence/0.1.15/hooks.json).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.14 패치 계약

신규 UILandExploreView.Refresh Postfix(LandSupplyRendered) 1개, 총 47개. 중앙 BtnSupply의 onTouchBegin 가운데 입력을 연결한다. 기존 ListLandBagRender Postfix는 index 0 보급품의 같은 입력을 연결하고 도구 행에는 독립 GTextField 수량 배지를 만든다. ListDepotRender/ResetLand는 풀링된 표시와 구독을 정리한다.

공유 상태: UILandExploreSupplyCtrl.Model의 EntryType/ItemHeavy/MaxHeavy/AddNum/MaxNum/가격, UILandExploreModel.Supply 및 MarkDirty. 원본 OnClickSupply와 같은 데이터 준비 후 ShowHook/SetCountPrice만 호출하며 UI Show/Close를 호출하지 않는다. 원본 OnClickBtnOK의 Supply 대입과 dirty 처리를 재현한다. 실제 탐험 출발 시 결제 경로는 유지한다.

입력·수명: 기존 QuickBinding의 button=2/활성 화면/세대/모델/팝업 경계를 유지한다. 수량 배지는 touchable=false, 아이콘 내부 비율 배치, onRemovedFromStage/재렌더/Reset 때 제거한다. [서명](../../../ItemRebuild/evidence/0.1.14/hooks.json) · [근거](0.1.14.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.13 패치 계약

신규 native 훅 2개, 총 46개. [정확한 서명/핸들러](../../../ItemRebuild/evidence/0.1.13/hooks.json) · [근거](0.1.13.md).

| 대상 | 변경 |
|---|---|
| UIPropStoreCtrl.OnAction_R2 / OnAction_L2 | 신규 Prefix: 물리 가운데 버튼 누름/해제 프레임은 기존 같은 상품/전체 선택 중복 경로 억제; 키보드·컨트롤러 유지 |
| UIPropStoreCtrl.SetPlayerGoods | 기존 Prefix: 현재 상점 ctrl 연결 및 이전 아이콘 이벤트 정리 |
| UIPropStoreView.ListPlayerRender / ListShopRender | 기존 Postfix 확장/추가: 현재 바인딩 상품의 가운데 버튼 이벤트 연결 |
| UIPropStoreCtrl.BtnMulti | 기존 Prefix: 가운데 클릭이 native 클릭 경로로도 들어오면 수량 창 없이 Max 처리 |
| UILandExploreView.ListDepotRender | 기존 Postfix: 현재 도구의 가운데 클릭을 최대 수량 적용에 연결 |
| 기존 ResetSales / ResetLand | 아이콘 이벤트와 ctrl 참조 정리 |
| QuantityTrack | 자체 수량 창에 Min/Max 버튼과 비율 너비 배치; 클릭 전파 차단 |

공유 상태: 기존 판매/구매 단위 Slot.isSelect, SelectNum/원본 CalculateMoney, 탐험 ListLandBag/ItemWeight/IsSelect, 아이콘 onTouchBegin/onRemovedFromStage 및 자체 숫자 CurNum. 실제 결제/재고 삭제와 장비 GUID/저장은 변경하지 않는다. 클릭 이벤트에서만 처리하며 전역 입력 폴링 없음. 포커스/모델/세대/실제 목록 소속을 확인하고 재사용 때 action 교체, stage 이탈과 화면 종료 때 listener를 제거한다.

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.12 패치 계약

신규 native 훅 없음, 44개 유지. [서명/핸들러](../../../ItemRebuild/evidence/0.1.12/hooks.json) · [원본 근거](0.1.12.md).

- Unworn: EquipData.Equip의 객체 주소 비교를 영속 GUID+ItemId 비교로 교체. missing/null·WearRoleId·활성 효과 제외는 유지. 가방 UI 그룹/용량/판매 대상이 같은 함수를 사용한다.
- Rules.Consumable: 설치 type=4의 10059/10077/10108/40001~40038 총 41종 추가. 기존 병합/추가/삭제/가방 배지/용량/판매 어댑터를 그대로 사용한다.
- 장비 저장 행·GUID·장착 상태는 변경하지 않는다. 기념품은 기존 소모품처럼 백업 후 ItemKey별 Number를 병합한다. 대표 GUID 보존, 속성이 다른 행은 별도다.
- 입력·콜백·수명주기·native 패치 대상은 0.1.11과 동일하다. 슬라이더 수정도 유지한다.

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.11 패치 계약

0.1.10의 40개에 입력 액션 4개 추가, 고유 native 훅 44개. [정확한 서명/RVA/핸들러](../../../ItemRebuild/evidence/0.1.11/hooks.json) · [본문 근거](0.1.11.md).

| 대상 | 변경 |
|---|---|
| UICommonInputNumCtrl.OnAction_B | 신규 전처리: 자체 팝업에서 감소 버튼 대신 CancelPopup; 외부 금액 팝업은 원본 |
| UICommonInputNumCtrl.OnAction_A | 신규 전처리: 자체 팝업 콜백을 한 번 호출 후 원본 닫기 |
| UILandExploreSupplyCtrl.OnAction_A | 신규 전처리: 활성 마우스 UI 정리 후 원본 OnClickBtnOK |
| UILandExploreSupplyCtrl.OnAction_B | 신규 전처리: 마우스 UI 즉시 정리, 원본 Close 실행 |
| UICommonInputNumCtrl.OnClickBtnAdd/Reduce | 기존 전처리: 자체 수량 창의 0/최대값 순환 |
| UICommonInputNumCtrl.OnClickBtnReturn | 기존 전처리: 취소로 통일, 확정 콜백 호출 제거 |
| UICommonInputNumView.Refresh | 기존 후처리: 수량 제목, 입력과 하단 버튼 사이 슬라이더, 취소/적용 두 버튼 |
| UILandExploreSupplyView.Refresh | 기존 후처리: 원본 보이는 바 영역의 마우스 트랙, 취소/적용 두 버튼 |
| UIPropStoreView.ListPlayerRender | 기존 후처리: 수량 글자/영역 1.5배 |

공유 상태: 자체 숫자 모델/콜백/제목과 원본 하단 버튼 visible, 공급 AddNum/MaxNum/가격 계산, 추가 FairyGUI 자식과 마우스 캡처. 원본 하단 버튼은 이 UI 동안 숨기고 종료 때 복원한다. 취소·적용/Reset/창 교체/stage 이탈에서 컨트롤 및 콜백을 해제한다. 공급 취소/적용은 닫기 애니메이션 전 즉시 해제한다. 새로운 전역 키 폴링·DB 변경 없음. 원본 기본 액션 A/B를 사용하므로 사용자 리매핑을 강제로 변경하지 않는다.

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.10 패치 계약

0.1.9의 34개에 6개 추가, 고유 native 훅 40개. [정확한 서명/RVA/핸들러](../../../ItemRebuild/evidence/0.1.10/hooks.json) · [본문 근거와 제한](0.1.10.md).

| 대상 | 변경 |
|---|---|
| UIPropStoreView.Refresh | 신규 전/후처리: 표시용 그룹 인덱스 생성, 최종 ListPlayer 개수와 용량 텍스트 보정 |
| UIPropStoreView.ListPlayerRender | 신규 전/후처리: 실제 대표 인덱스 전달, 개별 Slot 바인딩과 단가 유지, 수량 라벨 |
| UIPropStoreView.ListShopRender | 신규 전처리: 재사용 슬롯의 자체 수량 라벨 정리 |
| UIPropStoreCtrl.SelectAllCargos | 신규 전처리: 묶인 플레이어 목록은 실제 단위 Slot 선택/해제; 구매측 원본 유지 |
| UIPropStoreCtrl.SelectSameGoods | 기존 전처리: 표시 인덱스로 대표 항목 선택, 숨은 단위 UI 인덱스 접근 방지 |
| UILandExploreSupplyView.Refresh | 신규 후처리: 현재 보급품 모델/MaxNum과 마우스 수량 컨트롤 동기화 |
| UILandExploreSupplyCtrl.ShowHook | 신규 전처리: 이전 보급품 컨트롤/콜백 정리 |
| UICommonInputNumView.Refresh | 기존 후처리: 자체 판매 팝업뿐 아니라 탐험 도구 팝업에도 공통 QuantityTrack 제공 |
| UILandExploreCtrl.OnClickListDepot | 기존 전처리: 입력 상한에 현재 가용 무게 반영, 확정 때 재검사 유지 |

공유 상태는 실제 상점 ListCurPlay/Slot.isSelect 및 표시 리스트의 numItems/인덱스/수량 라벨, 자체 숫자 팝업 모델, 보급품 AddNum/MaxNum/가격/버튼 상태다. 모델의 원본 단위 목록은 수정하지 않는다. 입력 이벤트는 해당 컨트롤의 클릭/드래그에 한정하고 좌표 변환·정수 반올림·pointer capture를 사용한다. 부모 stage 이탈/창 종료/Reset/보급품 재열기에서 이벤트와 컨트롤을 정리한다. 보급품 공유 RVA setter는 후킹하지 않는다. 전역 입력/폴링 추가 없음.

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.9 패치 계약

장비 표시와 용량을 동일한 미장착/동일 ItemKey 그룹으로 계산한다. 0.1.8에 3개 추가, 고유 native 대상 34개. [정확한 훅/핸들러/RVA](../../../ItemRebuild/evidence/0.1.9/hooks.json) · [원본 근거](0.1.9.md).

| 대상 | 변경 |
|---|---|
| PlayerBagDB.GetFreeCapacity | 신규 후처리: 현재 플레이어 가방의 Capacity - 논리 점유량 |
| UIBagView.RefreshBagData | 신규 후처리: texCapacity와 ctrlOverLoad를 논리 점유량으로 갱신 |
| ItemBagData.InnerAddItem | 기존 전처리에 논리 용량 검사 추가; 해당 호출의 원본 행 수 검사만 대체 |
| UIPropStoreCtrl.Exchange | 기존 Prefix를 ExchangeBegin으로 확장, 최종 점유량 검증과 거래 범위 연결; Finalizer로 예외/정상 모두 복원 |
| UIPropStoreModel.get_SellCount | 신규 후처리: 해당 Exchange의 최초 공간 비교에만 동치 용량 계산값 반환, 이후 원래 판매 개수 |
| GameManager.ForceReset | 거래 용량 범위도 해제 |

공유 상태: 가방 실제 행/GUID/장착 상태를 읽어 논리 그룹 집계, 화면 용량 텍스트/과부하 Controller, 상점 구매/판매 선택 목록과 최초 SellCount 경계. Capacity 저장값과 목록 자체는 용량 계산 때문에 변경하지 않는다. 거래 범위 안에서만 검증된 구매 우선 실행을 허용하고 Finalizer로 수명을 제한한다. 단독 획득은 각 호출에서 검사한다. 전역 입력/폴링 추가 없음. ItemBagData.get_Capacity/GetCargoTotalCapacity의 공유 RVA는 패치하지 않는다.

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.8 패치 계약

기존 24개에 신규 7개, 고유 native 훅 31개. 정확한 서명/RVA/핸들러는 [hooks.json](../../../ItemRebuild/evidence/0.1.8/hooks.json). [정책 및 근거](0.1.8.md).

| 대상 | 변경 |
|---|---|
| 기존 ItemBagData 추가/삭제 및 Deserialize | 소모품 61종 확장, 신규는 ItemKey별 병합 및 속성 보존; 역참 정규화 유지 |
| UIBagCtrl.RefreshDataModel | 후처리 GroupEquipment 추가, UI 목록만 필터, GlobalIndex 보존 |
| UIBagCtrl.DiscardItem | 신규 후처리, 전체 삭제 배치 종료 뒤 모델 재구성 |
| UIBagCtrl.CloseHook / GameManager.ForceReset | 장비 표시 참조와 판매 세션 정리 추가 |
| UIPropStoreCtrl.SetPlayerGoods | 신규 전처리, 현재 가방 연결·병합 후 원본 단위 Slot 생성 |
| UIPropStoreCtrl.BtnMulti / SelectSameGoods | 신규 전처리, 플레이어 판매 대상에만 숫자 팝업 |
| UIPropStoreCtrl.Exchange | 신규 전처리, 재고·장착 상태 및 실제 빈 행 기준 동시 구매 공간 검사; 원본 결제 전 실행 |
| UIPropStoreCtrl.CloseHook | 신규 전처리, 미확정 판매 팝업·콜백·슬라이더 해제 |
| UICommonInputNumView.Refresh | 신규 후처리, 자체 판매 모델일 때만 정수 슬라이더 생성/동기화 |
| 기존 공통 숫자 입력 훅 | 판매는 0~판매 가능량, 역참은 기존 3개/무게 제한; own pointer 경계 유지 |

공유 상태: ItemBagData.Items/Number/Guid, PlayerEquipData의 GUID/착용자/효과 참조, 가방 모델 ItemList/BagGroupList, 상점 ListCurPlay/Slot.isSelect/SelectNum 및 원본 CalculateMoney. 표시 그룹은 장비 저장 상태를 수정하지 않는다. 입력은 기존 클릭과 자체 GSlider 이벤트이며 전역 입력/주기적 폴링은 없다. 다른 숫자 팝업·상점 닫기·Reset에 세션을 해제하고 세대/모델/분류로 오래된 콜백을 차단한다. 지연 UIManager 훅과 경고 창은 시작 단계에서 호출하지 않는다.

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.7 패치 계약

0.1.7: 가방 수량 표시의 아이콘 내부 여백을 4→14 UI 단위(작은 아이콘에서는 짧은 변의 20% 상한)로 늘렸다. 흰색 글자·검은색 외곽선 1을 적용하고 기존 글자 크기에서 10% 확대(정수 반올림)했다. 기존 우측/하단 정렬, 긴 수량 Shrink 및 변경 시 갱신을 유지한다. 실전 화면 확인 전이다. [근거](0.1.7.md).

아래는 이전 버전 기록이다.

# Item Rebuild 0.1.6 패치 계약

0.1.6: BagSlotRenderer 배지를 loaderIcon 부모 좌표 및 최상위 sortingOrder로 배치. 템플릿/실제 항목 ID 검증 추가. RefreshDataModel 전처리에 안정적 도구 연속 배치 추가: 플레이어 목록 순서만 변경 후 원본이 GlobalIndex를 재생성한다. 실제 순서 변경 때만 기존 배지 정리와 DB dirty. 모든 항목 참조/수량 보존, 다른 항목 상대 순서 유지. native 훅 24개 및 변경 알림 방식 유지. [근거](0.1.6.md).

0.1.5: 폴링 제거. GUID별 변경 표시를 HashSet으로 모아 다음 프레임에 직접 참조의 최종 Number만 읽는다. InnerRemoveItem 전후처리 및 UIBagCtrl.CloseHook 정리 추가로 총 24개 native 훅. 개별/일괄 삭제에 성공 후 알림, 병합은 실제 변경 항목만 알림, 롤백은 원래 참조로 재연결. 저장/입력 정책 유지. [근거](0.1.5.md). 아래 내용은 이전 버전 기록이다.

0.1.4: common.title은 표시 중 숨기고 별도 터치 불가 GTextField를 common 자식으로 추가한다. loaderIcon의 실제 경계를 변환하여 우측 아래 안쪽에 배치, Shrink로 긴 수량의 넘침을 방지한다. Melon OnUpdate에서 최대 5Hz로 현재 가방을 한 번 스캔하고 GUID+ItemId로 배지를 동기화한다. 이전 인덱스에 의존하지 않는다. 재사용/화면 이탈/ForceReset에서 자식 제거 및 원래 제목 visible 복원. native 훅 22개 유지, 저장/입력 변경 없음. [근거](0.1.4.md).

0.1.3: BagSlotRenderer 후처리만 시각 변경. 대상 common.title을 `x수량`, 원래 font size/2, black, Right/Bottom, autoSize=None, singleLine, stroke=0, 가장자리 4 단위 여백으로 표시한다. TextFormat은 복사하여 변경하고 이전 텍스트/서식/좌표/크기를 저장한다. 재사용 시 자체 작성 텍스트가 남은 슬롯만 복원하여 다른 아이템에 적용되지 않게 한다. 신규 훅/저장/입력/수명주기 변경 없음. [근거](0.1.3.md).

0.1.2: 신규 훅 없음. ListLandBag의 null 보급품 칸을 검색에서 제외, CarryLabel은 index 0 및 null 항목을 건너뛴다. 출발용 임시 목록에는 null 항목을 원래 순서대로 보존하여 원본 Where(item != null) 경로에 전달한다. 빈 목록/모델/표시 자식에 방어 처리 추가. 수량·무게·보유 저장 정책 유지. [재현·근거](0.1.2.md).

0.1.1: 총 대상 22개 유지. 아래 UIManager 훅만 최초 역참 수량 팝업 클릭으로 지연하여 원본 static 생성자의 FairyGUI.Stage 조기 생성을 방지한다. 초기화 중 대상 타입/메서드 해석 금지, 별도 NoInlining 함수로 격리. 성공 후 중복 등록하지 않으며 실패 시 해당 팝업을 열지 않고 오류 기록. Reset에도 이미 설치된 훅은 유지한다. [변경 근거](0.1.1.md).

[등록·핸들러](../../../ItemRebuild/src/EntryPoint.cs), [출발·팝업](../../../ItemRebuild/src/Land.cs), [정확한 서명/RVA/핸들러](../../../ItemRebuild/evidence/hooks.json).

| 대상 | 역할 |
|---|---|
| PlayerBagDB.Deserialize | 후처리: 현재 가방 연결 및 대상 항목 병합 |
| PlayerBagDB.AddItem | 전처리: 현재 가방 연결 |
| UIBagCtrl.RefreshDataModel | 전처리: 모델 구성 전 대상 항목 병합 및 도구 연속 배치 |
| ItemBagData.InnerAddItem | Prefix/Postfix/Finalizer: 원본에 1개 전달 후 요청량 적용·병합; 기존 종류만 용량 검사 우회; 예외 시 인벤토리 스냅샷 복구 |
| ItemBagData.InnerRemoveItemByIndex / InnerRemoveItemByGuid | 대상 수량>1이면 1개 차감하고 원본 행 삭제 생략; 마지막 1개는 원본 |
| ItemBagData.InnerRemoveCargoByIndexList | 대상 묶음은 1개 차감하고 원본에 남은 삭제 인덱스만 전달 |
| ItemBagData.InnerRemoveItem | 전후처리: 요청 종류의 표시 항목 Number 비교 후 변경분만 큐 등록 |
| UIBagCtrl.CloseHook | 전처리: 자체 배지·GUID 인덱스·미처리 큐 정리 |
| UIBagView.BagSlotRenderer | 후처리: loaderIcon 안 우측 아래 별도 검은색 x수량 배지, 절반 글자 크기; 재사용 시 이전 배지 제거 및 제목 표시 복원 |
| GameManager.ForceReset | 전처리: 팝업 취소, 가방/선택/백업 세션 상태 정리 |
| UILandExploreCtrl.SetDeportGoods | 전후처리: 병합 후 원본 목록 생성, 대상별 재고/단위무게 등록 |
| UILandExploreCtrl.OnClickListDepot | 대상만 원본 토글 대체, 숫자 팝업 열기 |
| UILandExploreCtrl.JumpToLandExplore | Prefix/Finalizer: 선택 묶음을 원본 출발 루프용 임시 Goods로 펼치고 목록 복원 |
| UILandExploreCtrl.CloseHook | 후처리: 팝업 취소 및 출발 세션 해제 |
| UILandExploreView.ListDepotRender / ListLandBagRender | 후처리: 보유/휴대 수량 표시 |
| UICommonInputNumCtrl.OnClickBtnAdd / OnClickBtnReduce | 자체 팝업 포인터일 때만 ±1, 상한 제한 |
| UICommonInputNumCtrl.OnValueChange | 자체 팝업에서 원본 실시간 callback/slider 접근 생략 |
| UICommonInputNumCtrl.OnClickBtnReturn | 전처리: 확정 callback 1회, 후처리: 소유 해제 |
| UICommonInputNumModel.set_CurNum | 자체 모델에만 0~min(재고,3) clamp·dirty; 원본 callback/slider 경로 대체 |
| UICommonInputNumCtrl.OpenInputNumPanel | 자체 호출에만 소유 설정, 타 용도의 직접 호출 시 기존 선택 취소 |
| UIManager.ShowInputNumPromptBox | 최초 역참 수량 클릭에서 1회 지연 등록. 원본이 OpenInputNumPanel을 인라인하므로 별도 전처리에서 기존 선택 취소 |

## 공유 상태·입력·수명

플레이어 ItemBagData.Items/Number/Guid 및 저장 dirty 경로, BagGroup.GlobalIndex, 상점/선물의 GUID/인덱스 선택, 탐험 준비 ListDeport/ListLandBag/ItemWeight를 공유한다. 플레이어 가방 native pointer와 초기화 스레드가 맞을 때만 저장 데이터 수정. 허용 ID 13종, IsCommerce=false 한정. 장착·무역·내구도/파손 확률은 변경하지 않는다.

공통 숫자 입력창의 기존 입력/포커스/버튼을 사용하며 전역 키 입력은 추가하지 않는다. 공용 ChangeStep=1000 getter는 변경하지 않는다. 자체 callback은 보유량과 무게를 재검사하며 팝업 확정 전에는 휴대 목록을 수정하지 않는다. 다른 팝업 열기/탐험창 닫기/전체 Reset은 미확정 입력 취소, 세대 번호로 오래된 callback 무효화. 원본 A/B 버튼 경유 닫기는 확정으로 처리한다.

TabMenu 진단과 RefreshDataModel/BagSlotRenderer가 직접 중첩한다. ForceReset은 기존 모드들과 수명주기를 공유한다. 지정 심벌을 검색한 프로젝트 소스/분석 SpecialOrder 1.1.0 소스에서는 핵심 수량/탐험/숫자 입력 함수의 추가 직접 패치를 찾지 못했다. 전체 설치 DLL과 동적 패치 순서/다른 모드에 의한 live pileNum 변경의 호환성 증명은 아니다.

실행 파일 디스크 해시가 다르면 비활성화하며 초기화 실패 시 자체 훅 해제. 병합 전 JSON 백업, 총합 overflow/0 이하 수량 거부, 변경 중 예외에 인벤토리 복구. 원본 호출의 게임 이벤트나 출발의 부분 진행까지 트랜잭션으로 되돌리는 기능은 없다. 원래 예외를 숨기지 않는다.
