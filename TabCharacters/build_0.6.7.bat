@echo off
rem Tab Characters 0.6.7 build: runs build.ps1 (restore, build, tests, interop verify, copy DLL to project root). Does not touch the game Mods folder.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
echo.
echo Exit code: %ERRORLEVEL%
pause
