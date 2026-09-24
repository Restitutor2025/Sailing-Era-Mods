@echo off
rem Sailing Era C# mods repo: add all current mods on one branch, one commit per mod. Run from anywhere.
setlocal
set BRANCH=chore/add-mods
cd /d "E:\Documents\ChatGPT\Sailing_era_Restitutor" || goto :fail
if exist ".git\index.lock" (
  echo .git\index.lock exists. Make sure no other git is running here, delete it, then run again.
  goto :fail
)
git switch main || goto :fail
git pull --ff-only || goto :fail
git switch -c %BRANCH% 2>nul || git switch %BRANCH% || goto :fail
call :add .gitignore NuGet.Config || goto :fail
git diff --cached --quiet || git commit -q -m "chore: allowlist all current mods, shared NuGet.Config" -m "Each mod block re-includes only csproj, src/tests .cs, build scripts and README; bin/obj, archive, evidence, releases, logs and game-extracted data stay out." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add StatRank docs/mods/stat-rank || goto :fail
git diff --cached --quiet || git commit -q -m "feat(stat-rank): tooltip alpha 0.8 (0.1.5)" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add Restitutor_fixes.csproj src docs/mods/intro || goto :fail
git diff --cached --quiet || git commit -q -m "chore(intro): add Intro Skip 0.1.2 source" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add TextSpeed docs/mods/textspeed || goto :fail
git diff --cached --quiet || git commit -q -m "chore(textspeed): add Text Speed 0.1.5 source" -m "0.1.5: no per-frame LINQ in LateUpdate (resource cleanup, behavior unchanged)." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add InstantEntrance docs/mods/instant-entrance || goto :fail
git diff --cached --quiet || git commit -q -m "chore(instant-entrance): add Instant Entrance 0.1.0 source" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add FleetInfo docs/mods/fleet-info || goto :fail
git diff --cached --quiet || git commit -q -m "chore(fleet-info): add Fleet Info 0.1.0 source" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add TradeExp docs/mods/trade-exp || goto :fail
git diff --cached --quiet || git commit -q -m "chore(trade-exp): add Trade Exp 0.1.1 source (not installed)" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add CTRLInstant docs/mods/ctrl-instant || goto :fail
git diff --cached --quiet || git commit -q -m "chore(ctrl-instant): add CTRL Instant 0.1.6 source" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add Contribution docs/mods/contribution || goto :fail
git diff --cached --quiet || git commit -q -m "chore(contribution): add Contribution 0.5.5 source" -m "0.5.5: harbor HUD check and benefit text on events (focus, main UI, city/port, notices) instead of every frame; postfix-only hooks registered on the first city frame." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add ItemRebuild docs/mods/item-rebuild || goto :fail
git diff --cached --quiet || git commit -q -m "chore(item-rebuild): add Item Rebuild 0.1.22 source" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add TabCharacters docs/mods/tab-characters || goto :fail
git diff --cached --quiet || git commit -q -m "chore(tab-characters): add Tab Characters 0.6.11 source" -m "0.6.10: right-click close swallows only its own Action_B until release. 0.6.11: idle key read removed." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add DevilFruits docs/mods/devil-fruits || goto :fail
git diff --cached --quiet || git commit -q -m "chore(devil-fruits): add Devil Fruits 0.1.2 source" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add Map docs/mods/map || goto :fail
git diff --cached --quiet || git commit -q -m "chore(map): add Map 0.2.3 source (candidates/tab-only)" -m "0.2.3: unreachable ports drawn by the native UpdateInfo red-port branch; no per-frame icon overwrite." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add HookCensus docs/mods/hook-census || goto :fail
git diff --cached --quiet || git commit -q -m "chore(hook-census): add HookCensus 0.1.0 diagnostic source" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add Cheats docs/mods/cheat || goto :fail
git diff --cached --quiet || git commit -q -m "chore(cheats): add Cheats source (Interface 1.5.1 + feature DLLs + tests)" -m "Optional package: distributed separately from the base mods." -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add docs || goto :fail
git diff --cached --quiet || git commit -q -m "docs: shared mod docs (README, compatibility, patches, retired diagnostics)" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
call :add install-2026-09-22-perf.ps1 || goto :fail
git diff --cached --quiet || git commit -q -m "chore: add 2026-09-22 install script" -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" -m "Claude-Session: https://claude.ai/code/session_01QAvCUBF7RSjUXDbamCbmja" || goto :fail
echo.
echo ---- commits on %BRANCH% ----
git log --oneline main..%BRANCH%
echo ----------------------------
git status --short
git push -u origin %BRANCH% || goto :fail
echo.
echo Done. Open the pull request: https://github.com/Restitutor2025/Sailing-Era-Mods/compare/main...chore/add-mods?expand=1
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
