# Stat Rank 0.1.4 패치 목록

- `UICharacterView.RefreshTipsRoleInfo` Postfix `AfterRoleInfo`: `_UIContent`/`roleInfo` null이면 return. 등급 글자 `GTextField('RestitutorStatRank')`를 각 `UICom_Prop.TexPropTitle`의 부모에 1회 추가·재배치. 폴링 켬.
- `UICharacterView.HideHook` Postfix `AfterHide`: 표 닫기, 폴링 끔.
- `OnUpdate`(창 열림 동안만): 커서-글자 영역 판정, 맨 위 히트 대상이 `UISheetCharacter.btnReturn` 또는 글자 자신일 때 표 생성(GRoot 자식, sortingOrder 31950, touchable=false). btnReturn은 읽기만.
- 원본 위젯 쓰기: 없음(자체 글자 추가만). 세이브 쓰기: 없음.
