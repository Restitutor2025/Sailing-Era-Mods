# Instant Entrance — 0.1.1

DLL: Restitutor_fixes_Instant_Entrance.dll (설치 이름 Restitutor_Additional_Entrance.dll). Restitutor.Core 0.1.0 필요. 사용자 실행 미검증.
[0.1.1 Core 전환](0.1.1.md) · [0.1.0 버전과 근거](0.1.0.md) · [패치](PATCHES.md)

항구 표식 등장 시 원래 Animator.Play(string,int,float)의 normalizedTime만 1로 변경한다. EntityPortNoteAnimator.PlayEnterAnimation 호출 중 동일 ENTER_ANIM_NAME인 경우에 한정한다. 원래 활성화 처리와 입항 함수는 유지한다.
MapHarbourEnterCtrl.Start 완료 후 _canEnterHarbour=true로 설정해 확인된 2.5초 초기 제한을 제거한다. 원래 코루틴은 이후 동일 값만 쓰므로 취소하지 않는다.

Release 빌드 경고/오류 0, 실제 interop 서명 확인, 독립 대역 10개 통과. Unity/Harmony 네이티브 호출과 화면 결과는 검증하지 않았다.
사용자가 보고한 반복 접근 지연 전체와 두 변경의 인과관계는 미확정이다. 별도의 EntityOperationTip 프리팹 연출, 비동기 로딩 시간은 변경하지 않았다. 즉시 표시·입항 성공을 확인한 정식 실전 결과로 취급하지 않는다.
