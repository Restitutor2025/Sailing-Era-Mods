# perf-contribution-1.1.3: Cheats Contribution 1.1.3 (GetPointProperty/GetProperty hooked only during Apply) + SailTrace 0.1.3 (save/weather timing).
# Also turns all cheats back on (the .dll.off test state). Run with the game closed. Safe to run again.
# Roll back Contribution: copy Cheats\releases\contribution-1.1.2-installed\Restitutor_Cheats_Contribution.dll over the installed file.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if((Get-FileHash $core).Hash -ne '36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E'){ Fail 'UserLibs\Restitutor.Core.dll 이 예상 버전(0.2.0)이 아닙니다' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다' }
$c="$g\Mods\Cheats"
$jobs=@(
 @{src="$p\Cheats\releases\contribution-1.1.3\Restitutor_Cheats_Contribution.dll"; dst="$c\Restitutor_Cheats_Contribution.dll"; want='2D93E22DCE188E661D0DEB52E72CB8E33821E97327C8EFBD55B18764C5AFA646'},
 @{src="$p\SailTrace\release\0.1.3\Restitutor_Analytics_SailTrace.dll"; dst="$g\Mods\Analytics\Restitutor_Analytics_SailTrace.dll"; want='4984F828B6408619762EDC1C855A29DDD2C5A6F68F8EA884DE92B4544183488E'})
Write-Host '== 1/3 설치' -ForegroundColor Cyan
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail "원본 해시 불일치: $($j.src)" } }
$bak="$p\Cheats\releases\contribution-1.1.2-installed"; New-Item -ItemType Directory -Force $bak | Out-Null
foreach($old in @("$c\Restitutor_Cheats_Contribution.dll.off","$c\Restitutor_Cheats_Contribution.dll")){ if((Test-Path $old) -and (Get-FileHash $old).Hash -eq '7CB78E5362C3A46ACAF79EA0A99338CEE175FAA08FD7BCC177A02A29C7B121FB'){ Move-Item $old "$bak\Restitutor_Cheats_Contribution.dll" -Force; "보관: 1.1.2 → $bak" } }
if(Test-Path "$c\Restitutor_Cheats_Contribution.dll.off"){ Fail "Mods\Cheats\Restitutor_Cheats_Contribution.dll.off 가 1.1.2 가 아닙니다. 확인 필요" }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail "교체 후 불일치: $($j.dst)" }; "설치: $(Split-Path $j.dst -Leaf)" }
Write-Host '== 2/3 치트 다시 켜기' -ForegroundColor Cyan
Get-ChildItem "$c\*.dll.off" | ForEach-Object { $n=$_.Name -replace '\.off$',''; if(Test-Path "$c\$n"){ Fail "$n 이 이미 있습니다" }; Rename-Item $_.FullName $n; "켬: $n" }
Write-Host '== 3/3 커밋·push' -ForegroundColor Cyan
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
function AddF([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -f -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01C8pv8RCmVygqf6nAniF6Sx'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'perf/contribution-lazy-hooks' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'perf/contribution-lazy-hooks' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch (다른 브랜치의 미커밋 변경과 충돌했을 수 있음)' } }
AddF @('Cheats/Contribution/EntryPoint.cs','Cheats/Contribution/Restitutor_Cheats_Contribution.csproj','docs/mods/cheat/contribution-1.1.3.md')
Commit 'perf(cheats): hook GetPointProperty/GetProperty only during Apply (Contribution 1.1.3)' 'The commander-modifier prefixes were installed at load and ran on every native call of two stat getters. The monthly market refresh makes ~65,000 such calls in one frame (19 ms without hooks, ~170 ms hooked). They are now patched right before UpdateInfluence and unpatched in finally; if unpatching fails they stay for the session. Behaviour unchanged.'
AddF @('SailTrace/Restitutor_Analytics_SailTrace.csproj','SailTrace/README.md','SailTrace/src/EntryPoint.cs','docs/mods/sail-trace/0.1.3.md')
Commit 'feat(sail-trace): save and weather timing (0.1.3)' 'Diagnostic only. Times PlayerDataManager.SavePlayerData/AutoSavePlayerData, PlatformManager.SaveData, NpcAgentManager.SaveAllBehaviorsData, WeatherMgr.PlayWeather, WeatherController.ChangeWeather and CloudController.OnTileCloudChange and lists them in long-frame rows.'
AddF @('perf-contribution-1.1.3.ps1','perf-contribution-1.1.3.bat'); Commit 'chore: Contribution 1.1.3 + SailTrace 0.1.3 install script' ''
git push -q -u origin 'perf/contribution-lazy-hooks'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...perf/contribution-lazy-hooks?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
