# hook-census-0.1.1: HookCensus 0.1.1 (diagnostic) - adds boat-instance count, fixedDeltaTime/timeScale and [HITCH] frame lines with GC/heap deltas.
# Run with the game closed. Safe to run again. Roll back: HookCensus\release\0.1.0\Restitutor_Analytics_HookCensus.dll over the installed file.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
$src="$p\HookCensus\release\0.1.1\Restitutor_Analytics_HookCensus.dll"; $dst="$g\Mods\Analytics\Restitutor_Analytics_HookCensus.dll"
$want='C86CBD3302C94884390B6788D93AC3663E8FBD112EF49F128AAFFE9329CE4140'
Write-Host '== 1/2 설치' -ForegroundColor Cyan
if((Get-FileHash $src).Hash -ne $want){ Fail "원본 해시 불일치: $src" }
Copy-Item $src $dst -Force; if((Get-FileHash $dst).Hash -ne $want){ Fail "교체 후 불일치: $dst" }; "설치: $(Split-Path $dst -Leaf) 0.1.1"
Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -A -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host "git add 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail "git add $paths" }
function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ "(변경 없음) $title"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>','-m','Claude-Session: https://claude.ai/code/session_01C8pv8RCmVygqf6nAniF6Sx'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail "commit $title" }; "커밋: $title" }
git switch -q 'feat/hook-census-0.1.1' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c 'feat/hook-census-0.1.1' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch (다른 브랜치의 미커밋 변경과 충돌했을 수 있음)' } }
Add @('HookCensus/Restitutor_Analytics_HookCensus.csproj','HookCensus/src','docs/mods/hook-census/0.1.1.md'); Commit 'feat(hook-census): boat count, physics step and hitch-frame GC logging (0.1.1)' 'Each [CENSUS] line now carries distinct BoatEntityOceanDriver instances, Time.fixedDeltaTime/timeScale, max frame gap, managed GC counts and IL2CPP heap size. Frames >= 40 ms outside loading get a [HITCH] line with GC/heap deltas. Diagnostic only; no game state is changed.'
git add -f -- 'hook-census-0.1.1.ps1' 'hook-census-0.1.1.bat'; if($LASTEXITCODE -ne 0){ Fail 'git add script' }; Commit 'chore: HookCensus 0.1.1 install script' ''
git push -q -u origin 'feat/hook-census-0.1.1'; if($LASTEXITCODE -ne 0){ Fail 'git push' }
git log --oneline origin/main..HEAD
$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...feat/hook-census-0.1.1?expand=1'
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
