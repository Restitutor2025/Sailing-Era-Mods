# REBALANCE_GROWTH_RISKS — 행운의 역할 · 능력치 99 초과 영향 · Rebalance Growth 0.1.0 리소스 점검 (2026-09-22)

기준: GameAssembly SHA256 `50D53D17…` (R11 설치본), table = `analysis/bundle-evidence/r11-table`, 영웅 표 `analysis/hero-growth/hero_growth.csv`.
표기: [본문]=GameAssembly 역어셈블 · [표]=table 덤프 · [메타]=global-metadata/interop · [추정] · [미확인]
조사 범위 한계: AOT 코드(GameAssembly)와 table 만. **HybridCLR 핫업데이트 어셈블리와 EventGraph 에셋(육지 탐험 이벤트 그래프 노드 값)은 보지 않았다** [미확인].

## 1. 능력치를 읽는 방법 [본문+메타]
- 능력치는 `BaseObjectData.ObjectPropertys` 사전의 PointProperty(id = ERolePropertyType: 1 신체, 2 기교, 3 감지, 4 매력, 5 학식, 6 행운, 7 공격, 8 HP).
- `PlayerRoleData.GetPhysicalValue` 등은 **가상 함수가 아님**(메서드 flags 0x86, vtable slot 없음) → 호출처는 직접 호출이거나 `GetPointProperty(id)` 인라인. 전 코드에서 `GetPointProperty`/`GetProperty` 호출 464곳을 인자 상수로 분류해 역할 능력치(1~6)를 읽는 함수를 전수 확인했다(배·항구·화물도 같은 함수와 번호를 쓰므로 함수 이름으로 걸러냄).

## 2. 행운(Lucky, id 6)의 역할
### 2-1. 값 [본문+표]
- `PlayerRoleData.GetLuckyValue`(0x6798A0) = 저장값(PointProperty 6) + (레벨-1) × RoleGrowthType(Hero.luckyGrowth).upValue.
- 동료 항해사 37명: 기본 행운 20(1명만 50), 행운 성장 등급 전원 D, upValue 는 모든 등급 1 → **표시 행운 = 20 + (레벨-1)**. 만렙 200이면 219.
- 레벨업 때 행운은 굴리지 않는다(5개 능력치만). 능력치 상한(ROLE_PROP_MAX_COUNT)도 적용되지 않는다.
- 보상·아이템으로 저장값을 올릴 수 있다(`InnerAddPropLucky`, 보상 아이콘 `icon_reward_property_lucky`). 하한 0만 있고 상한 없음.
- **값이 두 가지다:** `GetLuckyValue`(레벨 성장 포함)와 `GetRolePropByID(6)`/`GetProperty(6)`(저장값만, 레벨 성장 없음). 이벤트 조건 쪽은 뒤의 것을 읽는다 → 레벨이 올라도 조건 판정용 행운은 그대로다(원본 동작).

### 2-2. 행운을 읽는 곳 전수 [본문+표]
| 읽는 곳 | 하는 일 | 현재 데이터에서 효과 |
|---|---|---|
| `PlayerRoleData.CalculateJobTitleScore`(0x67A4D0) | 직책 점수 = Σ 능력치 × 직책별 가중치(행운 포함) | **없음** — 17개 직책 모두 행운 가중치 0 [표]. 게다가 직책 점수로 거는 효과 ID(affectID)가 17개 모두 "0"이고 GameEffect 표(617행)에 "0"이 없다 → 직책 점수 자체가 효과를 내지 않는다 [표+본문 AddCabinJobTitleEffect 0x9CBA50] |
| `RoleSelectorUtils.RoleSelector`(0xE6F320) | 조건식으로 인물 고르기(행운 분기 있음) | **없음** — RoleSelectorCondition 23행 중 행운을 쓰는 규칙 없음 [표] |
| `UIDialogCtrl.SupportRole`(0x13424D0) | 대화 선택지 "능력치 지원"(요구치 대비 비율 표시·성공 판정, 행운 포함 1~7) | **없음** — TalkOption 579행 모두 supportProperty 0 [표] |
| 이벤트 조건 `MainRolePropertyConditionTask`(조건 종류 21), `ENConditionPropertyCheck`(육지 탐험 그래프) | 능력치 id ≥ 값 | 표 쪽 조건 인스턴스에서 행운 사용은 찾지 못함. EventGraph 에셋은 [미확인] |
| Tab 인물창·레벨업 창 | — | **행운을 표시하는 UI 코드 없음**(표시 함수들은 1~5만 읽음) |

**결론:** 이 버전의 AOT 코드와 표 기준으로 행운은 저장·성장만 하고 **게임 결과에 영향을 주는 곳이 확인되지 않는다.** 남은 가능성은 EventGraph 에셋·핫업데이트 코드 [미확인]. 따라서 만렙 200으로 행운이 219까지 올라도 알려진 부작용은 없다.

## 3. 능력치 99 초과 시 영향
### 3-1. 원본도 이미 99 초과를 허용한다 [본문]
- `InnerAddProp*`(보상·책·아이템 경로)는 하한 0만 있고 상한이 없다. 99 상한은 **레벨업 판정(GetResult)에만** 걸려 있었다. → 99 초과 값 자체는 원본 코드가 이미 다루던 값이다.
- ROLE_PROP_MAX_COUNT 를 읽는 곳은 get_MaxProp·get_MaxPropValue 두 getter 뿐, 역할 능력치 관련 타입에 99 하드코딩 비교 없음(0.1.0 조사).

### 3-2. 읽는 곳별 판정 [본문]
| 계통 | 식 | 99 초과 시 | 판정 |
|---|---|---|---|
| 레벨업 판정 GetResult(0xB7FDF0) | 현재 < 상한일 때 +2/+1, 넘치면 +0 | 500까지 성장 | 정상 |
| 최대 HP `UpdateMaxHpValue`(0x679E20) | 레벨업마다 최대 HP += round(RoleLevel(레벨).hpRatio × (1 + 신체/100)) | 신체 99 → 레벨당 +8, 500 → +24 | 버그 아님. **밸런스 크게 변함** |
| 공격 `UpdateAttack`(0x679F70) | 레벨업마다 공격 += round(attackRatio × (1 + 신체/100)) | 99 → +4, 500 → +12 | 같음 |
| 백병전/일기토 `CreateHeroCombatUnit`(0x8175B0) | 공격 = 공격 + 0.5×신체, 기교·감지는 **양쪽 비율**(자기/(자기+상대)) | 비율이라 넘침·음수 없음 | 정상. 적 NPC 능력치는 표상 1~99라 아군이 압도(승패는 사실상 HP 비교 — BOARDING_MELEE_DUEL 3-3) |
| 일기토 피해 | round((1±0.1) × 공격^0.85) | 공격이 커져도 int 범위 안 | 정상 |
| 육지 탐험 운반량 `InitPlayerModelList`(0x1135EF0) | round(신체/3, 1) + 속성22 | 99 → 33, 500 → 166.7 | 밸런스 변화(운반량 5배) |
| 육지 탐험 판정 `ENConditionPropertyCheck` | 성공 = 능력치 ≥ 요구치, 확률 = clamp(능력치/요구치 − 0.2, 0, 1) | 확률은 1.0에서 잘림 | 정상(항상 성공) |
| 이벤트 조건 종류 21, 선택자 종류 2 | 능력치 ≥ 값 | 조건이 쉬워짐 | 정상 |
| 책·장비 착용 조건(IsBookCanLearn, IsEquipCanWear) | ≥ 요구치 | 쉬워짐 | 정상 |
| 직책 점수 | 가중 평균 → 효과 ID "0" | 효과 없음 | 영향 없음 |
| 능력치 막대(RefreshPropBar, 레벨업 창 막대) | GProgressBar max = 상한 값 | 500 기준 | FairyGUI 진행 막대는 비율을 0~1로 자름 [외부 라이브러리 지식, 이 빌드 본문 미대조] |
| 숫자 표시(인물창·선원 고용·대학·육지 탐험) | int.ToString | 3자리 | 글자 칸 넘침 여부는 화면 확인 필요 [미확인] |
| NPC 레벨 성장 `NpcData.InnerAddLevel` | 상한 없음(원본) | 이번 변경과 무관 | — |

### 3-3. 실제로 조심할 점
1. **레벨 행 없음 → NullReferenceException** [본문]: UpdateMaxHpValue·UpdateAttack 은 `GetRoleLevel(현재 레벨)` 이 null 이면 예외 호출(0x355850)로 간다. Tab 인물 팁(RefreshTipsRoleInfo)은 null 이면 나머지 표시를 건너뛴다. get_MaxExp 는 이번에 보지 않음 [미확인]. 100레벨 이상 인물이 있는 세이브를 **레벨표 확장 없이** 열면(모드 비활성화, 게임 업데이트로 기준 해시가 달라져 모드가 꺼짐, 표 검증 실패로 행 추가 거부) 그 인물의 HP·공격 갱신에서 예외, 팁 일부 누락이 난다. 사용자 정책상 모드 제거는 없지만 **게임 업데이트 시 위험**.
2. **원본 버그 증폭** [본문]: `PlayerHoldRoleDB.AddRolePropPhysical`·`RewardUtils.AddRoleProperty`(보상으로 능력치 증가 — 신체일 때만 부르는지는 [미확인])가 `UpdateAttack`·`UpdateMaxHpValue` 를 부르는데, 두 함수는 "레벨 1회분 증가량"을 더하는 함수다 → 보상으로 신체를 얻을 때마다 HP·공격이 레벨업 1회만큼 추가로 오른다(원본 동작). 레벨업 판정 경로(SettlementAttributeValue)는 값을 직접 쓰므로 해당 없음. 상한 변경과는 무관하지만 신체가 클수록 증가량도 커진다.
3. 밸런스: 아군 HP·공격·운반량이 NPC(능력치 ≤99, 레벨 ≤99) 대비 크게 벌어짐. 버그는 아님.

## 4. Rebalance Growth 0.1.0 리소스 점검 [소스+본문]
| 항목 | 빈도 | 비용 | 판단 |
|---|---|---|---|
| 매 프레임 작업(OnUpdate·폴링) | 없음 | — | 없음 |
| GameConst 2개 값 교체 | 표 로드 때 1회 | 사전 조회 2회 | 무시 가능 |
| RoleLevel 검증(2~99행 × 7필드) + 101행 추가 | 표 로드 때 1회 | interop 호출 약 800회, 수 ms [추정] | 무시 가능 |
| 레벨업 버튼·보상 상태 훅 4개(Prefix/Postfix) | 버튼 클릭·보상 연출 | 기록 1개 할당 | 무시 가능 |
| `UIHeroLevelUpView.RefreshRoleData` Postfix | 호출처 RefreshLevelUp 1곳(레벨업 창 갱신 때) | interop 3~4회 + 최대 200회 나머지 연산 | 무시 가능 |
| `UICharacterView.RefreshTipsRoleInfo` Postfix | 호출처 5곳(창 열기, 인물 전환, 스킬/장비 시트 갱신, 데이터 동기화) — 모두 이벤트 | 레벨 99 미만이면 interop 약 6회 후 즉시 반환 | 무시 가능 |
| **시작 시 GameAssembly.dll(57 MB) SHA256** | 게임 시작 1회 | 파일 읽기+해시. VM 측정 해시 23 ms·읽기 117 ms(VM 경로라 느림), 사용자 PC 는 수십 ms 로 추정 | **같은 검사를 Interface·Devil Fruits·Item Rebuild·Stat Rank·Tab Characters·Rebalance Growth 6개 모드가 각자 반복** → 합계 수백 ms 추정 [추정]. 로그상 모드 초기화 구간 약 1.3초 |
| `SHA256.Create()` 미해제 | 1회 | 네이티브 핸들 1개 | 사소함(using 누락) |
| 로그 | 값이 바뀔 때만·오류는 같은 내용 1회 | — | 양호 |

**결론:** 실행 중 느려지게 하는 코드는 없다. 쓸데없는 비용은 **시작 때 57 MB 해시**(다른 5개 모드와 중복)와 SHA256 객체 미해제 두 가지. 개선안(미구현): Core 에 "기준 해시 1회 계산·캐시" 추가 후 각 모드가 공유(모드 여러 개 수정 + Core 변경 → Core 담당 대화와 조율 필요), 또는 이 모드만 파일 크기+해시 캐시 파일(UserData)로 재계산 생략.
