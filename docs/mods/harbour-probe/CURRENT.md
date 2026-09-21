# harbour-probe — 현재 상태

## 0.1.0 (2026-09-20) — 작성, 빌드·실전 확인 전
- 목적: CityEditor 새 도시 배치 선행 조사. 항로도 노드(PrefabLaneNode) · 입항 오브젝트(MapHarbourEnterCtrl) · 범위 검사(MapRangeChecker 중심/반경) · BoxCollider · 항구 표식 · 도시 3D 메쉬(가까운 렌더러)의 관계 관측.
- 소스 `HarbourProbe/`, 빌드 `HarbourProbe/build.bat`, 설치 `Mods/Analytics/Restitutor_Analytics_HarbourProbe.dll`, 기록 `UserData/Restitutor/HarbourProbe/harbour-*.jsonl`.
- 훅 11개(모두 관측, 원본 동작 불변): MapHarbourEnterCtrl Start(post)·OnEnterRange(post)·OnExitRange(post)·OnSceneActionEnterHarbour(pre)·OnDestroy(pre), MapRangeChecker.InitRangeAction(post), OceanScene AddPortNote·ShowEnterPortNote·ShowPortNote(pre), PortManager.EnterHarbour(pre+post). 주기: 15초 FindObjectsOfType 탐색, 1초 근접 표본.
- 공유 접점: MapHarbourEnterCtrl.Start 는 instant-entrance 도 Postfix. PortManager.EnterHarbour 는 nav-to-city 도 관측.
- 근거 [메타]: MapRangeChecker 필드 `_focus`(Transform), `center`(Vector3), `_range`(float), `_inRange`, `showGizmosCircle` → 원형 거리 판정 가능성. MapGOIdentifier `type`, `tid`. 본문 대조·빌드·실행은 아직 없음.
- 분석: Sailing_Era_Editor 메뉴 도구 > 입항 조사 로그 분석 (또는 `tools/import_harbour_probe.py`) → `analysis_out/harbour_probe/<로그>/report.md`.
