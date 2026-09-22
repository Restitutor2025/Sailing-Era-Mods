# release-rebalance-saveslots-0.1.1: Rebalance SaveSlots 0.1.1: rows 11+ were blank (enter animation stopped hidden); show them at once, keep selection, count text N/101.
# Run with the game closed. Safe to run again. Roll back: copy RebalanceSaveSlots\releases\0.1.0\Restitutor_Rebalance_SaveSlots.dll over Mods\Rebalance\Restitutor_Rebalance_SaveSlots.dll, or delete it
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or @('36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E') -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
$jobs=@(
 @{src="$p\RebalanceSaveSlots\releases\0.1.1\Restitutor_Rebalance_SaveSlots.dll"; dst="$g\Mods\Rebalance\Restitutor_Rebalance_SaveSlots.dll"; want='D0ED7837EFBC46877C78C2BB1C7C061EF42AC42047865FA80A9513B7C3A9C525'})
Write-Host '== 1/2 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -f -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01GA2ihscBe7DquX5AQAzSpW'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'feat/rebalance-saveslots' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'feat/rebalance-saveslots' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('RebalanceSaveSlots/Restitutor_Rebalance_SaveSlots.csproj','RebalanceSaveSlots/README.md','RebalanceSaveSlots/src/EntryPoint.cs','RebalanceSaveSlots/src/Rules.cs','RebalanceSaveSlots/tests/Program.cs','docs/mods/rebalance-saveslots/0.1.1.md'); Commit 'fix(rebalance-saveslots): show rows 11+ in the save list (0.1.1)' 'Refresh shrinks the list to 10 before the mod grows it back, so rows 10+ leave the stage while their delayed enter transition (index*0.1 s) waits and FairyGUI stops it in the hidden state. New RenderStorageItem postfix runs aniReset/aniruchang to their end for rows 10+. Refresh postfix re-applies selection 10+ and writes the count text as N/101.'
Add @('tools/release-rebalance-saveslots-0.1.1.json','release-rebalance-saveslots-0.1.1.ps1','release-rebalance-saveslots-0.1.1.bat'); Commit 'chore: release script for Rebalance SaveSlots 0.1.1' ''
git push -q -u origin 'feat/rebalance-saveslots'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...feat/rebalance-saveslots?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
