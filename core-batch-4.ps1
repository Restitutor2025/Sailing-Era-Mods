# core-batch-4: Cheats Interface 1.6.0 + Skill 1.0.5 on Restitutor.Core 0.2.0 (shared input gate, behaviour unchanged), then commit and push.
# Run with the game closed. Safe to run again. Roll back: Cheats\releases\1.5.1 Interface and Skill DLLs over the installed files.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or @('36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E') -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
git merge-base --is-ancestor 'feat/core-input-gate' origin/main; if($LASTEXITCODE -ne 0){ Fail '이전 PR(feat/core-input-gate)이 아직 병합되지 않았습니다. GitHub 에서 먼저 Merge 후 다시 실행' }
$jobs=@(
 @{src="$p\Cheats\releases\1.6.0\Restitutor_Cheats_Interface.dll"; dst="$g\Mods\Cheats\Restitutor_Cheats_Interface.dll"; want='E02EB667FEFB9D79B766C594A08CE3FAD2590753BEF68224127E035FBE05A2E2'},
 @{src="$p\Cheats\releases\1.6.0\Restitutor_Cheats_Skill.dll"; dst="$g\Mods\Cheats\Restitutor_Cheats_Skill.dll"; want='20BBB0C6EC2AD2454954B5417667B9F70DF4BDC023647929409B85C066716CD2'})
Write-Host '== 1/2 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -A -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'refactor/cheats-core' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'refactor/cheats-core' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('Cheats','docs/mods/cheat'); Commit 'refactor(cheats): Interface 1.6.0 and Skill 1.0.5 on Restitutor.Core' 'Host.Hook keeps its signature and lookup rules but registers through Core HookSet, so Speed/Battle/Contribution need no rebuild. Interface input capture moves to the shared Core input gate; Skill hooks through HookSet. Requires Restitutor.Core 0.2.0. Optional package, distributed separately.'
Add @('tools','core-batch-4.ps1','core-batch-4.bat'); Commit 'chore: Core batch 4 (cheats) script' ''
git push -q -u origin 'refactor/cheats-core'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...refactor/cheats-core?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
