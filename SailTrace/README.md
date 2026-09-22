# SailTrace 0.1.2 — 항해 스터터링·검은 지형 진단 (관측 전용)

게임 동작을 바꾸지 않는다: 원본 생략·인수/반환값 변경·GC 호출·강제 로드 없음. 바다 장면에서만 기록한다.

## 빌드 (게임을 끈 상태 불필요, 설치 시에만 끔)
PowerShell에서:
    powershell -ExecutionPolicy Bypass -File E:\Documents\ChatGPT\Sailing_era_Restitutor\SailTrace\build.ps1
결과: `Analytics\Restitutor_Analytics_SailTrace.dll`, 로그 `SailTrace\build.log`.

## 설치
게임을 끄고 DLL을 `Sailing Era\Mods\Analytics\`에 넣는다(폴더에 manifest.json 필요 — 기존 폴더를 지웠다면 다른 분류 폴더의 manifest.json을 복사). 제거는 파일 삭제.

## 기록
`UserData\Restitutor\SailTrace\SailTrace-*.jsonl` (실행당 1개, 128 MiB 상한). MelonLoader 로그에 경로와 훅 성공/실패 수 표시.
- `sec`(1초 요약): fps, 최대 프레임 간격, 타일 요청/도착/해제/취소, 5초 내 재요청(churn), 도착 지연, 진행 중 수, 요청 분류별 수, NPC 함선, GC 카운터·힙, 카메라 위치·수평 속도, 전투 여부
- `gap`(40ms 이상 프레임, 0.1.1): 그 프레임 안의 타일 도착·해제 수, 직전 1초 합계, GC 증가
- `tile_req / tile_arrive / tile_unload / tile_dispose`: 타일 이름, 카메라 뷰포트 좌표(0~1 안이면 화면 안), 생존 시간
- `area`, `npc_boat`, `quality`, `terrain_quality_change`

## 테스트 절차 (세이브 사본 먼저)
같은 항로(예: 제다↔말라카 홍해 구간)를 각 2분씩, 순서대로:
1. 품질 낮음, X1, 카메라 고정
2. 품질 낮음, X5, 카메라 고정
3. 품질 높음, X5, 카메라 고정
4. 품질 높음, X5, 카메라 계속 회전
각 조건 시작 때 채팅 기록용으로 시각만 적어 두면 된다(게임 내 조작 불필요). 끝나면 JSONL과 Latest.log를 보낸다. 가능하면 4번은 녹화.
