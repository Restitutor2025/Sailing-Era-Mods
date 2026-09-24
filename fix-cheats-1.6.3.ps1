# fix-cheats-1.6.3: Cheats Interface 1.6.3 - key hint line "[H] 접기·펼치기   [O] 모든 치트 끄기·켜기" under the cheat window title (hidden while folded).
# Run (game closed, from any folder): powershell -NoProfile -ExecutionPolicy Bypass -File "E:\Documents\ChatGPT\Sailing_era_Restitutor\fix-cheats-1.6.3.ps1"
# Safe to run again. Roll back: copy Cheats\releases\1.6.2\Restitutor_Cheats_Interface.dll over Mods\Cheats\Restitutor_Cheats_Interface.dll
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
$b='feat/cheats-key-hint'; $wt='E:\Documents\ChatGPT\_wt-cheats-1.6.3'
$want='FA5BF5C80670B07655B8D12006EFED51F8C43A024EC77AF7DF379B4DBAFFA477'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
function Retry([string]$what,[scriptblock]$cmd){ for($i=1;$i -le 5;$i++){ & $cmd; if($LASTEXITCODE -eq 0){ return }; Write-Host "$what 실패 ($i/5), 3초 뒤 재시도" -ForegroundColor Yellow; Start-Sleep 3 }; Fail $what }
if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
$core="$g\UserLibs\Restitutor.Core.dll"; if(-not (Test-Path $core) -or (Get-FileHash $core).Hash -ne '36B96E0839E969CB36607A11A631917DDFCE3B6B08F756948FFA8DFEAE0EFA5E'){ Fail 'UserLibs\Restitutor.Core.dll 이 없거나 예상 버전(0.2.0)이 아닙니다' }
Set-Location $p; if((Get-Location).Path -ne $p){ Fail "폴더 이동 실패: $p" }
if(Test-Path '.git\index.lock'){ Start-Sleep 3; if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 남아 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' } }

Write-Host '== 1/3 설치' -ForegroundColor Cyan
$src="$p\Cheats\releases\1.6.3\Restitutor_Cheats_Interface.dll"; $dst="$g\Mods\Cheats\Restitutor_Cheats_Interface.dll"
if((Get-FileHash $src).Hash -ne $want){ Fail "원본 해시 불일치: $src" }
Copy-Item $src $dst -Force; if((Get-FileHash $dst).Hash -ne $want){ Fail "교체 후 불일치: $dst" }; '설치: Restitutor_Cheats_Interface.dll 1.6.3'

Write-Host '== 2/3 worktree 커밋' -ForegroundColor Cyan
$files=@('Cheats/Interface/Host.cs','Cheats/Interface/Restitutor_Cheats_Interface.csproj','Cheats/lifecycle-tests/Program.cs','docs/mods/cheat/1.6.3.md','fix-cheats-1.6.3.ps1')
foreach($f in $files){ if(-not (Test-Path (Join-Path $p $f))){ Fail "파일 없음: $f" } }
Retry 'git fetch' { git fetch -q origin }
git worktree prune 2>$null
if(Test-Path $wt){ git worktree remove --force $wt 2>$null; Remove-Item -Recurse -Force $wt -EA SilentlyContinue }
if(Test-Path $wt){ Fail "이전 worktree 폴더를 지우지 못했습니다: $wt" }
Retry 'git worktree add' { git worktree add -q -B $b $wt origin/main }
foreach($f in $files){ $d=Join-Path $wt $f; New-Item -ItemType Directory -Force (Split-Path $d) | Out-Null; Copy-Item (Join-Path $p $f) $d -Force }
Set-Location $wt
Retry 'git add' { git add -f -- @files }
git diff --cached --quiet
if($LASTEXITCODE -ne 0){
  Retry 'git commit' { git commit -q -m 'feat(cheats): key hint line under the cheat window title (Interface 1.6.3)' -m 'One text line under the title: [H] fold/unfold, [O] all cheats off/on (Korean text chosen by the user). Hidden while the window is folded. Panel list and scrollbar start 26 px lower; window height = list + 86. No hooks or checks added. Lifecycle checks 162 pass.' -m 'Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>' -m 'Claude-Session: https://claude.ai/code/session_01UhZyZcJHD7b3M3Z3NrDrFK' }
  '커밋 완료'
} else { '(변경 없음 — 이미 커밋됨)' }

Write-Host '== 3/3 push·PR' -ForegroundColor Cyan
Retry 'git push' { git push -q -f -u origin $b }
git --no-pager log --oneline origin/main..HEAD
Set-Location $p; git worktree remove --force $wt 2>$null
# The change now lives on the branch: put the main checkout's copies back so a later pull of main does not collide.
git checkout -- 'Cheats/Interface/Host.cs' 'Cheats/Interface/Restitutor_Cheats_Interface.csproj' 'Cheats/lifecycle-tests/Program.cs' 2>$null
Remove-Item "$p\docs\mods\cheat\1.6.3.md" -EA SilentlyContinue
$pr="https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...$b`?expand=1"
Write-Host "완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr" -ForegroundColor Green
Start-Process $pr
Remove-Item $PSCommandPath -EA SilentlyContinue
