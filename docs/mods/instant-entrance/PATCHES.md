# 패치 등록 — 0.1.0

소스: [EntryPoint.cs](../../../InstantEntrance/src/EntryPoint.cs)

| 원본 메서드 | RVA | 패치 | 변경 |
|---|---|---|---|
| MapHarbourEnterCtrl.Start() | 0x6B4AC0 | Postfix | 초기 대기 플래그만 true |
| EntityPortNoteAnimator.PlayEnterAnimation() | 0x25ADEF0 | Prefix / Finalizer | 스레드별 애니메이션 이름 범위 저장·복원; 예외 그대로 반환 |
| UnityEngine.Animator.Play(string,int,float) | 0x24C6120 | Prefix | 위 범위에서 같은 이름만 normalizedTime=1 |

원본 함수를 생략하는 Prefix 없음. 입력·저장·거리·포커스·퇴장 애니메이션 직접 변경 없음. Animator.Play는 공통 대상이므로 다른 모드가 이 메서드를 패치하는 경우 별도 순서/범위 검토가 필요하다.
