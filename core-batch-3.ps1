# core-batch-3: Restitutor.Core 0.2.0 (shared input gate) + Contribution 0.5.6, Tab Characters 0.6.12, Map 0.2.4 on the gate (behaviour unchanged), then commit and push.
# Run with the game closed. Safe to run again. Roll back: Core\releases\0.1.0 to UserLibs, Contribution\releases\0.5.5, TabCharacters\releases\0.6.11, Map\candidates\tab-only\dist\0.2.3 over the installed files (roll back all four together).
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or @('928E5F54948C12F769085780447F7E4647B14C86BA076EA0ABEEA7AC55E28027','36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E') -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
git merge-base --is-ancestor 'refactor/core-batch-2' origin/main; if($LASTEXITCODE -ne 0){ Fail '이전 PR(refactor/core-batch-2)이 아직 병합되지 않았습니다. GitHub 에서 먼저 Merge 후 다시 실행' }
$jobs=@(
 @{src="$p\Core\releases\0.2.0\Restitutor.Core.dll"; dst="$g\UserLibs\Restitutor.Core.dll"; want='36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E'},
 @{src="$p\Contribution\releases\0.5.6\Restitutor_fixes_Contribution.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_Contribution_Goods_Recognition.dll"; want='0412DEEC4F2A8B18AC160B6962D56B3510ACC3B20416300062692F0602AAA45C'},
 @{src="$p\TabCharacters\releases\0.6.12\Restitutor_Additional_Tab_Characters.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_Tab_Characters.dll"; want='0F9C4A6DD38EA19CA495B1FE9676B8FDC9A19F13374403994F2140DD7B1CF411'},
 @{src="$p\Map\candidates\tab-only\dist\0.2.4\Restitutor_fixes_map.dll"; dst="$g\Mods\Bug_Fixes\Restitutor_fixes_map.dll"; want='C94E91615C300271CFFE10BC3D06081200EDFB32300B42465FB816DBE8F09597'})
Write-Host '== 1/2 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -A -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'feat/core-input-gate' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'feat/core-input-gate' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('Core','docs/mods/core'); Commit 'feat(core): shared input gate, HookSet.Input/RemoveAll (0.2.0)' 'One Core prefix on InputSystemManager.OnEventCaptureInput; every registered handler runs, any false swallows the input, a throwing handler is logged once and allows. HookSet.RemoveAll removes a mod''s hooks and handlers. Additive; mods built against 0.1.0 keep working. All mods rebuilt and all suites pass.'
Add @('InstantEntrance/tests','ItemRebuild/tests'); Commit 'test: Core 0.2.0 doubles for suites that compile Core sources' ''
Add @('Contribution','docs/mods/contribution'); Commit 'refactor(contribution): input capture on the Core gate (0.5.6)' 'Same handler logic; hooks through HookSet (declaredOnly: false as before). Requires Restitutor.Core 0.2.0.'
Add @('TabCharacters','docs/mods/tab-characters'); Commit 'refactor(tab-characters): input capture on the Core gate (0.6.12)' 'BookCapture registered on the gate; UnpatchSelf paths now RemoveHooks (hooks and handler). Requires Restitutor.Core 0.2.0.'
Add @('Map','docs/mods/map'); Commit 'refactor(map): input capture on the Core gate (0.2.4)' 'CaptureClose registered on the gate; hooks through per-handler HookSets on the same Harmony instance. Requires Restitutor.Core 0.2.0.'
Add @('.gitignore','tools','core-batch-3.ps1','core-batch-3.bat'); Commit 'chore: Core batch 3 script' ''
git push -q -u origin 'feat/core-input-gate'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...feat/core-input-gate?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
