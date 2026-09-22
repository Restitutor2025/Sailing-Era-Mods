# Cheats Skill 1.1.1 (2026-09-22) — Rebalance Growth 동작 여부로 추가 지급 양보

게임 확인 전. SHA256 `047C0FD8…42B7`. 1.1.0 과 기능 같음. 변경: 10레벨(연속) 레벨업 추가 포인트를 Rebalance Growth 가 **로드돼 있을 때**가 아니라 **규칙이 동작할 때**(`Restitutor.RebalanceGrowth.EntryPoint.ExtraGrantActive`, 리플렉션)만 양보. Rebalance 가 GameAssembly 해시 불일치로 꺼지면 이 모드가 직접 지급 → 포인트 누락 없음.
