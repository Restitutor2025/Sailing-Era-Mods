# Cheats Contribution 1.1.3 — 능력치 조회 훅을 '적용' 동안만 설치

2026-09-22. 기능 변경 없음(공헌도 입력 → 적용 → `WorldPortHoldDB.UpdateInfluence` 1회, 사령관 보정 2개를 그 1회만 중립화).

## 원인 (handoff/HOOK_CENSUS_RESULTS.md K·L·N절)
- 1.1.2 까지 `BaseObjectData.GetPointProperty`·`GetProperty` Prefix 를 모드 로드 때 설치해 게임 내내 유지. 실제로 필요한 건 '적용' 버튼 1회의 UpdateInfluence 동안뿐.
- 게임 월 넘김(`PortScheduleManager.OnTheMonthRefresh`)이 한 프레임에 GetPointProperty 를 약 6.5만 회 호출(교역소 × 모든 교역품 시세). 훅 없음 18.9 ms 측정, 같은 함수에 관리 훅 1개가 있을 때 172 ms 끊김 관측.

## 변경
- 로드 시 훅 없음. `Apply` 안에서 `InstallHooks()` → ModifierScope + UpdateInfluence → `finally RemoveHooks()`(UnpatchSelf).
- 해제 실패 시 예외를 로그로 남기고 그 세션 동안 훅 유지(`keepHooks`, 1.1.2 와 같은 상태) — 반쯤 해제된 상태로 두지 않음.
- 로그: 로드 `GetPointProperty/GetProperty hooked only during Apply`, 적용마다 `Apply hooks: installed+removed in N ms (still installed=False)`.
- 위험: Il2CppInterop 런타임 Unpatch 는 이 프로젝트에서 처음 실사용(이전엔 실패 경로에서만). 게임 확인 필요.

## 검증
- VM 빌드 경고/오류 0, Cheats 검사 PASS 171(contribution regression 포함). 실행 확인 없음. SHA256 `2d93e22dce188e661d0deb52e72cb8e33821e97327c8efbd55b18764c5afa646`.
- 게임 확인: ① 도시에서 공헌도 적용 → 값이 목표와 같음, 로그 `still installed=False` ② 두 번째 적용도 정상 ③ 월 넘김 때 SailTrace `month_refresh.costMs` 가 약 20 ms 수준(치트 켠 상태).
