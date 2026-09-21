# Text Speed 0.1.5 — 매 프레임 LINQ 제거(기능 동일)

게임 확인 전. [상세](0.1.5.md)

# 텍스트 속도 현재 상태 — 0.1.4 (2026-09-21, **완료: 사용자 빌드·설치·게임 내 적용 확인**)
0.1.3에 추가: "빠르게" + CTRL 안 누름 → 오토 켜짐·꺼짐 관계없이 문장 완료 3초 뒤 다음 대사. 새 훅 UIDialogView.EndTyping Prefix/Postfix(오토 켜짐이면 원본 clickWait 해제, 자체 3초 대기 → 원본 OnAction_ContinueTalk), UIDialogCtrl.OnAction_ContinueTalk Prefix(원본이 먼저 진행하면 대기 취소). 대사 변경·재타이핑 시 취소, CTRL 눌림 시 즉시. [0.1.4](0.1.4.md)
공유 상태 추가: UIDialogModel.clickWait(쓰기), _autoTalk·IsTyping·TalkTid(읽기), UIDialogCtrl.autoAndSpeed(읽기), OnAction_ContinueTalk 호출.

아래는 이전 버전 기록이다.

# 텍스트 속도 현재 상태 — 0.1.3
DLL: Restitutor_fixes_textspeed.dll. [상세/설정](0.1.3.md) · [정확한 패치 5개](PATCHES.md) · [원본](../../../analysis/systems/DIALOGUE.md).
## 변경 계약
OnVideoSettingItemRender Prefix/Postfix: 기존 행 위치/높이 복원 후 옵션 삽입. 원래 실행 차단 없음.
UIDialogView.OnInit Postfix 및 StartEffect Prefix: 현재 효과 소유권 등록. HideHook Postfix: 등록/대기 제거.
TypingEffectObject.Start Postfix: 빠르게 모드일 때 해당 대화 효과만 완료 대기. LateUpdate에서 상태/콜백 일치 시 네이티브 Cancel.
## 공유 상태·복원
UI 행 자식/크기, 효과 진행 상태/완료 콜백, 자체 owners/pending.
원본 대화 자동 진행을 추가하지 않는다. 같은 효과 Cancel 또는 StartEffect를 변경하는 모드는 완료 중복/등록 순서를 검토한다.
원래 렌더러 재실행 전 행 복원, 폐기 행 정리. Shutdown은 패치와 대기/소유권을 정리하지만 모든 살아 있는 옵션 행을 즉시 원상복구하는 핫 언로드 검증은 없다.
저장 파일: UserData/Restitutor/textspeed.v1.json. Harmony ID: restitutor.module.textspeed.v1. 명시적 선택 저장·외부 변경 거부·손상 시 읽기 전용 백업.
## 검증
저장 독립 테스트와 UI 실전 검증은 별개. 이번 지식베이스 작업은 빌드/게임 실행을 하지 않았다.

