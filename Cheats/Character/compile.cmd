@echo off
rem Compile + checks only (no install). Safe while the game is running.
setlocal
set "ROOT=%~dp0..\.."
for %%I in ("%ROOT%") do set "ROOT=%%~fI"
set "APPDATA=%ROOT%\Contribution\build-profile"
set "NUGET_PACKAGES=%ROOT%\.packages"
set "LOG=%~dp0compile.log"
echo [%date% %time%] compile start > "%LOG%"
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
:done
echo exit=%RC% >> "%LOG%"
