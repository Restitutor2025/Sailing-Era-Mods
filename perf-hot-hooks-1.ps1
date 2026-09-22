# perf-hot-hooks-1: remove hooks on frequently-called shared functions.
#   Cheats Contribution 1.1.4 (no GetPointProperty/GetProperty hooks), Cheats Speed 1.2.0 (no FixedUpdate hook), Intro 0.1.4 (no Transition._Play hook).
# Run with the game closed. Safe to run again. Previous DLLs are kept under <project>\backup-2026-09-23\.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
if((Get-FileHash "$g\UserLibs\Restitutor.Core.dll").Hash -ne '36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E'){ Fail 'Restitutor.Core 0.2.0 이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다' }
if(Get-ChildItem "$g\Mods\Cheats\*.dll.off" -EA SilentlyContinue){ Fail 'Mods\Cheats 에 .dll.off 가 있습니다 (치트를 먼저 켜 주세요)' }
$jobs=@(
 @{src="$p\Cheats\releases\contribution-1.1.4\Restitutor_Cheats_Contribution.dll"; dst="$g\Mods\Cheats\Restitutor_Cheats_Contribution.dll"; want='416B3ACFE190D2B9A67B6261DDC7BC512EB67FE8251A72DA910E36246BFC2DBD'},
 @{src="$p\Cheats\releases\speed-1.2.0\Restitutor_Cheats_Speed.dll"; dst="$g\Mods\Cheats\Restitutor_Cheats_Speed.dll"; want='D1EF6B0D037D03CBB5E20589E5369569FB3F0626248D3677E520107B97E9D1F6'},
 @{src="$p\archive-intro\0.1.4\Restitutor_Additional_IntroSkip.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_IntroSkip.dll"; want='1043F4B0074C574DF7CDA617CD0705F8D14A9F0451D08CE1C60284D2F0B73259'})
Write-Host '== 1/2 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
$bak="$p\backup-2026-09-23"; New-Item -ItemType Directory -Force $bak | Out-Null
foreach($j in $jobs){ $b="$bak\$(Split-Path $j.dst -Leaf)"; if((Test-Path $j.dst) -and -not (Test-Path $b) -and (Get-FileHash $j.dst).Hash -ne $j.want){ Copy-Item $j.dst $b } }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
function AddF([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -f -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01C8pv8RCmVygqf6nAniF6Sx'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'perf/no-hot-hooks' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'perf/no-hot-hooks' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch (다른 브랜치의 미커밋 변경과 충돌했을 수 있음)' } }
AddF @('Cheats/Contribution/EntryPoint.cs','Cheats/Contribution/Restitutor_Cheats_Contribution.csproj','Cheats/contribution-sim/Program.cs','Cheats/contribution-sim/ContributionSim.csproj','docs/mods/cheat/contribution-1.1.3.md','docs/mods/cheat/contribution-1.1.4.md')
Commit 'perf(cheats): set contribution without native hooks (Contribution 1.1.4)' 'Il2CppInterop keeps the native detour after UnpatchSelf, so patching only during Apply (1.1.3) still slowed every GetPointProperty/GetProperty call after the first Apply (monthly refresh 19 ms -> 303 ms). The native UpdateInfluence now runs unhooked with a delta chosen from the commander rate (property 21) and culture bonus (point 133); leftovers are corrected with more calls without leaving the start/target 100-bracket. Simulator: 9,000 cases exact, no threshold crossing, max 4 calls.'
AddF @('Cheats/Speed/EntryPoint.cs','Cheats/Speed/SailingSpeed.cs','Cheats/tests/SailingChecks.cs','Cheats/tests/SailingStubs.cs','docs/mods/cheat/speed-1.2.0.md')
Commit 'perf(cheats): drop the FixedUpdate hook (Speed 1.2.0)' 'The FixedUpdate prefix ran for every boat (40-67 at sea, 2,000-2,900 calls/s) to change one. The player flagship driver (OceanScene.FocusBoatReference.LeaderDirectionMove) now gets forwardPowerFactorByEscape = original x multiplier once per frame from the panel update and is restored on X1, gate close, flagship change, cheats off and reset; values the game writes are kept as the new original. Tests: 175 pass.'
AddF @('src/IntroSkipMod.cs','docs/mods/intro/0.1.4.md')
Commit 'perf(intro): drop the Transition._Play hook (Intro 0.1.4)' 'Every FairyGUI animation passed through the _Play postfix. The two registered notice transitions are now checked with Transition.playing only while the launch view exists.'
AddF @('perf-hot-hooks-1.ps1','perf-hot-hooks-1.bat'); Commit 'chore: hot-hook removal install script' ''
git push -q -u origin 'perf/no-hot-hooks'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...perf/no-hot-hooks?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
