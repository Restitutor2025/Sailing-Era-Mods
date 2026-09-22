# Cheats Skill 1.1.0 — 간격 선택지 1/2/5레벨, 기본 5 (2026-09-22)

- 선택지 5/10/15 → **1/2/5**, 기본 15 → **5**(사용자). 저장값(`UserData/Restitutor/Cheats/skill.v1.json`)이 10·15면 선택지가 아니므로 기본 5로 읽힌다(로그 `invalid 15; default 5`).
- 치트 켜짐(O on): 선택 간격이 `LevelGetSkill` Postfix 로 적용 — Rebalance Growth 의 게임 값 5보다 우선(사용자). 치트 꺼짐: 게임 값(Rebalance Growth 설치 시 5, 없으면 원본 15).
- 10레벨 버튼 추가 지급: **"Restitutor Rebalance Growth" 가 로드돼 있으면 이 모드는 지급하지 않는다**(Rebalance Growth 가 실제 적용 간격으로 지급 → 이중 지급 방지). 없으면 1.0.5 와 같이 직접 지급. 판정은 첫 10레벨 클릭 때 `MelonBase.RegisteredMelons` 1회 조회 후 캐시.
- 훅·패널 구조 변화 없음(4개). 빌드 경고/오류 0, `Cheats/skill-tests` PASS 4. SHA256 `14D7FF3FEF7B48299D8F31CE20C53D6364D12BCC484AA939189AAA7778971EBA`. 게임 확인 전.
