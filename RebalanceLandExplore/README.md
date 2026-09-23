# Restitutor Rebalance LandExplore

육지 탐험(역참에서 편성) 판정에 쓰는 **행운의 동전** 확률 조정.

- 동전 성공 확률 **상한 50%** (하드 클램프: 50 초과만 50으로 내림, 그 이하는 그대로).
- 원본 버그 수정: 화면에 뜬 숫자 = 실제 확률. 원본은 `Random.Next(0,100) <= 표시값` 이라 항상 **1%p 더** 성공했다(표시 0% = 실제 1%).
- 0% 는 그대로 0%(동전은 소모되고 반드시 실패) — 사용자 결정 2026-09-23.
- 판정 자체(능력치 ≥ 요구치), 동전이 **확률이 가장 높은 인물** 기준으로 굴러가는 원본 동작, 동전 소모 개수는 건드리지 않는다.

구성: 소스 `src/`(Rules.cs = 순수 규칙, EntryPoint.cs = 훅 2개), 검사 `tests/`.
빌드·설치: 게임 종료 후 `powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1` → `Mods\Rebalance\`.
필요: `UserLibs\Restitutor.Core.dll` 0.1.0 이상. 문서: `docs/mods/rebalance-landexplore/0.1.0.md`.
