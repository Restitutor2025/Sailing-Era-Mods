# Generates <name>.ps1 + <name>.bat: game closed + Core check -> hash-checked install -> branch from origin/main
# (requires the previous branch to be merged) -> commits -> push -> open PR page.
import sys,json
def make(name,branch,desc,installs,commits,requires_merged=None,rollback='',core_ok=None):
    core_ok=core_ok or ['928E5F54948C12F769085780447F7E4647B14C86BA076EA0ABEEA7AC55E28027']
    TR='Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>'; SE='Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja'
    L=['\ufeff# '+name+': '+desc,'# Run with the game closed. Safe to run again.'+(' Roll back: '+rollback if rollback else ''),
    "$ErrorActionPreference='Continue'",
    "$p='E:\\Documents\\ChatGPT\\Sailing_era_Restitutor'; $g='E:\\Program\\steam\\steamapps\\common\\Sailing Era'",
    "function Fail($m){ Write-Host \"FAILED: $m\" -ForegroundColor Red; exit 1 }",
    "if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }",
    "$core=\"$g\\UserLibs\\Restitutor.Core.dll\"; if(-not (Test-Path $core) -or @("+','.join("'"+h+"'" for h in core_ok)+") -notcontains (Get-FileHash $core).Hash){ Fail 'UserLibs\\Restitutor.Core.dll 이 없거나 예상 버전이 아닙니다' }",
    "Set-Location $p",
    "if(Test-Path '.git\\index.lock'){ Fail '.git\\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }",
    "git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }"]
    if requires_merged:
        L.append("git merge-base --is-ancestor '"+requires_merged+"' origin/main; if($LASTEXITCODE -ne 0){ Fail '이전 PR("+requires_merged+")이 아직 병합되지 않았습니다. GitHub 에서 먼저 Merge 후 다시 실행' }")
    L+=['$jobs=@(',',\n'.join(f" @{{src=\"$p\\{s}\"; dst=\"$g\\{d}\"; want='{h}'}}" for s,d,h in installs)+')',
    "Write-Host '== 1/2 설치' -ForegroundColor Cyan",
    "foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){ Fail \"원본 해시 불일치: $($j.src)\" } }",
    "foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){ Fail \"교체 후 불일치: $($j.dst)\" }; \"설치: $(Split-Path $j.dst -Leaf)\" }",
    "Write-Host '== 2/2 커밋·push' -ForegroundColor Cyan",
    "function Add([string[]]$paths){ for($i=1;$i -le 5;$i++){ git add -A -- @paths; if($LASTEXITCODE -eq 0){return}; Write-Host \"git add 실패 ($i/5), 3초 뒤 재시도\"; Start-Sleep 3 }; Fail \"git add $paths\" }",
    "function Commit([string]$title,[string]$body){ git diff --cached --quiet; if($LASTEXITCODE -eq 0){ \"(변경 없음) $title\"; return }; $m=@('-m',$title); if($body){$m+=@('-m',$body)}; $m+=@('-m','"+TR+"','-m','"+SE+"'); git commit -q @m; if($LASTEXITCODE -ne 0){ Fail \"commit $title\" }; \"커밋: $title\" }",
    "git switch -q '"+branch+"' 2>$null; if($LASTEXITCODE -ne 0){ git switch -q -c '"+branch+"' origin/main; if($LASTEXITCODE -ne 0){ Fail 'git switch' } }",
    "git branch -f main origin/main 2>$null | Out-Null"]
    for paths,title,body in commits:
        L.append('Add @('+','.join("'"+x+"'" for x in paths)+"); Commit '"+title.replace("'","''")+"' '"+body.replace("'","''")+"'")
    L+=["git push -q -u origin '"+branch+"'; if($LASTEXITCODE -ne 0){ Fail 'git push' }",
    "git log --oneline origin/main..HEAD",
    "$pr='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main..."+branch+"?expand=1'",
    "Write-Host \"완료. PR 페이지를 엽니다 → Create pull request → Merge: $pr\" -ForegroundColor Green",
    "Start-Process $pr"]
    open(name+'.ps1','w',encoding='utf-8').write('\r\n'.join(L)+'\r\n')
    open(name+'.bat','wb').write(('@echo off\r\npowershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0'+name+'.ps1"\r\npause\r\n').encode())
if __name__=='__main__':
    make(**json.load(open(sys.argv[1],encoding='utf-8')))
