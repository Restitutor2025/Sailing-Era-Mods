@echo off
rem Builds Restitutor_Cheats_Exp.dll with the project's isolated NuGet profile (Cheats/README.md),
rem runs the rule checks, then installs it to Mods\Cheats. Close the game before running.
setlocal
set "ROOT=%~dp0..\.."
for %%I in ("%ROOT%") do set "ROOT=%%~fI"
set "APPDATA=%ROOT%\Contribution\build-profile"
set "NUGET_PACKAGES=%ROOT%\.packages"
set "GAME=E:\Program\steam\steamapps\common\Sailing Era"
set "LOG=%~dp0build.log"
echo [%date% %time%] build start > "%LOG%"
tasklist /FI "IMAGENAME eq SailingEra.exe" | find /I "SailingEra.exe" >nul
if not errorlevel 1 (echo Game is running. Close it and run again. >> "%LOG%" & set "RC=1" & goto done)
dotnet build "%~dp0Restitutor_Cheats_Exp.csproj" -c Release -p:NuGetAudit=false >> "%LOG%" 2>&1
set "RC=%ERRORLEVEL%"
echo build exit=%RC% >> "%LOG%"
if not "%RC%"=="0" goto done
dotnet run --project "%ROOT%\Cheats\exp-tests\Tests.csproj" -c Release >> "%LOG%" 2>&1
set "RC=%ERRORLEVEL%"
echo tests exit=%RC% >> "%LOG%"
if not "%RC%"=="0" goto done
set "OUT=%ROOT%\Cheats\releases\exp-1.0.0"
if not exist "%OUT%" mkdir "%OUT%"
copy /Y "%~dp0bin\Release\net6.0\Restitutor_Cheats_Exp.dll" "%OUT%\" >> "%LOG%"
copy /Y "%~dp0bin\Release\net6.0\Restitutor_Cheats_Exp.dll" "%GAME%\Mods\Cheats\" >> "%LOG%"
powershell -NoProfile -Command "Get-FileHash '%OUT%\Restitutor_Cheats_Exp.dll','%GAME%\Mods\Cheats\Restitutor_Cheats_Exp.dll','%GAME%\Mods\Cheats\Restitutor_Cheats_Interface.dll' -Algorithm SHA256 | Format-List" >> "%LOG%"
:done
echo exit=%RC% >> "%LOG%"
type "%LOG%"
pause
