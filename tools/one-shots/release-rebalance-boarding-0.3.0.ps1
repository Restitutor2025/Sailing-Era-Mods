# release-rebalance-boarding-0.3.0: Rebalance Boarding 0.3.0: threshold 100 -> 40 (native byte) + 3 s re-boarding grace + boarding progress gauge under the sailor gauge on every player ship, then commit and push.
# Run with the game closed. Safe to run again. Roll back: delete Mods\Rebalance\Restitutor_Rebalance_Boarding.dll
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or @('36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E') -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
$jobs=@(
 @{src="$p\RebalanceBoarding\releases\0.3.0\Restitutor_Rebalance_Boarding.dll"; dst="$g\Mods\Rebalance\Restitutor_Rebalance_Boarding.dll"; want='93158BB9527C7AD47879DD09C9C41A71AD8A5792342A8F7172F0B1BA5CDF2895'})
Write-Host '== 1/2 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -f -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_013VqsxPtcJ7toN3hrfQQ2q3'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'feat/rebalance-boarding' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'feat/rebalance-boarding' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('RebalanceBoarding/Restitutor_Rebalance_Boarding.csproj','RebalanceBoarding/README.md','RebalanceBoarding/build.ps1','RebalanceBoarding/src/EntryPoint.cs','RebalanceBoarding/src/NativeThreshold.cs','RebalanceBoarding/src/Patch.cs','RebalanceBoarding/src/Grace.cs','RebalanceBoarding/src/Gauge.cs','RebalanceBoarding/src/ProgressBarUI.cs','RebalanceBoarding/tests/Program.cs','RebalanceBoarding/tests/Tests.csproj','docs/mods/rebalance-boarding/CURRENT.md','docs/mods/rebalance-boarding/PATCHES.md','docs/mods/rebalance-boarding/0.1.0.md','docs/mods/rebalance-boarding/0.2.0.md','docs/mods/rebalance-boarding/0.3.0.md'); Commit 'feat(rebalance-boarding): faster boarding melee plus a boarding progress gauge' 'New mod Restitutor_Rebalance_Boarding 0.3.0 (Mods/Rebalance). (1) Threshold: imm8 of ''cmp ecx, 0x64'' in BoatEntityBoardShoot.CheckProgressFull (RVA 0x25CD8D1) becomes 0x28 in process memory, only when the GameAssembly SHA256 matches and the running body equals the file bytes; restored on exit. (2) Grace: the original zeroes ShootProgress the moment the last boarding target leaves; a void prefix on OnEnemyExitShooting records the value and a postfix on BeginShooting gives it back when the same ship is boarded again within 3 s. (3) Gauge: a second Common/proBattleSeaman bar, amber, added as a child of the ship''s own UICompBattleHp just below the sailor bar, on every PLAYER ship (BoatEntityData.IsPlayer == BoatTeam.isPlayer), escorts included: escorts enter melee too, it is only resolved automatically (CheckProgressFull routes to the played melee when a player FLAGSHIP is involved and to AIMeleeBattleSimulateHandler otherwise). Value is this ship''s ShootProgress plus its target''s over the threshold the running game compares against (40 patched, 100 not). Shown and hidden with a 0.5 s fade; hidden at zero progress and once melee starts. Updated only on boarding events (2 s accumulation tick, contact start/stop, melee entry); position comes free from the game''s existing UpdateHPBar, so no per-frame work is added. Event-driven only, saves untouched. Grace and gauge need Restitutor.Core 0.1.0+.'
Add @('tools/release-rebalance-boarding-0.3.0.json','release-rebalance-boarding-0.3.0.ps1','release-rebalance-boarding-0.3.0.bat'); Commit 'chore: release script for Rebalance Boarding 0.3.0' ''
git push -q -u origin 'feat/rebalance-boarding'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...feat/rebalance-boarding?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
