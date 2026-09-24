@echo off
rem Stat Rank 0.1.6 on Restitutor.Core, branch refactor/stat-rank-core. Safe to run again.
setlocal
set BRANCH=refactor/stat-rank-core
cd /d "E:\Documents\ChatGPT\Sailing_era_Restitutor" || goto :fail
if exist ".git\index.lock" (
  echo .git\index.lock exists. Make sure no other git is running here, delete it, then run again.
  goto :fail
)
git fetch origin || goto :fail
git switch %BRANCH% 2>nul || git switch -c %BRANCH% origin/main || goto :fail
git branch -f main origin/main >nul 2>&1
call :add StatRank docs/mods/stat-rank || goto :fail
git diff --cached --quiet || git commit -q -m "refactor(stat-rank): register hooks through Restitutor.Core (0.1.6)" -m "Same two postfixes, same GameAssembly baseline check and order; handler and tooltip code unchanged. Requires Restitutor.Core 0.1.0." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add .gitignore install-stat-rank-0.1.6.ps1 install-stat-rank-0.1.6.bat push-stat-rank-core.bat || goto :fail
git diff --cached --quiet || git commit -q -m "chore: add Stat Rank 0.1.6 install script" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
echo.
echo ---- commits on %BRANCH% ----
git log --oneline origin/main..%BRANCH%
echo ----------------------------
git status --short
git push -u origin %BRANCH% || goto :fail
echo.
echo Done. Open the pull request: https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...refactor/stat-rank-core?expand=1
pause
exit /b 0
:fail
echo FAILED. Nothing further was run. Safe to run this file again: finished commits are skipped.
pause
exit /b 1

:add
for /l %%i in (1,1,5) do (
  git add -A -- %* && exit /b 0
  echo git add failed ^(try %%i/5^), retrying in 3s...
  timeout /t 3 /nobreak >nul
)
exit /b 1
