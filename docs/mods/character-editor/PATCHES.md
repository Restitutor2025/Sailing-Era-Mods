# 0.1.0 에디터 접점

MainWindow 선택 집합/창 참조, HeroesTab 선택 수명, 통합 이벤트/대화 창은 하나의 View/CharDoc와 undo를 공유한다. Q/E는 활성 대화 창의 입력란 외부에서만 소비. 그림은 기존 메모리 캐시. 새 게임 후크/원본 런타임 변경 없음.


2026-09-24 에디터0.1.1: startTalk/startEvent 카드와 설치 추가 대화 읽기 연결. 원본 표 별도 보완본, 파일 쓰기 없음. 문서 설정(0 포함)이 설치 설정보다 우선. docs/mods/character-editor/0.1.1.md 참조.


## 2026-09-24 Addition 배경
[기능·검증·설치 기록](addition-backgrounds-v1.md). MainWindow 시작 적재, TalkCatalog.bg, shared_art 이미지 경로 매핑만 확장. 원본 표 및 게임 후크 변경 없음.


## 2026-09-24 음원 미리보기
[50% 음원 재생/정지·검증·배포 기록](audio-preview-v1.md). UI 공유 미리보기 수명/메모리만 추가, 게임 후크 없음.


## 2026-09-24 이벤트용 음원 목록
[시설 음성 제외·선택 범위·검증·배포](event-sounds-v1.md). TalkCatalog와 두 선택기의 표시·허용값만 확장. 게임 후크/재생 정책 변경 없음.
