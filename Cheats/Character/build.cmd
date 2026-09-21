@echo off
rem Builds Interface 1.4.0 + Character 1.1.0 (Interface via project reference), runs character and
rem host lifecycle checks, then installs BOTH DLLs to Mods\Cheats. Close the game before running.
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
dotnet build "%~dp0Restitutor_Cheats_Character.csproj" -c Release -p:NuGetAudit=false >> "%LOG%" 2>&1
set "RC=%ERRORLEVEL%"
echo build exit=%RC% >> "%LOG%"
if not "%RC%"=="0" goto done
dotnet run --project "%ROOT%\Cheats\character-tests\Tests.csproj" -c Release >> "%LOG%" 2>&1
set "RC=%ERRORLEVEL%"
echo character tests exit=%RC% >> "%LOG%"
if not "%RC%"=="0" goto done
dotnet run --project "%ROOT%\Cheats\lifecycle-tests\Tests.csproj" -c Release >> "%LOG%" 2>&1
set "RC=%ERRORLEVEL%"
echo lifecycle tests exit=%RC% >> "%LOG%"
if not "%RC%"=="0" goto done
set "OUT=%ROOT%\Cheats\releases\character-1.1.0"
if not exist "%OUT%" mkdir "%OUT%"
copy /Y "%~dp0bin\Release\net6.0\Restitutor_Cheats_Character.dll" "%OUT%\" >> "%LOG%"
copy /Y "%ROOT%\Cheats\Interface\bin\Release\net6.0\Restitutor_Cheats_Interface.dll" "%OUT%\" >> "%LOG%"
copy /Y "%OUT%\Restitutor_Cheats_Character.dll" "%GAME%\Mods\Cheats\" >> "%LOG%"
copy /Y "%OUT%\Restitutor_Cheats_Interface.dll" "%GAME%\Mods\Cheats\" >> "%LOG%"
powershell -NoProfile -Command "Get-FileHash '%OUT%\*.dll','%GAME%\Mods\Cheats\Restitutor_Cheats_Character.dll','%GAME%\Mods\Cheats\Restitutor_Cheats_Interface.dll' -Algorithm SHA256 | Format-List" >> "%LOG%"
:done
echo exit=%RC% >> "%LOG%"
type "%LOG%"
