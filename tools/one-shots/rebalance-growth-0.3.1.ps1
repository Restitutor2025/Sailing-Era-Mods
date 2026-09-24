# rebalance-growth-0.3.1: Rebalance Growth 0.3.1 + Stat Rank 0.2.1 (0.3.0 in-game test fixes).
# Q/E key icons, value/preview layout, skill point preview, Space = slider amount, cumulative % kept on repeated
# presses, HP/attack "+N" display /250; Stat Rank tip footer for devil fruits.
# Run with the game closed. Safe to run again.
# Roll back: RebalanceGrowth\releases\0.3.0 and StatRank\releases\0.2.0 DLLs back into Mods.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
$branch='fix/rebalance-growth-0.3.1'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or @('36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E') -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전(0.2.0)이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
$jobs=@(
 @{src="$p\RebalanceGrowth\releases\0.3.1\Restitutor_Rebalance_Growth.dll"; dst="$g\Mods\Rebalance\Restitutor_Rebalance_Growth.dll"; want='0525549A19D21FF0C799E3290123EFF22B75D7EADBDB6315345E34636FA0C4E4'},
 @{src="$p\StatRank\releases\0.2.1\Restitutor_Additional_Stat_Rank.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_Stat_Rank.dll"; want='EA65C15777DE24FF3F1AEDA5504612D48D2B21CD7BC8E323CF6C1DC4EA86687F'})
Write-Host '== 1/2 설치 (2개)' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
if(-not (Test-Path "$g\Mods\Rebalance\manifest.json")){ [IO.File]::WriteAllText("$g\Mods\Rebalance\manifest.json","{}`n") }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
# Explicit file lists with -f: the root .gitignore is an allowlist and currently has another session's
# uncommitted block, so it is not touched here.
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -f -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01XfaT3vMng3kt4JNXdZdABA'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q $branch 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c $branch origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('RebalanceGrowth/Restitutor_Rebalance_Growth.csproj','RebalanceGrowth/build.ps1','RebalanceGrowth/src/EntryPoint.cs','RebalanceGrowth/src/Rules.cs','RebalanceGrowth/src/Growth.cs','RebalanceGrowth/src/Slider.cs','RebalanceGrowth/src/Stats.cs','RebalanceGrowth/tests/Program.cs','RebalanceGrowth/evidence/0.3.1/tests.txt','RebalanceGrowth/evidence/0.3.1/sha256.txt','docs/mods/rebalance-growth/CURRENT.md','docs/mods/rebalance-growth/PATCHES.md','docs/mods/rebalance-growth/0.3.1.md')
Commit 'fix(rebalance-growth): 0.3.0 test fixes (0.3.1)' 'Q/E key icons through the game loader, value and preview packed beside the ability name, skill point preview, Space/A confirm the slider amount (nothing when nothing is chosen), cumulative progress no longer lost on presses during a level-up, HP/attack +N display uses physical/250.'
Add @('StatRank/src/EntryPoint.cs','StatRank/Restitutor_Additional_Stat_Rank.csproj','StatRank/build.ps1','StatRank/archive/0.2.0/EntryPoint.cs','StatRank/archive/0.2.0/Rules.cs','docs/mods/stat-rank/0.2.1.md','docs/mods/stat-rank/CURRENT.md')
Commit 'feat(stat-rank): devil fruit hint in the tip (0.2.1)' ''
Add @('rebalance-growth-0.3.1.ps1','rebalance-growth-0.3.1.bat'); Commit 'chore: Rebalance Growth 0.3.1 install script' ''
git push -q -u origin $branch; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr="https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...${branch}?expand=1"
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
