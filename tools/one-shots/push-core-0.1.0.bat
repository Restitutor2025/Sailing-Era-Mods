@echo off
rem Restitutor.Core 0.1.0 + Instant Entrance 0.1.1 on branch feat/core-0.1.0. Safe to run again.
setlocal
set BRANCH=feat/core-0.1.0
cd /d "E:\Documents\ChatGPT\Sailing_era_Restitutor" || goto :fail
if exist ".git\index.lock" (
  echo .git\index.lock exists. Make sure no other git is running here, delete it, then run again.
  goto :fail
)
rem Uncommitted Core files are carried over; switching to the old local main would refuse, so branch from origin/main.
git fetch origin || goto :fail
git switch %BRANCH% 2>nul || git switch -c %BRANCH% origin/main || goto :fail
git branch -f main origin/main >nul 2>&1
call :add .gitignore Core docs/mods/core || goto :fail
git diff --cached --quiet || git commit -q -m "feat(core): add Restitutor.Core 0.1.0 (hook helper, game-ready registration)" -m "Shared library for UserLibs: CoreInfo version check, HookSet (exactly one target, handlers resolved before patching, all-or-nothing install, UIManager refused before the game is ready), GameReady (runs once on the first scene-entered frame, per-handler exception isolation, frame callback removed afterwards). No gameplay logic, no hooks of its own." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add InstantEntrance docs/mods/instant-entrance || goto :fail
git diff --cached --quiet || git commit -q -m "refactor(instant-entrance): register hooks through Restitutor.Core (0.1.1)" -m "Same three targets, argument types, handlers and order as 0.1.0; handler bodies unchanged. Requires Restitutor.Core 0.1.0; without it the mod logs one error and stays disabled." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add install-core-0.1.0.ps1 install-core-0.1.0.bat push-core-0.1.0.bat || goto :fail
git diff --cached --quiet || git commit -q -m "chore: add Core 0.1.0 install script" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
echo.
echo ---- commits on %BRANCH% ----
git log --oneline origin/main..%BRANCH%
echo ----------------------------
git status --short
git push -u origin %BRANCH% || goto :fail
echo.
echo Done. Open the pull request: https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...feat/core-0.1.0?expand=1
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
