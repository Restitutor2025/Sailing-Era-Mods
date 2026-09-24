# perf-map-0.2.6: Map 0.2.6 - no UpdateInfo hook; red ports = native CheckPortSelectStatus (survey/no path/no open line), re-applied from the UIMapView.Refresh postfix, url mode default.
# Run with the game closed. Safe to run again. Roll back: Map\candidates\tab-only\dist\0.2.4\Restitutor_fixes_map.dll.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
if((Get-FileHash "$g\UserLibs\Restitutor.Core.dll").Hash -ne '36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E'){ Fail 'Restitutor.Core 0.2.0 이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다' }
$src="$p\Map\candidates\tab-only\dist\0.2.6\Restitutor_fixes_map.dll"; $dst="$g\Mods\Bug_Fixes\Restitutor_fixes_map.dll"; $want='9979A2AA1FC5CA3326934C3B6A15C53C2A9773612F1E99C0DD764BD1DFA45E53'
Write-Host '== 1/2 설치' -ForegroundColor Cyan
if((Get-FileHash $src).Hash -ne $want){ Fail "원본 해시 불일치: $src" }
Copy-Item $src $dst -Force; if((Get-FileHash $dst).Hash -ne $want){ Fail "교체 후 불일치: $dst" }; "설치: Restitutor_fixes_map.dll 0.2.6"
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
function AddF([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -f -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01C8pv8RCmVygqf6nAniF6Sx'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'perf/map-no-updateinfo-hook' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'perf/map-no-updateinfo-hook' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch (다른 브랜치의 미커밋 변경과 충돌했을 수 있음)' } }
AddF @('Map/candidates/tab-only/ImplementationCheck.csproj','Map/candidates/tab-only/src/EntryPoint.cs','Map/candidates/tab-only/src/RoutePortVisuals.cs','Map/candidates/tab-only/src/SharedMap.cs','Map/candidates/tab-only/tests/SharedChecks.cs','Map/candidates/tab-only/tests/SharedSupport.cs','docs/mods/map/0.2.5.md','docs/mods/map/0.2.6.md','Map/candidates/tab-only/tests/SharedRouteStubs.cs')
Commit 'perf(map): drop the UpdateInfo hook; red ports from the native click check (Map 0.2.6)' 'UIMapHarbourIcon.UpdateInfo runs for every map icon on each refresh, including the sailing minimap (900-1,700 calls/s at sea). Route-session red ports and flags are now re-applied from the existing UIMapView.Refresh postfix, only in a session and only for icons in view. Unreachable = UISailLineCtrl.CheckPortSelectStatus 3 (survey level) or 4 (no path from the last waypoint) or NoLineReach false, computed on route change. Re-applied to red and selected ports; url mode by default (ctrlSelfPort alone is not red in game). RedPort and _eMapUseType are no longer touched.'
AddF @('perf-map-0.2.6.ps1','perf-map-0.2.6.bat'); Commit 'chore: Map 0.2.6 install script' ''
git push -q -u origin 'perf/map-no-updateinfo-hook'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...perf/map-no-updateinfo-hook?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
