# Cheats Exp 1.0.0 — 술집 경험치 목표값

2026-09-20 (KST). `Restitutor_Cheats_Exp.dll` 신규. Interface 1.3.0 Panel API 사용, Order=16(소지금 15 아래, 여급 18 위). 다른 치트 DLL은 바꾸지 않는다.

## 사용자 요구와 결정

- 술집 안에서만 보이는 경험치 치트창. Max로 올리기, 직접 입력으로 올리기.
- 입력은 정수만, 음수 불가, 최대치 초과 입력은 최대치로 강제 보정.
- Max값: **2,147,483,647**(Int32.MaxValue). 사용자 선택(2026-09-20). 템플릿 upperLimit 방식은 채택하지 않음.
- 표시 범위: **술집 화면만**. 레벨업 창이 열려 있으면 숨김. 사용자 선택(2026-09-20).
- 빌드: 사용자가 `Cheats/Exp/build.cmd`를 게임 종료 상태에서 실행. 클라우드 세션은 .NET SDK 설치가 조직 정책으로 막혀 빌드하지 못했다.

## 동작·계약

- 표시 조건: Interface 활성, Host.Player = PlayerDataManager.Data, 장면 진입 완료·비로딩, 항구 장면, 입항 상태, 현재 도시의 `UIDrunkeryCtrl` 뷰가 열려 있고 부모 체인까지 보임, `UIHeroLevelUpCtrl` 뷰가 열려 있지 않음. 조건 밖에서는 패널 전체를 숨긴다(취소선 없음). 여급 판정 코드는 없다(여급 로직은 Bargirls 전용 규칙).
- 입력: `GTextInput.restrict=[0-9]`, maxLength 10. 변경 시 숫자 외 문자를 제거하고 2,147,483,647 초과는 2,147,483,647로 바꾼다. 적용 때 한 번 더 같은 보정을 한다. 빈 입력은 적용하지 않는다.
- 적용: 목표값 **절대 설정**. 현재값보다 낮은 값도 설정한다(소지금 치트와 같은 규칙. 사용자에게 명시적으로 확인받은 사항은 아님 — 요청 문구 "올릴"에서 낮추기 금지 여부는 정하지 않았다).
- Max 버튼: 즉시 2,147,483,647 적용. 현재값이 이미 2,147,483,647이면 비활성.
- 게임 상태 쓰기: `PlayerCurrencyDB.ModifyAmount(2,target)` 최대 1회. 현재값과 같으면 호출 없음. 반환값과 적용 후 `FindCurrency(2).Amount`를 확인하고 실패·불일치·예외에서 재시도하지 않는다. 경험치 기록(통화 2)이 세이브에 없으면 패널을 비활성화하고 생성하지 않는다.
- Harmony 훅 0개. 직접 Amount setter, EarningAmount 변경, 강제 저장, UI 갱신 함수 호출 없음.

## 확인된 근거 [본문]

GameAssembly SHA256 `50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA`. 본문은 GNU objdump로 추출. [native.json](../../../Cheats/evidence/exp-1.0.0/native.json)과 같은 폴더 asm 5개.

- `IncreaseExpAmount` RVA 0x452A30: `IncreaseAmount(2, value)`로 점프 → 경험치 = 통화 ID 2.
- `ModifyAmount` RVA 0x452AC0: `FindCurrency(id)` 없음 → false, 값 음수 → false, 아니면 Int32를 부호 확장해 Int64 Amount(+0x18)에 대입, `MarkDBDirty` 호출, true. **상한 검사 없음.** 인자가 Int32이므로 이 경로로 쓸 수 있는 최대치는 2,147,483,647이다.
- `IncreaseAmount` RVA 0x452740: 기존 통화는 Int64 Amount/EarningAmount에 더함(상한 없음). 통화가 없어 새로 만들 때 ID 2만 999,999(0xF423F)로 자른다. 템플릿 `upperLimit`(Gyyx.Template.Currency +0x38)은 이 함수와 ModifyAmount에서 읽지 않는다. 경험치 upperLimit의 실제 값은 데이터 번들에 있어 확인하지 않았다.
- `UIHeroLevelUpModel.set_Exp` RVA 0xB80D20: Int64 필드 +0x28에 대입 후 갱신 호출. `.text`/`il2cpp` 구역 E8 직접 호출 탐색에서 set_Exp·ModifyAmount의 직접 호출자는 0개(인라인·간접 사용은 배제 못 함). **레벨업 창이 열릴 때 경험치를 모델에 복사해 두는지는 확인하지 못했다 [추론].** 레벨업 창에서 숨기는 결정은 이 불확실성을 피하기 위한 것이다.

## 검증 상태

- 이 세션: 빌드·테스트 미실행(.NET 없음). 사용한 interop 이름(UIDrunkeryCtrl, UIHeroLevelUpCtrl, HarborId, _alreadyOpenedUICtrls, StayInPortId, IsStayInPort, FindCurrency, ModifyAmount, get_PlayerCurrency, get_Amount, IsInHarborScene)이 설치 interop Assembly-CSharp.dll 문자열에 있는 것만 확인. 같은 API는 Bargirls 1.1.2·Skill·Money가 이미 빌드에 사용.
- 관리 검사: `Cheats/exp-tests` (Rules.cs 실제 코드, 29개). build.cmd가 실행한다.
- 게임/Steam 실행 없음. 실전 미검증.

## 사용자 확인 항목

1. 술집 화면에서만 패널이 보이는지. 술집 밖·레벨업 창·여급 화면에서 숨는지(여급 화면에서 술집 뷰가 계속 보이는 상태로 판정되는지는 미확인).
2. `-`, `.`, `,` 입력 불가. 2147483648 이상 입력 시 즉시 2147483647로 바뀌는지.
3. 적용·Max 후 현재 경험치 표시, 레벨업 창에서 새 값으로 레벨업 가능한지.
4. 21억 가까운 경험치로 레벨업(여러 단계 버튼 포함) 시 계산 이상 여부 — 레벨업 쪽 Int32 계산 경로는 조사하지 않았다.
5. 저장 후 재로드 시 값 유지.
6. 세이브 사본을 먼저 만든다. DLL을 빼도 바뀐 경험치는 돌아가지 않는다.
