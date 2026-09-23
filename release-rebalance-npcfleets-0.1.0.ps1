# release-rebalance-npcfleets-0.1.0: New mod Rebalance NpcFleets 0.1.0: large Fu ship (300) removed from the NPC purchase list; 7 NPC fleets re-shipped (pirates: 간증선/해창선/개랑선, trade fleets: 복선).
# Run with the game closed. Safe to run again. Roll back: delete Mods\Rebalance\Restitutor_Rebalance_NpcFleets.dll
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or @('36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E') -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
$jobs=@(
 @{src="$p\RebalanceNpcFleets\releases\0.1.0\Restitutor_Rebalance_NpcFleets.dll"; dst="$g\Mods\Rebalance\Restitutor_Rebalance_NpcFleets.dll"; want='C7753325475A267D4EA960F05BCD8DDBA4417DFF08336FF62B79191B7EC31A3F'})
Write-Host '== 1/2 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -f -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01GA2ihscBe7DquX5AQAzSpW'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'feat/rebalance-npcfleets' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'feat/rebalance-npcfleets' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('RebalanceNpcFleets','docs/mods/rebalance-npcfleets'); Commit 'feat(rebalance-npcfleets): no large Fu ship for NPCs (0.1.0)' 'New mod Restitutor_Rebalance_NpcFleets 0.1.0 (Mods/Rebalance). Two postfixes on table load: TemplateManager.InitGameConst drops 300 from NPC_BUY_SHIP_LIST, TemplateManager.InitNpcShipTeam re-ships the 7 fleets that used a large Fu ship (pirates: 3293 flagship, 3291/3294 escorts; trade fleets: 3290), only when a row still equals the original. New saves; fleets already stored in a save keep their ships.'
Add @('tools/release-rebalance-npcfleets-0.1.0.json','release-rebalance-npcfleets-0.1.0.ps1','release-rebalance-npcfleets-0.1.0.bat'); Commit 'chore: release script for Rebalance NpcFleets 0.1.0' ''
git push -q -u origin 'feat/rebalance-npcfleets'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...feat/rebalance-npcfleets?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
