# Builds Restitutor_fixes_textspeed.dll, runs store tests, installs as Mods\Additional_Functions\Restitutor_Additional_Textspeed.dll.
# Close the game first. Usage: powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference='Stop'
$here=$PSScriptRoot; $root=Split-Path $here -Parent
$game='E:\Program\steam\steamapps\common\Sailing Era'
$version='0.1.4'
$env:APPDATA="$root\Contribution\build-profile"; $env:NUGET_PACKAGES="$root\.packages"
if(Get-Process SailingEra -ErrorAction SilentlyContinue){ Write-Host 'Game is running. Close it and run again.'; exit 1 }
dotnet build "$here\Restitutor_fixes_textspeed.csproj" -c Release -p:NuGetAudit=false
if($LASTEXITCODE -ne 0){ Write-Host "build failed ($LASTEXITCODE)"; exit 1 }
dotnet run --project "$here\tests\StoreTests.csproj" -c Release
if($LASTEXITCODE -ne 0){ Write-Host "tests failed ($LASTEXITCODE)"; exit 1 }
$dll="$here\bin\Release\net6.0\Restitutor_fixes_textspeed.dll"
$out="$here\releases\$version"; New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item $dll $out -Force
Copy-Item $dll "$game\Mods\Additional_Functions\Restitutor_Additional_Textspeed.dll" -Force
Get-FileHash "$out\Restitutor_fixes_textspeed.dll","$game\Mods\Additional_Functions\Restitutor_Additional_Textspeed.dll" -Algorithm SHA256 | Format-List
