# rebalance-growth-0.3.0: Rebalance Growth 0.3.0 (0.2.0 included: skill point every 5 levels, max level 200, ability cap 500,
# luck <= 99 incl. land-exploration checks, HP/attack gain physical/250, carry weight <= 30;
# 0.3.0: cumulative growth S/A/B/C/D 100/77.5/55/32.5/10 % per level, experience slider with partial exp,
# level table extension independent of the GameAssembly check)
# + Cheats Skill 1.1.1, Stat Rank 0.2.0, Devil Fruits 0.1.5 (follow Rebalance Growth when it is active).
# Replaces rebalance-growth-0.2.0 (never run). Run with the game closed. Safe to run again.
# Roll back: delete Mods\Rebalance\Restitutor_Rebalance_Growth.dll; put back Cheats\releases\skill-1.0.5-installed,
# StatRank\releases\0.1.6, DevilFruits\releases\0.1.3 DLLs.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
$branch='feat/rebalance-growth'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or @('36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E') -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전(0.2.0)이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
$jobs=@(
 @{src="$p\RebalanceGrowth\releases\0.3.0\Restitutor_Rebalance_Growth.dll"; dst="$g\Mods\Rebalance\Restitutor_Rebalance_Growth.dll"; want='4EAAD0B80199C217B264DDC705E3F3667ACBF3EDB43F40A67ABFF3725E8644BC'},
 @{src="$p\Cheats\releases\skill-1.1.1\Restitutor_Cheats_Skill.dll"; dst="$g\Mods\Cheats\Restitutor_Cheats_Skill.dll"; want='047C0FD8BD33A31B6E6BF7677D9D2F990A1E01E60DDD921400E4DCD7505142B7'},
 @{src="$p\StatRank\releases\0.2.0\Restitutor_Additional_Stat_Rank.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_Stat_Rank.dll"; want='A038B66DB7FADF740CE99175A9106E9257BCCD331EC472C8F2C4E372753F42F5'},
 @{src="$p\DevilFruits\releases\0.1.5\Restitutor_devil_fruits.dll"; dst="$g\Mods\Rebalance\Restitutor_devil_fruits.dll"; want='BA1323CA0C5092D1F960DAD748788403E22040FE3FF8BF60A43941FBDD76D0FA'})
Write-Host '== 1/2 설치 (4개)' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
# Keep the currently installed Cheats Skill (1.0.5) for rollback, once.
$old="$g\Mods\Cheats\Restitutor_Cheats_Skill.dll"; $keep="$p\Cheats\releases\skill-1.0.5-installed"
if((Test-Path $old) -and -not (Test-Path "$keep\Restitutor_Cheats_Skill.dll") -and (Get-FileHash $old).Hash -ne $jobs[1].want){ New-Item -ItemType Directory -Force $keep | Out-Null; Copy-Item $old $keep -Force; "보관: 기존 Cheats Skill -> Cheats\releases\skill-1.0.5-installed" }
if(-not (Test-Path "$g\Mods\Rebalance\manifest.json")){ [IO.File]::WriteAllText("$g\Mods\Rebalance\manifest.json","{}`n") }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
# Explicit file lists with -f: the root .gitignore is an allowlist and currently has another session's
# uncommitted block, so it is not touched here.
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -f -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01XfaT3vMng3kt4JNXdZdABA'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q $branch 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c $branch origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('RebalanceGrowth/Restitutor_Rebalance_Growth.csproj','RebalanceGrowth/README.md','RebalanceGrowth/build.ps1','RebalanceGrowth/src/EntryPoint.cs','RebalanceGrowth/src/Rules.cs','RebalanceGrowth/src/Tables.cs','RebalanceGrowth/src/LevelUp.cs','RebalanceGrowth/src/Display.cs','RebalanceGrowth/src/Luck.cs','RebalanceGrowth/src/Stats.cs','RebalanceGrowth/src/Weight.cs','RebalanceGrowth/src/Growth.cs','RebalanceGrowth/src/Slider.cs','RebalanceGrowth/src/Levels.cs','RebalanceGrowth/tests/Program.cs','RebalanceGrowth/tests/Tests.csproj','RebalanceGrowth/evidence/0.3.0/tests.txt','RebalanceGrowth/evidence/0.3.0/sha256.txt','docs/mods/rebalance-growth/CURRENT.md','docs/mods/rebalance-growth/PATCHES.md','docs/mods/rebalance-growth/0.1.0.md','docs/mods/rebalance-growth/RISKS-0.1.0.md','docs/mods/rebalance-growth/0.2.0.md','docs/mods/rebalance-growth/0.3.0.md')
Commit 'feat(rebalance-growth): growth rebalance (0.3.0)' 'Skill point every 5 levels, max level 200 (RoleLevel rows 100..200, installed independently of the GameAssembly check, missing rows logged), ability cap 500; luck <= 99 incl. land-exploration checks; HP/attack gain physical/250; carry weight <= 30. 0.3.0: cumulative growth per level (S/A/B/C/D 100/77.5/55/32.5/10 %, progress in <Stat>_Exp), experience slider replacing the 1/10-level buttons with partial exp stored on the role.'
Add @('Cheats/Skill/EntryPoint.cs','Cheats/Skill/Rules.cs','Cheats/Skill/Restitutor_Cheats_Skill.csproj','Cheats/Skill/build.cmd','Cheats/skill-tests/Program.cs','Cheats/skill-tests/Tests.csproj','docs/mods/cheat/skill-1.1.0.md','docs/mods/cheat/skill-1.1.1.md','docs/mods/cheat/CURRENT.md')
Commit 'feat(cheats-skill): interval choices 1/2/5 (default 5), yield the extra grant only while Rebalance Growth is active (1.1.1)' ''
Add @('StatRank/src/EntryPoint.cs','StatRank/src/Rules.cs','StatRank/Restitutor_Additional_Stat_Rank.csproj','StatRank/build.ps1','StatRank/tests/Program.cs','StatRank/tests/Tests.csproj','StatRank/archive/0.1.6/EntryPoint.cs','StatRank/archive/0.1.6/Rules.cs','docs/mods/stat-rank/0.2.0.md','docs/mods/stat-rank/CURRENT.md')
Commit 'feat(stat-rank): show per-level % and stored progress while Rebalance Growth is active (0.2.0)' ''
Add @('DevilFruits/src/EntryPoint.cs','DevilFruits/src/Growth.cs','DevilFruits/Restitutor_devil_fruits.csproj','DevilFruits/build.ps1','DevilFruits/archive/0.1.3/Dialog.cs','DevilFruits/archive/0.1.3/EntryPoint.cs','DevilFruits/archive/0.1.3/Growth.cs','DevilFruits/archive/0.1.3/Rules.cs','DevilFruits/archive/0.1.3/Sheet.cs','DevilFruits/archive/0.1.3/Templates.cs','docs/mods/devil-fruits/0.1.5.md','docs/mods/devil-fruits/CURRENT.md')
Commit 'fix(devil-fruits): resync grades before the n-level path too (0.1.5)' ''
Add @('rebalance-growth-0.3.0.ps1','rebalance-growth-0.3.0.bat'); Commit 'chore: Rebalance Growth 0.3.0 install script' ''
git push -q -u origin $branch; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr="https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...${branch}?expand=1"
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
