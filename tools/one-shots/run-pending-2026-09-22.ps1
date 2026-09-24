# run-pending-2026-09-22: fix-contribution-0.5.7 -> (사용자 PR Merge) -> release-v1.0.0 을 차례로 실행.
# 각 스크립트는 다시 실행해도 안전. 게임을 끈 상태에서 실행.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; Set-Location $p
function Run($name){ Write-Host "`n===== $name =====" -ForegroundColor Cyan; powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $p "$name.ps1") | Out-Host; return $LASTEXITCODE }

if((Run 'fix-contribution-0.5.7') -ne 0){ Write-Host "`n1단계 실패 - 위 FAILED 메시지를 확인하세요. 2단계는 실행하지 않았습니다." -ForegroundColor Red; exit 1 }

while($true){
  Write-Host "`n방금 열린 PR 페이지에서 Create pull request -> Merge 를 누른 뒤 Enter (그만두려면 q + Enter)" -ForegroundColor Yellow
  if((Read-Host) -eq 'q'){ Write-Host '중단. 나중에 release-v1.0.0.bat 만 따로 실행하면 됩니다.'; exit 1 }
  if((Run 'release-v1.0.0') -eq 0){ break }
  Write-Host "`n2단계 실패 - 병합 전이었다면 Merge 후 Enter 로 다시 시도. gh 미설치/미로그인이면 안내대로 처리 후 Enter." -ForegroundColor Red
}
Write-Host "`n완료. 열린 PR(chore/release-v1.0.0)도 Create -> Merge 하세요. 그 다음 게임에서 도시 혜택 HUD 확인." -ForegroundColor Green
