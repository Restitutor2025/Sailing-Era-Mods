# Cheats Bargirls 1.0.0

2026-09-19. `Restitutor_Cheats_Bargirls.dll`, Interface 1.1.1 필수. 공통 창에서 소지금 아래/배속 위에 여급 패널을 추가한다. 1·2·3은 클릭 버튼이며 전역 키보드 단축키는 추가하지 않는다.

- 현재 도시의 여급이며 실제 표시된 술집 또는 해당 여급 화면에서만 활성화. 술집 화면은 현재 도시 및 실제 NPC 목록도 대조한다. 게임/플레이어 준비, 로딩, 도시 상태, 뷰 열린 상태, stage 부착과 표시 계층을 검사한다.
- 여급 없는 도시, 술집 밖 및 미준비 상태는 제목 취소선/패널 흐림/입력 차단. 비활성 사유 표시.
- 하트 누적값 1단계 50, 2단계 250, 3단계 650. 런타임 GameConst의 FIRST/SECOND/THIRD 값을 합산하며 하드코딩 수치로 쓰지 않는다.
- 2단계에서 1, 3단계에서 1·2는 alpha .35 및 touchable=false. Apply에서 대상을 재조회하고 하위 단계 요청을 다시 거부한다. 같은 단계는 기존 수치를 보존한다.
- `Favorability`와 선택 단계에 필요한 `CompleteTask` 하한(단계-1)을 올리고 `MarkDBDirty()`로 원본 저장/갱신 경로에 알린다. **CompleteTask는 여급 과제 진행 기록이기도 하므로 2·3단계는 해당 과제 제한을 건너뛴다.** 보상/선물 목록 및 최초 숙박 플래그는 수정하지 않으며 보상을 지급하지 않는다. 기존 과제 진행값은 내리지 않는다.
- 미등록 관계는 버튼 적용 시에만 원본 AddSocialRole로 만든다. 조회/패널 표시만으로 저장을 변경하지 않는다. 세션 및 뷰 교체 시 자체 대상 참조 초기화, 해제 시 패널 제거.

## 원본 근거

[선택 함수 본문과 SHA256](../../../Cheats/evidence/bargirls-1.0.0/native.json), [추출기](../../../Cheats/evidence/bargirls-native.py). 현재 설치 GameAssembly SHA256 `50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA`가 기존 기준과 일치했다. 전체 메타데이터를 문서로 읽지 않고 대상 타입/함수를 선택 추출했다.

- 확인된 본문: `PlayerSocialData.InnerUpdateFavorability` RVA 0x682020, Favorability +0x14, 상한 +0x18, CompleteTask +0x54. 과제 0이면 첫 하트, 1이면 첫+둘째 상한. 과제 제한을 그대로 둔 채 수치만 올리면 다음 정상 증가 경로에서 하락할 수 있어 제한도 함께 상승시킨다.
- 확인된 본문: `UIBarGirlCtrl.ListHeartRender` 0xB03FB0는 firstHeart/secondHeart/thirdHeart를 합산하여 하트 표시를 판정한다. `UIBarGirlModel` 생성자 0xB0B340는 GameConst를 읽는다. 현재 설치 table 사본의 값은 50/200/400이다.
- 확인된 본문: `PlayerSocialDB.UpdateFavorability` 0x681C80는 InnerUpdate 후 MarkDBDirty를 호출한다. 이번 치트는 과제 제한 및 목표값을 설정한 뒤 같은 dirty 경로를 호출한다. setter 자체의 이벤트 발생을 가정하지 않는다.
- 확인된 본문/메타데이터: `UIFacilityEntraceCtrl.SetFacilityNPC`는 시설 2의 목록에서 GetBarmaid(RoleId)를 확인하며, UIDrunkery ShowHook는 facilityType=2 및 SetFacilityNPC를 호출한다. 현재 구현은 기존 UI 목록을 읽고 새 게임 UI를 만들지 않는다.

## 패치·호환성

새 Harmony/네이티브 패치 0개. Interface 입력/저장 세션/포커스/뷰 수명주기를 재사용한다. Fleet Info의 GetNpcsBySeaAreaId는 호출하거나 수정하지 않지만 여급 화면 수명주기를 공유한다. SocialData와 과제/하트/선물 UI, 저장 갱신 이벤트는 공유 상태다. 검토한 Cheats/Contribution/Map/FleetInfo 및 외부 분석 소스에서 다른 SocialDB/CompleteTask 직접 쓰기는 검색되지 않았으나 전체 외부 모드나 실행 충돌을 검증한 것은 아니다.

## 검증 및 배포

기능 DLL 빌드 0 경고/0 오류. 순수 정책 검사 88개, Host 수명주기 검사 61개 통과. IL 감사로 Apply에서만 애정/과제 제한/dirty 쓰기를 수행하고 새 훅/보상 쓰기가 없는 것을 확인했다. [검사 스크립트](../../../Cheats/evidence/bargirls-audit.ps1).

작업 폴더 루트 DLL 및 `Cheats/releases/bargirls-1.0.0/`에 배포 사본을 보존한다. 게임 설치 폴더는 변경하지 않았다. 기존 Interface 1.1.1과 함께 Mods/Cheats에 넣는다.

게임과 Steam은 실행·조작하지 않았다. 실제 UI 갱신/과제 이벤트/저장 재로드/다른 모드 병용은 사용자 실행 전 미검증이다. 사용자 확인 항목: 여급 없는 도시 취소선, 술집 왕복 활성화, 1→2→3 및 0→3, 하위 버튼 차단, 다른 도시 대상 변경, 저장 재로드.
