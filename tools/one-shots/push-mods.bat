@echo off
rem Sailing Era C# mods repo: first push + Stat Rank branch. Run from anywhere.
rem 1) Create an EMPTY GitHub repo (no README/license) under Restitutor2025 first.
rem 2) If you chose another name, change REPO below.
setlocal
set REPO=Sailing-Era-Mods
set BRANCH=fix/stat-rank-hover
cd /d "E:\Documents\ChatGPT\Sailing_era_Restitutor" || goto :fail

if exist ".git\index.lock" (
  echo .git\index.lock exists. Make sure no other git is running here, delete it, then run again.
  goto :fail
)

git branch -M main || goto :fail
git remote get-url origin >nul 2>&1
if errorlevel 1 (
  git remote add origin git@github.com:Restitutor2025/%REPO%.git || goto :fail
)
git add .gitignore || goto :fail
git commit -m "chore: allowlist .gitignore for the C# mods repo" || goto :fail
git push -u origin main || goto :fail

git switch -c %BRANCH% || goto :fail
git add StatRank docs/mods/stat-rank || goto :fail
echo.
echo ---- files to be committed ----
git diff --cached --name-only
echo -------------------------------
git commit -m "fix(stat-rank): show grade odds on hover and guard null content (0.1.4)" -m "btnReturn is the hit target over the grade letters, so onRollOver never fired; hover is now polled by cursor position while the sheet is open. RefreshTipsRoleInfo postfix returns when content/roleInfo is null, as the native method does. Tip alpha 0.7, spacing scales with font size." || goto :fail
git push -u origin %BRANCH% || goto :fail

echo.
echo Done. Open a pull request on GitHub: %BRANCH% -^> main
pause
exit /b 0
:fail
echo FAILED. Nothing further was run.
pause
exit /b 1
