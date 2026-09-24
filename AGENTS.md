# 프로젝트 작업 규칙
- 게임과 Steam을 실행·조작하지 않는다. 실전 테스트는 사용자만 수행한다.
- 먼저 analysis/INDEX.md만 읽고 요청과 관련된 시스템 문서 및 docs/mods/<모드>/CURRENT.md를 선택한다.
- 모든 Markdown, 버전 이력, native-map.json을 일괄 읽지 않는다. 원문은 함수/타입으로 검색한다.
- 변경 전 해당 모드 PATCHES.md와 docs/mods/COMPATIBILITY.md에서 함수·공유 상태·입력·수명주기를 확인한다.
- 문서는 원본 대체물이 아니다. 대상 바이너리/소스가 기준과 다르면 관련 근거를 재대조한다.
- 확인된 메타데이터, 확인된 본문, 추론, 사용자 실행 보고를 구별한다. 문서만으로 충돌 없음/실전 성공을 선언하지 않는다.
- 변경 후 CURRENT, 패치 목록, 해당 버전 문서와 근거를 함께 갱신한다. 과거 기록은 보존한다.
- 버그 분석·성능 진단 작업은 docs/handoff/DIAGNOSTIC_PRINCIPLES.md의 원칙과 절차를 따른다. 그럴듯한 원인을 바로 수정하지 말고 다른 후보와 구별할 근거를 먼저 확인한다.

- 배포·설치·업로드마다 GitHub에 소스 커밋/PR, 버전, SHA256, 배포 원본·설치 대상, 검증 상태를 기록한다. 에디터 배포 원본과 게임 설치본을 함께 확인해 다운그레이드를 막는다. 공개 업로드 승인이 보류된 경우 게시 완료로 보고하지 않는다.

- 에디터 저장소(`E:\Program\Sailing_Era_Editor`, GitHub Sailing-Era-Editor)의 코드 — City/Character/Ship Editor 와 CityEditor Core(그 저장소 `core/`) — 는 이 저장소에서 고치지 않는다. `output\` 등에 사본을 만들어 작업하지 말고, 그 저장소 `AGENTS.md`(작업 폴더 `scripts\new-worktree.ps1`)를 따른다. 이 저장소의 `CityEditorCore\`·`CityEditor\` 는 이동 안내만 남은 폴더다. (2026-09-24 구조 리뷰: 코어 0.6.0~0.7.7 이 `output\hero-*` 사본에서 작업돼 버전이 갈라졌음)
- 이 저장소 `Core\` = Restitutor.Core(모드 공용 라이브러리). 에디터 저장소 `core/`(Restitutor_CityEditor_Core, 에디터 적용 모드)와 다른 것이다.
