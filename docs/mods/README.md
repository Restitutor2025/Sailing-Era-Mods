# Restitutor 모드 DLL 버전별 지시서

일반 작업은 [탐색 인덱스](../../analysis/INDEX.md)에서 시작한다. 현재 상태만 필요하면 [intro](intro/CURRENT.md), [textspeed](textspeed/CURRENT.md), [map](map/CURRENT.md)를 읽고 과거 이력은 필요할 때만 읽는다. [호환성 점검](COMPATIBILITY.md).
작성 기준: 2026-09-18. 프로젝트 소스, 보관 DLL, 기존 검토 문서 및 사용자 보고를 근거로 작성했다.

## 범위와 현재 버전
| DLL | 현재 소스/빌드 버전 | 버전별 문서 |
| --- | --- | --- |
| Restitutor_fixes.dll | 0.1.2 | [시작 화면](intro/0.1.2.md) |
| Restitutor_fixes_textspeed.dll | 0.1.3 | [텍스트 속도](textspeed/0.1.3.md) |
| Restitutor_fixes_map.dll | 0.1.5 | [지도](map/0.1.5.md) |

- 시작 화면: [0.1.0](intro/0.1.0.md), [0.1.1 이력 미확인](intro/0.1.1.md), [0.1.2](intro/0.1.2.md).
- 텍스트 속도: [0.1.0 이력 미확인](textspeed/0.1.0.md), [0.1.1 이력 미확인](textspeed/0.1.1.md), [0.1.2 이력 미확인](textspeed/0.1.2.md), [0.1.3](textspeed/0.1.3.md).
- 지도: [0.1.0](map/0.1.0.md), [0.1.1](map/0.1.1.md), [0.1.2](map/0.1.2.md), [0.1.3](map/0.1.3.md), [0.1.4](map/0.1.4.md).

중간 번호의 문서가 있다는 이유만으로 해당 버전의 실제 배포를 확인한 것은 아니다. 자료가 없는 버전은 이력 미확인 문서로 남겼다. 현재 구현을 과거 각 버전의 변경으로 소급하지 않는다. Git 커밋 이력은 없다.

analysis 아래 외부 모드 조사 자료(SpecialOrderEra, sailingera_morestat, R11 등)는 Restitutor 제작 DLL 이력으로 합치지 않는다. 조사용 게임 DLL, 빌드 의존 DLL, 테스트 DLL도 배포 모드에서 제외한다.

## 반드시 지킬 작업 규칙
- 게임과 Steam 실행·조작, 실제 플레이 테스트는 사용자만 담당한다.
- 에이전트는 코드 검토, 메타데이터/네이티브 정적 대조, 빌드, 독립 대역 테스트만 수행한다.
- 테스트 대역 통과를 실제 게임 동작 확인으로 표현하지 않는다.
- 배포 파일은 사용자가 지정한 위치에 전달한다. 바탕화면 덮어쓰기 요청을 게임 Mods 교체로 확대하지 않는다.
- 새 버전마다 별도 문서를 만들고 발생 이슈, 근거, 변경, 검증, 미확인 사항, DLL 해시를 기록한다.
- 버전 번호를 프로젝트와 MelonInfo 양쪽에서 맞춘다. 이전 DLL/소스와 문서를 보존한다.

## 빌드·설치
프로젝트 루트에서 각 csproj를 Release로 빌드한다. net6.0이며 설치된 MelonLoader/Il2CppAssemblies를 참조한다.
```powershell
dotnet build Restitutor_fixes.csproj -c Release --no-restore
dotnet build TextSpeed/Restitutor_fixes_textspeed.csproj -c Release --no-restore
dotnet build Map/Restitutor_fixes_map.csproj -c Release --no-restore
```
복원 자료가 없다면 해당 프로젝트에 dotnet restore를 먼저 수행한다. 게임 경로가 다르면 csproj의 GameDir 참조를 확인한다.
사용자가 게임을 종료한 뒤 필요한 Restitutor DLL을 게임의 Mods에 넣는다. 실행 중인 게임에는 새 DLL이 자동 반영되지 않는다. 제거는 종료 후 해당 DLL을 Mods 밖으로 이동한다. 의존 DLL 전체나 조사 자료를 Mods에 복사하지 않는다. MelonLoader 설치 패키지의 완전한 배포 구성은 이 문서에서 검증하지 않았다.

## 검증과 원문
- [현재 DLL 목록·해시](DLL_INVENTORY.md)
- [기존 공통 조사 기록](../../MODDING_CONTEXT.md)
- [지도 상세 검토](../../Map/REVIEW_0.1.4.md)
이번 작업은 문서화이며 게임 실행·DLL 재빌드·교체 작업이 아니다.
