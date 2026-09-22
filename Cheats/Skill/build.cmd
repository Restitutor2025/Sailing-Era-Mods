@echo off
rem Builds Restitutor_Cheats_Skill.dll with the project's isolated NuGet profile (Cheats/README.md).
setlocal
set "ROOT=%~dp0..\.."
for %%I in ("%ROOT%") do set "ROOT=%%~fI"
set "APPDATA=%ROOT%\Contribution\build-profile"
set "NUGET_PACKAGES=%ROOT%\.packages"
set "LOG=%~dp0build.log"
echo [%date% %time%] build start > "%LOG%"
dotnet build "%~dp0Restitutor_Cheats_Skill.csproj" -c Release -p:NuGetAudit=false >> "%LOG%" 2>&1
set "RC=%ERRORLEVEL%"
echo exit=%RC% >> "%LOG%"
if not "%RC%"=="0" goto done
set "OUT=%ROOT%\Cheats\releases\skill-1.1.1"
if not exist "%OUT%" mkdir "%OUT%"
copy /Y "%~dp0bin\Release\net6.0\Restitutor_Cheats_Skill.dll" "%OUT%\" >> "%LOG%"
powershell -NoProfile -Command "Get-FileHash '%OUT%\Restitutor_Cheats_Skill.dll' -Algorithm SHA256 | Format-List" >> "%LOG%"
:done
type "%LOG%"
pause
