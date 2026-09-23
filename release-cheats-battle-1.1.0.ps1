# release-cheats-battle-1.1.0: Cheats Battle 1.1.0 — checkboxes instant boarding (flagship) + hull lock (controlled ship); multipliers and checkboxes kept between battles. No new hooks.
# Double-click the .bat (game closed). Works from any folder. Safe to run again.
# Roll back: copy Cheats\releases\1.5.1\Restitutor_Cheats_Battle.dll over Mods\Cheats\Restitutor_Cheats_Battle.dll
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
function Retry([string]$what,[scriptblock]$cmd){ for($i=1;$i -le 5;$i++){ & $cmd; if($LASTEXITCODE -eq 0){ return }; Write-Host "$what 실패 ($i/5), 3초 뒤 재시도" -ForegroundColor Yellow; Start-Sleep 3 }; Fail $what }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or @('36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E') -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전(0.2.0)이 아닙니다' }
Set-Location $p; if((Get-Location).Path -ne $p){ Fail "폴더 이동 실패: $p" }
if(Test-Path '.git\index.lock'){ Start-Sleep 3; if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 남아 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' } }
Write-Host '== 1/3 설치' -ForegroundColor Cyan
$src="$p\Cheats\releases\battle-1.1.0\Restitutor_Cheats_Battle.dll"; $dst="$g\Mods\Cheats\Restitutor_Cheats_Battle.dll"
if((Get-FileHash $src).Hash -ne '8036C9BCB4E05241A1B17E728CC81DFD774E820288D16B759F1AE7011D6B3196'){ Fail "원본 해시 불일치: $src" }
Copy-Item $src $dst -Force; if((Get-FileHash $dst).Hash -ne '8036C9BCB4E05241A1B17E728CC81DFD774E820288D16B759F1AE7011D6B3196'){ Fail "교체 후 불일치: $dst" }; '설치: Restitutor_Cheats_Battle.dll 1.1.0'
Write-Host '== 2/3 브랜치·커밋' -ForegroundColor Cyan
Retry 'git fetch' { git fetch -q origin }
$cur=git branch --show-current
if($cur -ne 'feat/cheats-battle-toggles'){ git show-ref --verify --quiet 'refs/heads/feat/cheats-battle-toggles'; if($LASTEXITCODE -eq 0){ Retry 'git switch' { git switch -q 'feat/cheats-battle-toggles' } } else { Retry 'git switch -c' { git switch -q -c 'feat/cheats-battle-toggles' origin/main } } }
function Commit([string[]]$paths,[string]$title,[string]$body){ Retry 'git add' { git add -f -- @paths }; git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01UhZyZcJHD7b3M3Z3NrDrFK'); Retry "git commit $title" { git commit -q @m }; "커밋: $title" }
Commit @('Cheats/Battle/BattlePanel.cs','Cheats/Battle/BattleRuntime.cs','Cheats/Battle/EntryPoint.cs','Cheats/Battle/GameLinks.cs','Cheats/Battle/Rules.cs','Cheats/Battle/Restitutor_Cheats_Battle.csproj','Cheats/battle-tests/Program.cs','Cheats/battle-tests/Stubs.cs','docs/mods/cheat/battle-1.1.0.md') 'feat(cheats-battle): instant boarding and hull lock checkboxes, keep choices between battles (1.1.0)' 'Two checkboxes under the cannon row. Instant boarding: one delegate on the flagship BoatEntityBoardShoot.BoardShootingBegin (never assigned by the game), next frame AddProgress through the original CheckProgressFull path; re-entry after boarding allowed. Hull lock: the game own BoatEntityHitHandler.lockHealth on the controlled ship, restored at battle end. The X1..X5 multipliers and the checkboxes are now kept between sea battles and reset only on session reset (save load, title, O off). No new Harmony hooks; battle tests 87 pass.'
Commit @('release-cheats-battle-1.1.0.ps1','release-cheats-battle-1.1.0.bat') 'chore: release script for Cheats Battle 1.1.0' ''
Write-Host '== 3/3 push·PR' -ForegroundColor Cyan
Retry 'git push' { git push -q -u origin 'feat/cheats-battle-toggles' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...feat/cheats-battle-toggles?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
