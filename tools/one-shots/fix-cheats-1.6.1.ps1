# fix-cheats-1.6.1: Cheats Interface 1.6.1 - the cheat window exists only in play (city/sea/land scene of the loaded save), never on the title screen or while loading.
# Run with the game closed. Safe to run again. Roll back: Cheats\releases\1.6.0\Restitutor_Cheats_Interface.dll over the installed file.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or @('36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E') -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
git merge-base --is-ancestor 'fix/contribution-assets' origin/main; if($LASTEXITCODE -ne 0){ Fail '이전 PR(fix/contribution-assets)이 아직 병합되지 않았습니다. GitHub 에서 먼저 Merge 후 다시 실행' }
$jobs=@(
 @{src="$p\Cheats\releases\1.6.1\Restitutor_Cheats_Interface.dll"; dst="$g\Mods\Cheats\Restitutor_Cheats_Interface.dll"; want='EA03DCC7C4A2E8843E77E589E8A26059304A1FCD14F099B488A1ACBEF96F81CA'})
Write-Host '== 1/2 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -A -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'fix/cheats-in-play-only' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'fix/cheats-in-play-only' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }
git branch -f main origin/main 2>$null | Out-Null
Add @('Cheats/Interface','Cheats/lifecycle-tests','docs/mods/cheat/1.6.1.md'); Commit 'fix(cheats): show the cheat window only in play (Interface 1.6.1)' 'The window, H and O now require the loaded save to be the current PlayerDataManager data and a city/sea/land scene that is entered and not loading. Previously Player stayed set after returning to the title, so the window appeared on the title screen.'
Add @('tools/fix-cheats-1.6.1.json','fix-cheats-1.6.1.ps1','fix-cheats-1.6.1.bat'); Commit 'chore: Cheats Interface 1.6.1 install script' ''
git push -q -u origin 'fix/cheats-in-play-only'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...fix/cheats-in-play-only?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
