# sail-trace-0.1.2: SailTrace 0.1.2 (diagnostic) - adds month/day refresh timing; also turns HookCensus off (.dll.off) to UserData\Restitutor\SailTrace\*.jsonl.
# Run with the game closed. Safe to run again. Remove: delete Mods\Analytics\Restitutor_Analytics_SailTrace.dll. HookCensus back: rename .dll.off to .dll.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
$src="$p\SailTrace\release\0.1.2\Restitutor_Analytics_SailTrace.dll"; $dst="$g\Mods\Analytics\Restitutor_Analytics_SailTrace.dll"
$want='9003B51CFAA81EA76182D33FE180EF64061F2CFF41C29B2F45ADE19C4D21C551'
Write-Host '== 1/2 설치' -ForegroundColor Cyan
if(-not (Test-Path "$g\Mods\Analytics\manifest.json")){ Fail 'Mods\Analytics\manifest.json 이 없습니다' }
if((Get-FileHash $src).Hash -ne $want){ Fail "원본 해시 불일치: $src" }
Copy-Item $src $dst -Force; if((Get-FileHash $dst).Hash -ne $want){ Fail "교체 후 불일치: $dst" }; "설치: $(Split-Path $dst -Leaf) 0.1.2"
$hc="$g\Mods\Analytics\Restitutor_Analytics_HookCensus.dll"; if(Test-Path $hc){ Rename-Item $hc 'Restitutor_Analytics_HookCensus.dll.off' -Force; 'HookCensus 끔 (.dll.off)' } else { 'HookCensus 이미 꺼져 있음' }
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01C8pv8RCmVygqf6nAniF6Sx'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'feat/sail-trace-0.1.2' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'feat/sail-trace-0.1.2' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch (다른 브랜치의 미커밋 변경과 충돌했을 수 있음)' } }
function AddF([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -f -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도 (백신 검사 등 일시적 잠금)"; Start-Sleep 3 }; Fail "git add $paths" }
AddF @('SailTrace/Restitutor_Analytics_SailTrace.csproj','SailTrace/README.md','SailTrace/build.ps1','SailTrace/src/EntryPoint.cs','docs/mods/sail-trace/0.1.2.md')
Commit 'feat(sail-trace): month/day refresh timing (0.1.2)' 'Diagnostic only. Adds timing of CalendarMgr.AddGameInsideMonth and PortScheduleManager.OnTheMonthRefresh/OnTheDayRefresh in every scene, and month/day work inside long frames. No game state is changed.'
AddF @('sail-trace-0.1.2.ps1','sail-trace-0.1.2.bat'); Commit 'chore: SailTrace 0.1.2 install script' ''
git push -q -u origin 'feat/sail-trace-0.1.2'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...feat/sail-trace-0.1.2?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
