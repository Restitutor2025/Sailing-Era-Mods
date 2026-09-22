# Builds Restitutor_Additional_Stat_Rank.dll, runs rule checks, installs to Mods\Additional_Functions.
# Close the game first. Usage: powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference='Stop'
$here=$PSScriptRoot; $root=Split-Path $here -Parent
$game='E:\Program\steam\steamapps\common\Sailing Era'
$env:APPDATA="$root\Contribution\build-profile"; $env:NUGET_PACKAGES="$root\.packages"
if(Get-Process SailingEra -ErrorAction SilentlyContinue){ Write-Host 'Game is running. Close it and run again.'; exit 1 }
dotnet build "$here\Restitutor_Additional_Stat_Rank.csproj" -c Release -p:NuGetAudit=false
if($LASTEXITCODE -ne 0){ Write-Host "build failed ($LASTEXITCODE)"; exit 1 }
dotnet run --project "$here\tests\Tests.csproj" -c Release
if($LASTEXITCODE -ne 0){ Write-Host "tests failed ($LASTEXITCODE)"; exit 1 }
$dll="$here\bin\Release\net6.0\Restitutor_Additional_Stat_Rank.dll"
$out="$here\releases\0.2.0"; New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item $dll $out -Force
Copy-Item $dll "$game\Mods\Additional_Functions\" -Force
Get-FileHash "$out\Restitutor_Additional_Stat_Rank.dll","$game\Mods\Additional_Functions\Restitutor_Additional_Stat_Rank.dll" -Algorithm SHA256 | Format-List
