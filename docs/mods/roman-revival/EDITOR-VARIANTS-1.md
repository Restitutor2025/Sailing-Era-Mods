# 대체 도시 설정 작성 기능 1 — 2026-09-26

사용자 요청: 기본 도시와 다른 도시 설정을 작성할 수 있도록 Sailing Era Editor 수정.

## 구현

- 기존 도시 선택 → **대체 도시 설정…** → **새 대체 설정**. 기본 도시를 복사해 별도로 편집한다.
- 이름·약칭·국가·문화·기본 수치·외관·역참·교역품·산지·허가 품목·상점 판매 목록 지원.
- 생성·복제·설정 이름 변경·삭제·취소·값 undo/redo·파일 저장/복원. 메인 문서에서는 전체 변경이 한 번의 undo 단위다.
- 기존 Port ID 유지. 이스탄불145와 콘스탄티노플 대체 설정이 위치·항로·여급·입항/출항을 공유한다.
- `cities.<tid>.variants` / 도시 JSON `editorState.cities.<tid>.variants`에 지원 필드 전체값 보관.
- 기본 도시의 나중 수정과 대체 설정은 분리된다. 가격·재고는 에디터의 문화권/생산 규칙에 따라 계산된다.
- 기본 게임 적용 ops 불변. 이벤트 후 전환 및 세이브별 영구 유지 기능은 아직 미구현이다.

## 소스와 검증 상태

- 에디터 전용 작업 폴더 `E:/Program/Sailing_Era_Editor-worktrees/feat-romanos-dlc`, 브랜치 `feat/city-variants`.
- 에디터 버전0.3.0 유지(기능 PR에서는 버전 미증가 규칙). 대체 설정 schema version1.
- 근거: 에디터 `docs/CITY_VARIANTS.md`, `tests/test_city_variants.py`, exporter `payload.py` 및 Core `Payload.cs`.
- [기능 PR #133](https://github.com/Restitutor2025/Sailing-Era-Editor/pull/133) 병합 완료. 소스 `caa19456d3055d085b0a6305ad8374dd8983f879`, 병합 `1acd5463330391c974fa55bf63d4debe7256c1fc`.
- `E:/Program/Sailing_Era_Editor` main 실행 폴더 반영 완료. Python 실행 파일10개가 배포 원본 SHA256과 일치한다. 전체 변경 전후 해시는 에디터 `docs/CITY_VARIANTS_DEPLOYMENT.md`에 기록했다.
- 전체 검사14묶음1885/1885 통과(360초). 후속 창 해제/메인 문서 연결 변경 후 대체 설정14/14 통과(7초). 실제 설치 폴더에서도14/14 통과(8초).
- 게임 Core/에디터 배포용 Core: `5199a1318118d79507046472a7c4c419353c06cf06b3348551efe4396865e3b2`, DLC: `4d99517fb8e6e4a879fdb0f3a9538eeee0114729dd8c50dd9989dfbf10756649` 유지 재확인. DLL 교체 없음.
- [GitHub 배포 완료 기록](https://github.com/Restitutor2025/Sailing-Era-Editor/pull/133#issuecomment-5844658158), [설치 문서 PR #134](https://github.com/Restitutor2025/Sailing-Era-Editor/pull/134).
- 사용자 cities/patches/게임 세이브는 수정하지 않았다. 예제 콘스탄티노플은 임시 검사 데이터에만 작성했다. 에디터 재시작 후 이스탄불 선택 → 대체 도시 설정… → 새 대체 설정으로 작성한다.
- Core0.13.0.0, DLC0.1.0.0 설치 파일 유지. 게임/Steam 실행 없음. 실제 게임 전환·저장 재로드는 미확인(전환 자체 미구현).
