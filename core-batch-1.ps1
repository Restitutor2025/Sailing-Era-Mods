# core-batch-1: install Stat Rank 0.1.6, Devil Fruits 0.1.3, Intro Skip 0.1.3 (all on Restitutor.Core, behaviour unchanged),
# then commit them on refactor/core-batch-1 and push. Run with the game closed. Safe to run again.
# Roll back: StatRank\releases\0.1.5, DevilFruits\releases\0.1.2, releases\intro\0.1.2 over the installed files.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
if((Get-FileHash "$g\UserLibs\Restitutor.Core.dll").Hash -ne '928E5F54948C12F769085780447F7E4647B14C86BA076EA0ABEEA7AC55E28027'){ Fail 'UserLibs\Restitutor.Core.dll 0.1.0 이 없거나 다릅니다' }
$jobs=@(
 @{src="$p\StatRank\releases\0.1.6\Restitutor_Additional_Stat_Rank.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_Stat_Rank.dll"; want='1F418BC954030712ED670CB81B8F15E15135C0CFEF5E323BB4EA000F833A830E'},
 @{src="$p\DevilFruits\releases\0.1.3\Restitutor_devil_fruits.dll"; dst="$g\Mods\Rebalance\Restitutor_devil_fruits.dll"; want='826CD7F93A22B2E4E83EA78C66EDA2128191D875A671AF6EC455479CD2B1F22C'},
 @{src="$p\releases\intro\0.1.3\Restitutor_fixes.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_IntroSkip.dll"; want='7C4D7F5C6470236A808BFA83063E3F7818166AECA87F163EE5CF124CFB7F5CB6'})
Write-Host '== 1/2 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -A -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
git switch -q 'refactor/core-batch-1' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'refactor/core-batch-1' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('StatRank','docs/mods/stat-rank'); Commit 'refactor(stat-rank): register hooks through Restitutor.Core (0.1.6)' 'Same two postfixes, same GameAssembly baseline check and order; handler and tooltip code unchanged. Requires Restitutor.Core 0.1.0.'
Add @('DevilFruits','docs/mods/devil-fruits'); Commit 'refactor(devil-fruits): register hooks through Restitutor.Core (0.1.3)' 'Same 11 hooks and order; lookup by name only (every name is unique on its type in the interop metadata, same parameter counts as before). Requires Restitutor.Core 0.1.0.'
Add @('Restitutor_fixes.csproj','src','docs/mods/intro'); Commit 'refactor(intro): register hooks through Restitutor.Core (0.1.3)' 'Same four patches (declaredOnly: false keeps the inherited lookup); failure still leaves the original launch UI. Requires Restitutor.Core 0.1.0.'
Add @('.gitignore','core-batch-1.ps1','core-batch-1.bat'); Commit 'chore: add Core batch 1 install-and-push script' ''
git push -q -u origin 'refactor/core-batch-1'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...refactor/core-batch-1?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
