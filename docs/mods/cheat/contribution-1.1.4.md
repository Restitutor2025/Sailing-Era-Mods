# Cheats Contribution 1.1.4 — 네이티브 훅 없이 공헌도 설정

2026-09-22. 사용자 기능 동일: 도시 공헌도 입력 → 적용 → 정확히 그 값. 1.1.3 대체.

## 왜
- 1.1.2: `BaseObjectData.GetPointProperty/GetProperty` Prefix 상시 → 게임 월 넘김(GetPointProperty 약 6.5만 회/프레임) 19 ms → 약 300 ms.
- 1.1.3: 적용 동안만 Patch → 적용 뒤 `UnpatchSelf` 가 성공 보고해도 **Il2CppInterop 네이티브 우회 비용이 세션 끝까지 남음**(6회차 303 ms, 7회차 적용 안 하면 19 ms — handoff/HOOK_CENSUS_RESULTS.md P·Q절).

## 방법 (훅 0개)
- 원본 `WorldPortHoldDB.UpdateInfluence` 0xB78FA0 식(역어셈블): 양수 delta 는 플레이어가 해당 문화권에 머무를 때 `RoundToInt(point133 × delta × 0.01)` 로 먼저 바뀜 → `(int)(((float)property21/100 + 1) × delta)` → 0~1000 제한 → 이벤트 "OnPortInfluenceUpdate".
- 사령관의 property 21·point 133 값을 읽어(호출만, 훅 없음) 결과가 목표에 가장 가까운 delta 를 골라 원본을 그대로 호출, 남은 차이는 추가 호출(최대 8회)로 맞춤. 문화권 보정 적용 여부는 첫 양수 호출 결과로 판별.
- **중간값이 시작값·목표값과 다른 100 단위 구간으로 넘어가지 않게** 선택(해금 기준 100/200/400/600/800 을 지나쳤다 돌아오며 해금·알림이 잘못 나오는 것 방지). 목표 1000/0 은 한 번에 ±2000.
- 정확히 못 맞추는 경우(보정 배율 ≥ 2, 즉 property 21 ≥ 100 등): 경고 로그 + 창에 "보정 때문에 정확히 못 맞춤" 표시(예외 아님).

## 검증
- 시뮬레이터 `Cheats/contribution-sim/`(선택 코드 그대로 + 원본 식 재현): rate −50~99%, 문화 보정 없음/80/110/125/150/200%(적용·미적용), 시작 8 × 목표 10 = 9,000 경우 모두 정확, 구간 넘김 0, 최대 호출 4회.
- VM 빌드 경고/오류 0, Cheats 검사 PASS 171. 게임 실행 확인 없음. SHA256 `416B3ACFE190D2B9A67B6261DDC7BC512EB67FE8251A72DA910E36246BFC2DBD`.
- 게임 확인: ① 적용 몇 번 → 값 일치, 로그 `UpdateInfluence calls=N (native, unhooked; rate x…, culture …)` ② 적용 후 월 넘김 → SailTrace `month_refresh` ≈ 20 ms.
