# core-batch-2: install Text Speed 0.1.6, CTRL Instant 0.1.7, Item Rebuild 0.1.23 (all on Restitutor.Core, behaviour unchanged), then commit and push.
# Run with the game closed. Safe to run again. Roll back: TextSpeed\releases\0.1.5, CTRLInstant\releases\0.1.6, ItemRebuild\releases\0.1.22 over the installed files.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
if((Get-FileHash "$g\UserLibs\Restitutor.Core.dll").Hash -ne '928E5F54948C12F769085780447F7E4647B14C86BA076EA0ABEEA7AC55E28027'){ Fail 'UserLibs\Restitutor.Core.dll 0.1.0 이 없거나 다릅니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
git merge-base --is-ancestor 'refactor/core-batch-1' origin/main; if($LASTEXITCODE -ne 0){ Fail '이전 PR(refactor/core-batch-1)이 아직 병합되지 않았습니다. GitHub 에서 먼저 Merge 후 다시 실행' }
$jobs=@(
 @{src="$p\TextSpeed\releases\0.1.6\Restitutor_fixes_textspeed.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_Textspeed.dll"; want='3AEF807822EAFF01C1799AE65CD44B71B95A7339C757D263A0A1AA530F7010B6'},
 @{src="$p\CTRLInstant\releases\0.1.7\Restitutor_BugFixes_CTRL_Instant.dll"; dst="$g\Mods\Bug_Fixes\Restitutor_BugFixes_CTRL_Instant.dll"; want='66D038C7590D8E364B8D89E0C267E5A927D6B992A050267CF943365EF6023D31'},
 @{src="$p\ItemRebuild\releases\0.1.23\Restitutor_Additional_Item_Rebuild.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_Item_Rebuild.dll"; want='7766E3DAF8E859558BA717AE8AE292571CA13A33D8C6E505F461E198F0787BCD'})
Write-Host '== 1/2 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -A -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'refactor/core-batch-2' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'refactor/core-batch-2' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('TextSpeed','docs/mods/textspeed'); Commit 'refactor(textspeed): register hooks through Restitutor.Core (0.1.6)' 'Same seven hooks and order on the same Harmony id; two HookSets (OptionsRow, TextSpeedModule). Requires Restitutor.Core 0.1.0.'
Add @('CTRLInstant','docs/mods/ctrl-instant'); Commit 'refactor(ctrl-instant): register hooks through Restitutor.Core (0.1.7)' 'Same seven hooks, handlers and order. Requires Restitutor.Core 0.1.0.'
Add @('ItemRebuild','docs/mods/item-rebuild'); Commit 'refactor(item-rebuild): register hooks through Restitutor.Core (0.1.23)' 'Same 48 install-time hooks and the lazy UIManager.ShowInputNumPromptBox hook; lookup rules, handlers and order unchanged. Tests compile Core; 5676 checks pass. Requires Restitutor.Core 0.1.0.'
Add @('docs/mods/devil-fruits','.gitignore','tools','core-batch-2.ps1','core-batch-2.bat'); Commit 'chore: Core batch 2 script, oneshot generator; note withdrawn Devil Fruits 0.1.4' ''
git push -q -u origin 'refactor/core-batch-2'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...refactor/core-batch-2?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
