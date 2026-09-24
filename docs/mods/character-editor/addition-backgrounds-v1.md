# Addition 배경 선택 v1 — 2026-09-24

에디터 실행 시 Addition/Conversations/Background 하위 PNG/JPEG/WebP/BMP를 재귀 검색하고 두 배경 선택기에 [추가]로 등록한다. 상대 경로를 문서에 저장하고 기존 TalkArt/ArtCache에서 RGBA로 미리본다. 파일 변경 후 재시작 필요. 손상 파일은 로그 후 제외. 게임 런타임 및 DLL 내장은 미구현.

소스 95b87b7, GitHub PR https://github.com/Restitutor2025/Sailing-Era-Editor/pull/70 (미병합). 실제 에디터에 해당 여섯 Python 파일만 적용, 원본 백업 및 SHA256 일치 확인. main 작업 폴더의 해당 변경은 의도된 설치본이다. 다른 버전으로 덮어쓰지 않는다. 기록: output/addition-backgrounds-install.json. Core DLL/게임 변경 없음.

검증: Core 366/366, GUI 163/163. 실제 설치 폴더에서 Constantinople_1453_rain_fire_RGBA.png 등록 및 1920x1080 RGBA 8294400 bytes 읽기 확인. 수동 UI/게임 실전 미확인.


## PR 병합·pull 완료
2026-09-24 PR #70 병합, main bcf6aa81d25d5f435882773571c75c52d0329246까지 실제 에디터 fast-forward 완료. 이전 미병합/설치 변경 상태는 해소됨. main 대화 분할·합치기 UI 보존. 검사: Character 248/248, Dialogue 35/35, 최신 GUI 165/165. 이전 설치 여섯 파일은 stash backup-addition-before-pr70-pull로 보관, 재적용 불필요. Core0.7.7 배포 DLL SHA256 5a205a288287ea70f410ac4cb5da149e0ebb28c07f362ab3b607989f1ec4a312 유지.
