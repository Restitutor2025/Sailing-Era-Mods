
## 2026-09-26 18:47 설치 완료

[에디터 PR135](https://github.com/Restitutor2025/Sailing-Era-Editor/pull/135) 병합·실행용 main 반영, Core0.14.0.0을 게임 Mods와 에디터 core/release 양쪽에 설치했다. 게시 소스 d8f7c8e16b3bee6cb84c8c762c9c6c8774db024a, 병합9ecd36d. 최종 SHA256 `2054b5c6897e9f29ef473ec54c3a2504fb7beb28b5d9b01589b43ffdae9a66e1`, 두 설치 대상 일치. 변경 전0.13.0.0/5199a131…e3b2 백업은 게임 UserData/CityEditor/deployment-backups/city-development-20260926-184742-522a5d/core-{0,1}.dll 및 receipt.json. 배포 원본은 에디터 작업 폴더 core/release.

전체15묶음1899/1899 통과(439초), 최종GUI215/215·대체14/14·개발13/13 재검사, 실제 관리 코드28검사 포함. 최종 빌드 경고0 오류0. 사용자 도시 JSON·원본 게임·DLC 변경 없음, 게임/Steam 미실행. 실전 해금·저장 왕복·NPC 항해·병용은 미확인.

이전 준비 기록:

# 도시 개발 단계 — Core0.14.0 준비

모든 기존/신규/최종 제작 도시에서 초기 개방 시설·순차 TalkPart/GameEvent 완료 단계·추가 시설·NPC 방문 허용을 편집한다. 새 게임 계획과 완료 단계는 세이브 내부에 저장한다. 사용자 도시 설정 자동 변경 없음.

소스는 에디터 저장소 feat/city-development. [계약](PATCHES.md) · [버전/검증/배포](0.14.0.md). 빌드/오프라인 검증과 실제 설치·사용자 실전은 구별한다.

설치 후 실행용 에디터 main 개발기능13/13 통과(12초). 공개 배포 기록과 최종 메타데이터는 [PR136](https://github.com/Restitutor2025/Sailing-Era-Editor/pull/136)으로 main에 반영했다.
