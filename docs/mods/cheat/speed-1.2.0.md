# Cheats Speed 1.2.0 — FixedUpdate 훅 제거

2026-09-23. 기능 동일: 일반 항해(Free 상태)에서 플레이어 기함만 X1~X5 이동, 전투·입항·함대 전환·지도 게이트는 1.1.2 와 같음.

## 왜
- 1.1.2 는 `BoatEntityOceanDriver.FixedUpdate` 에 Prefix+Finalizer. Harmony 훅은 **인스턴스가 아니라 함수(모든 배가 공유하는 코드)** 에 걸리므로, 플레이어 배만 바꾸려 해도 바다의 배 40~67척 × 초당 50회 = 초당 2,000~2,900번 훅이 돌았음(프리픽스가 안에서 플레이어 배만 골라냄).

## 방법 (훅 0개, 역어셈블 근거 handoff/HOOK_CENSUS_RESULTS.md S절)
- 속도식(FixedUpdate 0x104AEA0): 힘 = enginePower × forwardPowerFactorByEscape(+0x64) × 기타 계수 × GameModuleSpeedScale(일시정지 중 0).
- `forwardPowerFactorByEscape` 를 쓰는 원본 코드는 생성자(1.0)·도주(BoatEntityEscape/MoveToEscapeAct/MoveToPortAct 1.5) 뿐 — 매 프레임 쓰지 않음.
- 패널 Refresh 가 프레임당 1번 부르는 `SailingSpeed.Update` 에서, `OceanScene.FocusBoatReference.LeaderDirectionMove`(플레이어 기함 driver) 하나에 원래값×배수를 써 두고, 배수 X1·게이트 닫힘·기함 교체·치트 OFF·리셋 때 원래값으로 복원. 게임이 그 사이 값을 바꾸면 그 값을 새 원래값으로 삼음(1.1.2 의 foreign-write 보존과 같은 규칙).
- 일시정지 별도 처리 불필요(원본 속도 곱이 0).
- 차이: 게이트가 닫히는 순간(입항·전투 진입)부터 다음 프레임까지 물리 스텝 0~2회는 배수가 남을 수 있음(1.1.2 는 즉시). 체감 차이 없을 것으로 봄[추론].

## 검증
- Cheats 검사 PASS 175(속도 검사 재작성: X1~X5, 게이트 9종 복원, 일시정지, 기함 교체, 비플레이어/실패 배, 비정상 원래값, 게임 쓰기 보존, 배수 변경 재계산, 리셋). VM 빌드 0/0. 게임 실행 확인 없음.
- SHA256 `d1ef6b0d037d03cbb5e20589e5369569fb3f0626248d3677e520107b97e9d1f6`.
