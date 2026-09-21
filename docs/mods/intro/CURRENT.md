# 시작 화면 모드 현재 상태 — 0.1.3
DLL: Restitutor_fixes.dll (설치 이름 Restitutor_Additional_IntroSkip.dll). Restitutor.Core 0.1.0 필요, 게임 확인 전. [0.1.3 Core 전환](0.1.3.md) · [0.1.2 버전 근거](0.1.2.md) · [정확한 패치 4개](PATCHES.md) · [원본](../../../analysis/systems/STARTUP.md).
## 변경 계약
UILaunchView.OnInit/HideHook Postfix로 launch/안내 포인터 등록·해제.
Transition._Play Postfix는 등록된 두 안내만 Stop(true,true)로 완료시킨다. 전역 함수지만 다른 전환에는 적용하지 않는다.
UpdateHook Prefix는 반환 void이며 원래 실행을 차단하지 않는다. 안내 생략 후 GroupDeal 표시를 정리한다.
OnUpdate는 스플래시 중단/지연 SkipDeal, LateUpdate는 잔여 표시 정리.
## 공유 상태·복원
읽기/쓰기: 안내 Transition 상태, launch.GroupDeal.visible, 자체 pending/포인터.
HideHook에서 세션 참조 정리. GroupDeal을 이전 visible로 되돌리는 구현은 아니다. 동의 저장 플래그를 수정하지 않는다.
패치 설치 실패 시 UnpatchSelf. 같은 타임라인·SkipDeal·GroupDeal을 변경하는 모드는 별도 검토한다.
## 검증
실전 모든 분기 미확인. 소스/현재 DLL 기준 해시는 [목록](../DLL_INVENTORY.md). 파일 변경 시 갱신한다.

