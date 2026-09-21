# CTRL Instant 0.1.6

진단을 제거한 정식판이다. 동작은 0.1.5와 같다. `src/TraceLog.cs`는 빌드 대상이 아니다. [근거](../docs/mods/ctrl-instant/0.1.6.md). 0.1.5 진단판은 `archive/0.1.5/`에 있다.

# CTRL Instant 0.1.5

선단 상호작용 창이 열려도 CTRL 정지를 유지한다. 선택 정지는 창에 넘기고, 창이 닫힐 때 원본이 반환한다. [근거](../docs/mods/ctrl-instant/0.1.5.md). 0.1.4는 `archive/0.1.4/`에 있다.

# CTRL Instant 0.1.4 (진단판)

0.1.3은 게임 시작 시점의 훅 등록 때문에 검은 화면이 났다. 0.1.4는 진단 훅 등록을 첫 항해 진입 뒤로 미뤘다. [근거](../docs/mods/ctrl-instant/0.1.4.md). 0.1.3은 `archive/0.1.3/`에 있다.

# CTRL Instant 0.1.3 (진단판)

0.1.2 동작을 유지하고, 교섭(선단 상호작용 창) 중 시간 흐름 제보를 구별하기 위한 진단 로그를 추가했다. 빌드는 프로젝트 루트에서 `./CTRLInstant/build.ps1`로 한다(이 세션에서는 미실행). [0.1.3 근거·재현 절차](../docs/mods/ctrl-instant/0.1.3.md). 0.1.2 원본은 `archive/0.1.2/`에 있다.

## 이전 기록: 0.1.2

# CTRL Instant 0.1.2

정상 항해에서 HUD가 포커스를 갖지 않아 시작이 거부되는 문제를 수정했다. 시작과 유지 양쪽 검사를 수정하고 진단 로그는 유지했다. [0.1.2 검증·배포 상태](../docs/mods/ctrl-instant/0.1.2.md). 프로젝트 출력 및 실제 Mods/Bug_Fixes 설치본 모두 0.1.2. 사용자 게임 종료 후 교체하고 해시 일치를 확인했다.

## 이전 기록: 0.1.1 진단판

0.1.0은 사용자가 요구 동작 실패를 보고했다. 현재 0.1.1은 기존 로직을 유지하고 입력·가드·정지 토큰 진단을 추가한 버전이다. 해결 여부는 미확정. [진단 기록과 설치](../docs/mods/ctrl-instant/0.1.1.md). 사용자 게임 `Mods/Bug_Fixes`의 해당 DLL을 0.1.1로 교체하고 해시를 확인했다.

## 기존 구현 의도

일반 항해 중 CheckTarget(기본 좌/우 CTRL)을 누르는 순간 원본 게임 일시정지를 획득한다. 선단이 없어도 정지하며 키를 놓으면 해제한다. 선택 가능한 선단은 원본 선택 처리를 즉시 실행하고, 실패하면 누르는 동안 0.15초 간격으로 다시 확인한다.

- 결과: [Restitutor_BugFixes_CTRL_Instant.dll](../Bug_Fixes/Restitutor_BugFixes_CTRL_Instant.dll)
- [현재 상태](../docs/mods/ctrl-instant/CURRENT.md) · [패치 계약](../docs/mods/ctrl-instant/PATCHES.md) · [0.1.0 근거](../docs/mods/ctrl-instant/0.1.0.md)
- 빌드: 프로젝트 루트에서 `./CTRLInstant/build.ps1`. 게임 DLL은 메타데이터·컴파일 참조로만 사용한다. 테스트는 관리 코드의 토큰 소유권만 실행한다.

DLL 단독 모드이며 Cheats Interface에 의존하지 않는다. 빌드 스크립트 출력 폴더는 이 프로젝트의 Bug_Fixes다. 게임/Steam 실행과 실전 확인은 사용자만 수행한다.
