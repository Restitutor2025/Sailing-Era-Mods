# release-rebalance-landexplore-0.1.0: New mod Rebalance LandExplore 0.1.0: fate coin chance capped at 50% and the shown number made the real chance, then commit and push.
# Run with the game closed. Safe to run again. Roll back: delete Mods\Rebalance\Restitutor_Rebalance_LandExplore.dll (nothing is stored in the save)
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or @('36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E') -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
$jobs=@(
 @{src="$p\RebalanceLandExplore\releases\0.1.0\Restitutor_Rebalance_LandExplore.dll"; dst="$g\Mods\Rebalance\Restitutor_Rebalance_LandExplore.dll"; want='E9A41BB986B8AD06C5041987D2D3CFED643493F23E13B7BF3F511C5C19B6F03D'})
Write-Host '== 1/2 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -A -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01CkXRH7KhtbmGaLTaTrgcpH'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'feat/rebalance-landexplore' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'feat/rebalance-landexplore' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('.gitignore','RebalanceLandExplore/Restitutor_Rebalance_LandExplore.csproj','RebalanceLandExplore/README.md','RebalanceLandExplore/build.ps1','RebalanceLandExplore/src/EntryPoint.cs','RebalanceLandExplore/src/Rules.cs','RebalanceLandExplore/tests/Program.cs','RebalanceLandExplore/tests/Tests.csproj','docs/mods/rebalance-landexplore/0.1.0.md','docs/mods/rebalance-landexplore/CURRENT.md'); Commit 'feat(rebalance-landexplore): cap the fate coin at 50% and make the shown chance the real one' 'New mod Restitutor_Rebalance_LandExplore 0.1.0 (Mods/Rebalance). Land-explore property checks: the fate coin re-roll chance (CalculateProbability inlined in UILandExploreEventView.ListRoleRender, kept as the maximum over the listed navigators in UILandExploreEventModel.Ratio) is hard-clamped to 50 by a ListRoleRender postfix, and a UILandExploreEventCtrl.ShowRatioResult prefix lowers the roll threshold by one so that Random.Next(0,100) <= t matches the number on the button (the original was one percentage point more generous; shown 0% was really 1%). The deterministic check, the navigator the coin uses and the coin cost are unchanged. Requires Restitutor.Core 0.1.0+.'
Add @('tools/release-rebalance-landexplore-0.1.0.json','release-rebalance-landexplore-0.1.0.ps1','release-rebalance-landexplore-0.1.0.bat'); Commit 'chore: release script for Rebalance LandExplore 0.1.0' ''
git push -q -u origin 'feat/rebalance-landexplore'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...feat/rebalance-landexplore?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
