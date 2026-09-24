# CityEditor Core — 주인공 2단계 패치 계약 (0.6.0)

모두 Postfix, 원본 생략·인자/반환값 교체 없음. 오류는 로깅 후 외부 전파 차단. 신규 UI 이벤트에서도 동일 원칙.

| 대상 | 버전 | 역할 |
|---|---|---|
| UICreatRoleManager.Start | 기존 | 원본 위치 및 추가 인물 복제. 이전 자체 선택 상태 정리 추가 |
| UICreateRoleView.OnInit | 기존 | 이름표 구성 |
| UICreatRoleManager.SetUIPos | 기존 | 버튼 배치 |
| UICreatRoleManager.RoleOnClick(ECharacter,bool) | 기존 | 추가 모델 확대 위치 |
| UICreatRoleManager.ReturnMap | 기존 | 추가 모델 복귀 |
| UICreatRoleManager.OnDisable | 신규 | 자체 선택 패널 정리 |
| UICreateRoleView.HideHook | 신규 | 자체 선택 패널 정리 |
| UICreateRoleView.DisposeHook | 신규 | 자체 선택 패널 정리 |
| UIHarborModel.InitRoleCommerceIcon | 신규 | 사전의 없는 신규 tid만 추가 |
| CreateArchiveCommand.OnMainRoleCreate | 신규 | 요청 tid 일치 시 반환 기록 |
| HarbourScene.OnEnter | 신규 | 생성 후 항구/주인공/시작완료 읽기 |
| UIHarborCtrl.EnterHarbourAniEnd | 신규 | 도착 상태 읽기·관측 요청 종료 |

모든 신규 대상은 인수 0개, void. 자산이 있고 선택 화면 Extras가 있으면 선택 패치8개+시작 패치4개. 기존 HeroRows의 Init표 Postfix 및 ResourceManager.InstantiateAsync 두 진단 오버로드는 보존(selfTest gate 추가). 기존 TableApply/Rows/Sea 패치 범위는 변경하지 않는다.

공유 상태: CreateRoleConst 기존 위치값은 기존 기능이 수정. 신규 UI는 GRoot 포커스, 해당 ctrl.UIAgent, UIContent.touchable, 선택 장면 transform을 소유 기간 동안 수정·취소 복원한다. CommerceIconDic 신규키만 쓰기. PlayerData/PlayerPort는 관측만 하며 새 게임의 쓰기는 원본 명령에 맡긴다. 상태 PendingTid는 중복 생성 방지/계측용.

접점: NavtoCity 0.2.0도 HarbourScene.OnEnter를 계측한다. 양쪽은 관측 Postfix지만 실행 순서·병용 실전은 미확인. InstantEntrance 0.1.1은 EnterHarbourAniEnd를 직접 패치하지 않으며 MapHarbourEnterCtrl/EntityPortNoteAnimator/Animator.Play가 대상이다. 입력·포커스를 동시에 바꾸는 별도 UI 모드와 병용 테스트 필요. 충돌 없음으로 선언하지 않는다.

[준비 소스](../../../output/hero-stage2-0.6.0/package/source/core/src/CreateRole/CreateRoleSelection.cs) · [원본/오프라인 검증](../../../analysis/hero-stage2/REPORT.md).


## 0.7.0 추가

진단 Postfix4개: TemplateUtils.GetPort(int)→Port, GetPortFacilityLineByType(int,int)→PortFacility, TemplateManager.GetWharf(int)→Wharf, WorldPortData.InitFacilityData()→void. 각 정확한 overload만 설치. 원본 변경 없음. 자체 ConcurrentDictionary/스레드별 직전port만 기록하며 최대180초/2048개로 제한한다. UIHarborCtrl.EnterHarbourAniEnd의 기존 Postfix에서 요청 일치 확인 후 이벤트가0보다 클 때 GameEventManager.CheckAndTriggerGameEvent를 한 번 호출한다. 따라서 이벤트를 지정하면 게임 상태를 바꿀 수 있고, 진단 자체는 관측만 한다. 초기 옵션0이면 추가 이벤트 호출 없음. NavtoCity/도시 표 수정/FunctionOpenDB 및 이벤트 manager와 수명주기 접점 존재; 병용 실전 미확인.


## 0.7.1 추가 계약

UICreateRoleView.Refresh/ShowHook Postfix2개 추가. SelectIndex0에서 추가 이름표 표시, 자체 패널 동안 UIContent.visible 보존/복원. 자체 PNG Texture/NTexture/tween은 패널별 소유/해제. HarbourScene.OnEnter에는 기존Postfix와 신규Prefix: PendingTid/실제주인공/시작항구 일치 시 설정된 기능만 SetFunctionOpen; 새게임1회 구간, 저장불러오기/기존인물 제외. 진단 조회 형식 보완. 새 도시 exporter는 Wharf 행을 같은 donor에서 복제, 기존게임함수 훅 추가 없음. [0.7.1](0.7.1.md).


## 0.7.2 추가 계약

표시 Postfix10개는 유지. UICreateRoleCtrl.OnAction_B/ReturnToMenu에 자체 선택/취소 해제 대기 한정 bool Prefix2개 추가(선택 훅 총12). OnUpdate는 취소 소유자가 있을 때만 물리 입력 해제를 관측한다. 화면 종료 시 참조 해제. 새 기함 지급은 기존 HarbourScene.OnEnter Prefix 내 PendingTid/주인공/항구/최초 실행/기함 없음 검사 뒤 원본 AddFlagShip 호출이며 PlayerShipHold·기함·선장·dirty/알림·자동 저장과 접점이 있다. 저장 로드에는 적용하지 않는다. 상세 UI 복제 및 새 PNG NTexture는 모달 소유/해제, 원본 UIContent는 복원한다. [상세](0.7.2.md). 실전 병용 확인 전.


2026-09-23 사용자 정정: 0.7.2 시작 함선은 아라비아 갤리220에서 일반 슬루프110으로 변경. 5000 및 다른 설정, 함수·입력·수명주기 계약은 유지. 원본 Ship110: 슬루프, Special=false, NeedSailorNumber=18. DLL 재빌드 불필요한 배포 설정 변경.


## 0.7.3 추가 계약

선택 후크 총15: 기존10 Postfix+취소2 Prefix+확인/방향3 Prefix. OnAction_A는 자체 상세 소유 시 확인 요청 후 원본 생략; 방향2는 자체 상세/취소 해제 대기 때만 원본 생략. 원본 5인 배열/enum 수정 없음. 원본UI 사본 groupCharacterSelect/Attribute/Tex/Select와 n34 표시·알파만 변경. 원본 컴포넌트 자체는 숨김/복원을 유지한다.

출항 관측10개: UIHarborView.CheckQuickEnterOcean, EnterOcean, EnterOceanResult, ShowHook, SetLimit, SetUnLimit + UIHarborCtrl.OnAction_X/Y 전후(8개); UIHarborView.IsTaskBan, GameEventManager.HasPortFacilityEvent 반환(2개). 모두 읽기 전용, prefix는void, 원본/반환값 불변. diagnostics 옵션·해당 신규주인공·새 게임600초·120행 제한. 별도 세션tid를 유지해 PendingTid가 첫 입항 뒤0으로 바뀌어도 계측 가능. OnUpdate는 R keydown만 관측, 입력 소비 없음. 한도/만료 참조 해제. 기존 출항 기능20/21은 이미 열려 있어 원인 확정/실전 성공 선언 금지.

데이터: MainHero156.DefaultOpenFunction에1~10 추가(기본 메뉴); 기존 시설 목록 유지. 기존 세이브 자동 보정 없음. 상세: 0.7.3.md.


## 0.7.4 시작 대화 계약

새 수명: 기존 EnterHarbourAniEnd Postfix의 일치한 신규 게임 요청에서만 HeroOpeningTalk.Schedule. OnUpdate 최대30초/항구준비 후1회 ShowTalkPart(15607401,null,156,callback,...) 호출. 다른 주인공/항구/활성대화/다른양수파트 제외. 입력 소비 없음. 직접 상태 초기화/출항 우회 없음. 원본 ShowTalkPart가 대화파트를 바꾸고 SetTalkPartEnd가 완료처리한다. 기존 저장 자동변경 없음.
HeroRows 삽입대상 TalkPart/Talk/TalkTextLib/중번/영/일의 Init 후크6개 추가(기존 행 거부). 원본 GameEvent11001 관련 표는 무변경. 시작 옵션 startTalk와 startEvent는 동시지정 금지. 에디터는 전용 시험 대화 또는 없음만 지원. 기존 출항 계측 유지. 병용검증 전. 0.7.4.md 및 분석 RESOLUTION 참조.


## 2026-09-24 설치 입력 재검증
사용자 BAT 실행은 게임 파일 변경 검사에서 중단됐다. 현재 게임 DLL은 FileVersion0.5.0.0, SHA256 07565b30d6072f0a783b5b920f9b52e6f49f9eccb4bb48e74b4ce7a54f1dbf37이며 기존 에디터 core/release DLL과 바이트 일치한다. 그 release 파일은 git 변경 없음. 에디터 citysave 적용 경로는 이 release를 복사한다. 실제 마지막 덮어쓰기 주체는 미확인이다. 이전 패키지 입력 해시 25130fcfa141e55df048b58419834c6830061192e5e908f5aa48bdef48703cd8에서 확인된 0.5.0 해시로 DLL 한 항목만 갱신했다. 출력 0.7.4 DLL·소스·게임 데이터는 불변. 보호 검사/백업/원자교체 유지. 기존 에디터의 재적용은 다시 구 DLL을 복사할 수 있다. 게임 실행 없이 CheckOnly로 재검증한다.

## 0.7.5 이름표 그룹 계약
TryBuildButtons의 추가 GButton을 donor.group에 연결한다. 원본 그룹 visible/alpha를 변경하지 않는다. 원본 그룹 경계 갱신 대상에 추가됨. 신규 후크·입력·타이머 없음. 추가 버튼 Dispose 및 모달별 visible 제어 유지. 게임 실전 확인 전. [근거](0.7.5.md).

## 0.7.6 상세 표시 계약
BuildCreationArt에 원본 이름/소속 컴포넌트, 자체 모달 최상위 이름 영역 추가. BuildNativeDetails는 원본 입력 로더 또는 모델 iconReturn/iconConfirm 리소스를 읽어 연결한다. 입력·후크 변화 없음. 원본 텍스처 폐기 없음, 자체 이름 영역 root Dispose 및 tween 정리 유지. [근거](0.7.6.md).

## 0.7.7 이름 PNG
BuildCreationArt: name_02 이미지+원본 소속 색 tint, 구분선, 소속 중앙 정렬. PNG 없으면 이름 rotation=0. 추가 이미지 NTexture는 기존 자체 소유/해제 목록 사용. 후크·입력 무변경. [근거](0.7.7.md).


## 2026-09-24 15:46 KST 복구 완료 — 파일 검증, 실전 재확인 전

사용자 요청으로 게임 Mods와 에디터 core/release의 Restitutor_CityEditor_Core.dll을 검증된 0.7.7 패키지로 복구했다. 두 대상 모두 FileVersion 0.7.7.0, SHA256 `5a205a288287ea70f410ac4cb5da149e0ebb28c07f362ab3b607989f1ec4a312`로 설치 후 재검증했다. 게임 원본 해시와 변경 전 0.5.0 해시를 검사하고 각 DLL을 백업한 뒤 원자 교체했다. 게임·Steam은 실행/조작하지 않았다.

로컬 복구 근거: 모드 작업 폴더 `output/core-0.7.7-recovery/verified-installation.json`, `restore.ps1`, `backups/20260924-154654-b591dd1b72754bd29341b933a657e887/manifest.json`. 소스 패키지: `output/hero-name-0.7.7/package/Restitutor_CityEditor_Core.dll`.

**남은 불일치:** 실행용 에디터 저장소 core 소스는 0.7.4다. 이번 작업은 알려진 정상 패키지를 복구한 것이며 소스 통합/재빌드를 수행하지 않았다. 현재 도시 적용은 같은 0.7.7 DLL을 사용하지만, 기존 소스를 재빌드하거나 별도 설치기를 실행하면 다시 낮은 버전이 설치될 수 있다. 0.7.7 소스 통합과 자동 다운그레이드 차단은 미완료다. 사용자 게임 재시험도 미확인이다.

