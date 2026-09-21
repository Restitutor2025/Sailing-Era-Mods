# Trade Exp 0.1.1 — 현재 배포본

무역 정산 보유 경험치 표시 상한을 **2,147,483,647**로 변경했다. Int64 덧셈과 overflow 검사를 거쳐 0~Int32.MaxValue로 제한한다. 지급·저장 계산은 유지한다.

DLL: [Bug_Fixes/Restitutor_fixes_trade_exp.dll](../../../Bug_Fixes/Restitutor_fixes_trade_exp.dll). [계약](PATCHES.md) · [0.1.1 근거](0.1.1.md).

Release 경고/오류 0. 실제 교체 기계어의 독립 산술 검사 1,132개 통과. 실전 UI/병용은 사용자 검증 전이며 게임 Mods에는 설치하지 않았다.

아래는 이전 제작 기록이다.

# Trade Exp 0.1.0

무역 정산 화면의 보유 경험치 합계 999,999 표시 제한과 해당 MAX 표시를 제거한다.
DLL: [Bug_Fixes/Restitutor_fixes_trade_exp.dll](../../../Bug_Fixes/Restitutor_fixes_trade_exp.dll).

지급량·경험치 DB·저장 형식은 수정하지 않는다. 화면의 기존 Int32 합계 계산을 유지하므로 2,147,483,647 초과는 지원 범위 밖이다. UI 폭/숫자 애니메이션/실전 병용은 사용자 검증 전이다.

Release 빌드 경고/오류 0 및 원본/분기 정적 검사 통과. 게임/Steam 실행 및 게임 Mods 폴더 설치는 하지 않았다.
[패치 계약](PATCHES.md) · [버전과 근거](0.1.0.md).
