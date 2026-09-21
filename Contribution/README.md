# Restitutor fixes Contribution 0.3.1

공헌도 상승 혜택과 도시 최초 입장/최초 100 도달/신규 자원 등록의 해금 상품 인식을 처리한다. 과거 자원 보충 없음. [현재 동작](../docs/mods/contribution/CURRENT.md), [검증 및 한계](../docs/mods/contribution/0.3.1.md).

현재 설치 위치: `E:\Program\steam\steamapps\common\Sailing Era\Mods\Additional_Functions\Restitutor_fixes_Contribution.dll` (2026-09-19 사용자 폴더 이동 후 확인).

빌드: dotnet build Contribution/Restitutor_fixes_Contribution.csproj -c Release --no-restore

검증: dotnet run --project Contribution/tests/RulesTests.csproj -c Release --no-restore

IL 검사: powershell -ExecutionPolicy Bypass -File Contribution/evidence/audit-il.ps1

부가 저장 UserData/Restitutor/Contribution은 게임 저장과 함께 보관한다. 게임/Steam은 실행하지 않는다. 과거 회수 테스트와 취소한 중간 수정은 archive에 보존했으며 현재 빌드에 포함하지 않는다.

