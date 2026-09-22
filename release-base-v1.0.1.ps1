# release-base-v1.0.1: commit Base v1.0.1 package sources in a temporary worktree, push, PR, merge. Does not touch the current checkout or the game.
$ErrorActionPreference='Continue'
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $b='chore/base-v1.0.1-gdrive'; $wt='E:\Documents\ChatGPT\_wt-base-v1.0.1'
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; exit 1 }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 작업이 끝났는지 확인 후 지우고 다시 실행' }
$zip="$p\release\v1.0.1-base\Restitutor-Base-v1.0.1.zip"; if((Get-FileHash $zip).Hash -ne '9FE497BD3B6EB7A6BF87DC85019A909501F43883C9DC42967F79797C2C25872F'){ Fail 'zip 해시가 예상과 다릅니다' }
git fetch -q origin; if($LASTEXITCODE -ne 0){ Fail 'git fetch' }
if(Test-Path $wt){ git worktree remove --force $wt 2>$null; Remove-Item -Recurse -Force $wt -EA SilentlyContinue }
git worktree add -q -B $b $wt origin/main; if($LASTEXITCODE -ne 0){ Fail 'git worktree add' }
$files=@('tools/make_base_zip.py','release/RELEASE_NOTES-v1.0.1-base.md','release/v1.0.1-base/README_Restitutor-Base.txt','release/readme-draft/README_Restitutor-Base.txt','release/readme-draft/README_Restitutor-Cheats.txt','release/readme-draft/README_Restitutor-Rebalance.txt','release-base-v1.0.1.ps1','release-base-v1.0.1.bat')
foreach($f in $files){ $d=Join-Path $wt $f; New-Item -ItemType Directory -Force (Split-Path $d) | Out-Null; Copy-Item (Join-Path $p $f) $d -Force }
Set-Location $wt
git add -f -- @files; if($LASTEXITCODE -ne 0){ Fail 'git add' }
git commit -q -m 'chore(release): Base v1.0.1 package for Google Drive' -m 'Base only, shared via Google Drive. v1.0.0 Base DLLs unchanged (hash-checked) + manifest.json in each Mods subfolder (MelonLoader 0.7.2+ skips subfolders without it) + user README. Also README drafts for Cheats/Rebalance.' -m 'Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>' -m 'Claude-Session: https://claude.ai/code/session_01HQfSXEh7e6kC2bxDF2S7Jz'; if($LASTEXITCODE -ne 0){ Fail 'git commit' }
git push -q -u origin $b; if($LASTEXITCODE -ne 0){ Fail 'git push' }
Set-Location $p; git worktree remove --force $wt
$compare='https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...'+$b+'?expand=1'
if(-not (Get-Command gh -EA SilentlyContinue)){ Write-Host "gh 가 없어 PR 페이지를 엽니다 → Create pull request → Merge: $compare" -ForegroundColor Yellow; Start-Process $compare; exit 0 }
$body=@'
Base v1.0.1 package for Google Drive: v1.0.0 Base DLLs unchanged + manifest.json per Mods subfolder + README. Zip itself is not committed.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_01HQfSXEh7e6kC2bxDF2S7Jz
'@
gh pr create -R Restitutor2025/Sailing-Era-Mods --base main --head $b --title 'chore(release): Base v1.0.1 package for Google Drive' --body $body; if($LASTEXITCODE -ne 0){ Fail 'gh pr create' }
gh pr merge -R Restitutor2025/Sailing-Era-Mods $b --merge --delete-branch; if($LASTEXITCODE -ne 0){ Fail 'gh pr merge (PR 은 만들어짐 — GitHub 에서 직접 Merge)' }
Write-Host '완료: 커밋·push·PR·병합. 배포 파일:' -ForegroundColor Green; Write-Host $zip; explorer.exe /select,$zip
