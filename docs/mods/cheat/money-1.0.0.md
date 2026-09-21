# Cheats Money 1.0.0 — 소지금 목표값

2026-09-19. Restitutor_Cheats_Money.dll 신규 추가. Interface 1.1.1의 Panel API 사용, Order=15로 공헌도(10) 바로 아래/배속(20) 위에 표시한다. 기존 세 DLL은 교체하지 않았다.

## 동작·계약

현재 소지금 표시와 목표값 입력/적용. 입력 범위 0~2,147,483,647, 초과 입력은 상한으로 보정하며 입력만으로 금액을 변경하지 않는다. 도시/항해/지도/일시정지/전투별 제한은 두지 않는다. Host.Enabled, 현재 PlayerDataManager.Data와 Host.Player 동일성, SceneEntered 및 비로딩, 소지금 데이터 존재를 확인한다. 적용 시 표시 플레이어와 현재 플레이어를 재확인한다. 세션 Reset/뷰 해제에서 입력과 표시 참조를 지운다.

Harmony 훅 0개. 적용 버튼에서 PlayerCurrencyDB.ModifyAmount(1,target)를 최대 1회 호출한다. 현재값과 같으면 호출 없음. bool 반환과 실제 잔액을 확인하고 실패/불일치/예외에서 재시도하지 않는다. 직접 Amount setter, 수입 누계 변경, 강제 저장, UIManager.RefreshMoney 호출 없음. RefreshMoney는 원본 애니메이션 갱신 함수이므로 임의 호출하지 않는다.

## 확인된 근거

- 메타데이터/현재 본문: GameAssembly SHA256 50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA 기준 일치.
- GetCoinCurrencyAmount RVA 0x452710: 통화 ID=1 조회 후 64비트 Amount 읽기.
- ModifyAmount RVA 0x452AC0: 통화 존재 및 음수 거부, int 목표를 64비트 Amount에 대입, MarkDBDirty 호출. 증감 함수가 아닌 절대 목표값 설정이다. GameMasterManager.ModifyCurrencyAmount RVA 0x4E8120에도 같은 설정/dirty 경로가 있다. 따라서 큰 기존 잔액에서도 차액 int 오버플로 없이 낮출 수 있다.
- MarkDBDirty는 DB dirty 상태를 설정하고 변경 통지 경로로 연결한다. 실제 각 게임 화면의 즉시 갱신/저장 후 재로드는 사용자 검증 전이다.
- [선택한 원본 본문·해시](../../../Cheats/evidence/money-1.0.0/native.json), 같은 폴더의 asm 6개. 재현: python Cheats/evidence/money-native.py.
- 빌드 경고 0/오류 0. 실제 Rules 코드의 관리 검사 24개, 기존 Host 대역 수명주기 검사 61개 통과. 관리 검사는 네이티브 실행 검사가 아니다.
- [IL 검사](../../../Cheats/evidence/money-1.0.0/assembly-audit.txt): Interface 의존, ModifyAmount 1개 호출 위치, 신규 훅/직접 Amount setter 없음. 기존 기능 소유권 검사 통과.
- 설치 Interface 해시 EEAE9AAD320452B3DBAC2CB4140C714994AE3A361B686DFF575EDF15FAA4A140는 현재 빌드와 일치한다. 루트의 과거 Interface 복사본을 설치하지 않았다.

## 배포·한계

설치: E:/Program/steam/steamapps/common/Sailing Era/Mods/Cheats/Restitutor_Cheats_Money.dll
SHA256: D17CE9954B5A39E2515B35FD660D9FE2261C8996528F2E06C17E04508C331C54
빌드/루트/ releases/money-1.0.0/설치본 해시 일치. 기존 DLL/manifest 유지.

게임/Steam 실행·조작 없음. 실제 UI 배치, 도시/항해/지도/전투 중 적용, HUD 반영, 저장/재로드 및 외부 모드 병용은 사용자 검증 전. 외부 모드와 PlayerCurrency 및 dirty 통지를 공유하므로 훅이 없다는 이유만으로 무충돌을 선언하지 않는다. 특히 진행 중 결제·보상은 잔액을 관측할 수 있다.

사용자 확인: 공헌도 아래 패널에서 금액 상승/감소/0 적용, 도시·지도·항해에서 사용, 저장 후 재로드, 캐릭터 저장 전환 시 이전 입력 초기화.
