# Interface 1.5.0 / Skill 1.0.3

O로 치트 전체 끄기/켜기(게임 종료 전까지 유지), H는 접기/펼치기. Skill은 꺼진 동안 원본 간격. 두 DLL 함께 설치. [근거·배포](../docs/mods/cheat/1.5.0.md).

# Interface 1.3.0

H로 기본 접힘/펼침 토글. 사용자 선택은 장소/세션에 걸쳐 유지. [근거 및 배포](../docs/mods/cheat/1.3.0.md).

# Cheats Interface 1.2.2 — 기본 펼침 복원

기본/세션 초기화 시 펼침, 장소 전환 자동 접기 제거. 수동 접기·프로세스 닫힘 및 기존 입력/4개 훅 계약 유지. 사용자 로그와 설치 목록에서 Battle DLL 누락 확인: 배포 ZIP에 Battle 1.0.0 동봉. Battle 로직/배율 유지 정책은 변경 없음. 빌드 및 관리 검사 129개 통과, 설치·실전 검증 전. [근거·배포](../docs/mods/cheat/1.2.2.md). 아래는 과거 기록.

# Battle 1.0.0

전투 전용 `Restitutor_Cheats_Battle.dll`: 백병전/포격전 X1~X5. 해상 전투 전체 종료 시 둘 다 X1, 백병전/일기토 왕복에서는 선택 유지. Interface 1.2.0 이상 필요. [계약·범위·검증](../docs/mods/cheat/battle-1.0.0.md). Battle/ 프로젝트, battle-tests/ 관리 검사, releases/battle-1.0.0 배포.

# Cheats 1.2.1

공헌도 도시 밖 숨김 및 도시/항해 진입 자동 최소화. 기본 제목줄만 표시하고 화살표로 펼친다. 배포 ZIP은 releases/1.2.1의 DLL 4개. [근거·검증](../docs/mods/cheat/1.2.1.md).

# Cheats 1.2.0

장소별 표시를 위해 Panel.Visible을 추가했다(기본 true). 숨긴 패널도 Refresh는 계속되며 Host가 공간과 포커스를 정리한다. Interface/Speed/Bargirls 배포본은 releases/1.2.0. [상세](../docs/mods/cheat/1.2.0.md).

# Cheats 1.1.0

공통 창은 기능 이름을 알지 않는 집합 UI다. 새 기능은 Panel 파생형에 Id/Order/Height/Build/Refresh/Reset을 구현하고 Register/Unregister한다. 공통 스타일은 protected Surface/Foreground/Accent/Border/Text/Rect/ButtonFace로 사용한다. 긴 패널 목록은 자동 스크롤된다. 제목줄 드래그/전체 최소화/프로세스 수명 닫힘을 Host가 관리한다. [1.1.0](../docs/mods/cheat/1.1.0.md).

세 개의 독립 Melon 모듈로 구성한다. 설치 위치는 `Mods/Cheats`이며 Interface는 필수다. Speed와 Contribution은 각각 Interface만 참조하고 서로 참조하지 않는다. 기능 하나를 제거하면 해당 패널도 등록되지 않는다.

| DLL | 소스 | 책임 |
|---|---|---|
| Restitutor_Cheats_Interface.dll | Interface/ | 공통 창, 패널 등록·배치, 입력 차단, 플레이어/저장 수명주기 |
| Restitutor_Cheats_Speed.dll | Speed/ | X1~X5 패널, 항해 판정, 이동 배율 적용·복원 |
| Restitutor_Cheats_Contribution.dll | Contribution/ | 목표 공헌도 입력/적용 패널, 도시 판정, 보정 조회 범위 |

공통 Panel API의 Build/Refresh/Reset/ClearReferences를 각 기능에서 구현한다. 패널 뷰 재생성과 게임 세션 초기화를 구분한다. 항해 지도에서 배율 선택 유지, 도시 비활성, 항해 종료 X1 정책은 0.3.2와 같다. 시간/물자 소모 배율은 변경하지 않는다.

빌드: `dotnet build Cheats/Speed/Restitutor_Cheats_Speed.csproj -c Release -p:NuGetAudit=false` 및 Contribution 프로젝트. Interface는 프로젝트 참조로 함께 빌드된다. Directory.Build.props의 GameDir를 사용한다. 기능 빌드 출력에 복사된 Interface를 중복 설치하지 않는다.

검사: `dotnet run --project Cheats/tests/Tests.csproj -c Release`, `dotnet run --project Cheats/lifecycle-tests/Tests.csproj -c Release`, `python Cheats/evidence/check.py`, `pwsh -NoProfile -File Cheats/evidence/audit.ps1`.

[현재 문서](../docs/mods/cheat/CURRENT.md) · [1.0.0 근거](../docs/mods/cheat/1.0.0.md). 기존 단일 모듈 소스/프로젝트는 Cheat/archive/0.3.2-source, 과거 DLL은 Cheat/releases에 보존한다. 과거 DLL은 설치하지 않는다.

## Money 1.0.0
네 번째 선택 기능 Restitutor_Cheats_Money.dll (Money/) 추가. 공헌도 바로 아래 소지금 입력/적용. Interface 1.1.1 필요. [범위·근거·검증](../docs/mods/cheat/money-1.0.0.md). 빌드 프로젝트 Cheats/Money/Restitutor_Cheats_Money.csproj, 검사 프로젝트 Cheats/money-tests/Tests.csproj. 이 환경에서는 APPDATA=Contribution/build-profile, NUGET_PACKAGES=.packages의 절대 경로를 해당 빌드 프로세스에 지정해 기존 로컬 패키지를 사용한다.

## Bargirls 1.0.0
Restitutor_Cheats_Bargirls.dll: 술집 여급 애정 1·2·3 버튼, 하위 단계 차단 및 비활성 취소선. Interface 1.1.1 필요. [전체 계약](../docs/mods/cheat/bargirls-1.0.0.md). 빌드 Cheats/Bargirls/Restitutor_Cheats_Bargirls.csproj, 검사 Cheats/bargirls-tests/Tests.csproj.

## Bargirls 1.1.0
현재판: 여급 호감도 상승 버튼 하나, 3단계 및 현재 여급 과제 진행/보고 대기 시 차단. 과제 기록을 수정하지 않으며 원본 상한을 준수한다. [계약·검증](../docs/mods/cheat/bargirls-1.1.0.md). 1.0.0 기록은 위에 보존.

## Exp 1.0.0
Restitutor_Cheats_Exp.dll (Exp/): 술집 화면 전용 경험치 목표값 입력/적용과 Max(2,147,483,647). Interface 1.3.0 필요. [계약·근거](../docs/mods/cheat/exp-1.0.0.md). 빌드·검사·설치: 게임 종료 후 `Cheats/Exp/build.cmd`(빌드 → exp-tests → releases/exp-1.0.0 → Mods\Cheats 복사·해시). 검사 프로젝트 Cheats/exp-tests/Tests.csproj.

## Character 1.0.0
Restitutor_Cheats_Character.dll (Character/): Tab 인물창 전용 인물 수정(체력 현재·최대, 공격력, 능력치 6종, 보유 스킬 레벨 0~최대). Interface 1.3.0 필요. [계약·근거](../docs/mods/cheat/character-1.0.0.md). 빌드·검사·설치: 게임 종료 후 `Cheats/Character/build.cmd`(빌드 → character-tests → releases/character-1.0.0 → Mods\Cheats 복사·해시). 게임 실행 중에는 `Cheats/Character/compile.cmd`(빌드·검사만). 검사 프로젝트 Cheats/character-tests/Tests.csproj.
