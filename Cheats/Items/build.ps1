# Builds Restitutor_Items.dll (+ Interface via project reference, not installed), runs item checks,
# installs ONLY Restitutor_Items.dll to Mods\Cheats. Close the game first.
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
$here=$PSScriptRoot; $root=Split-Path (Split-Path $here -Parent) -Parent
$game='E:\Program\steam\steamapps\common\Sailing Era'
$env:APPDATA="$root\Contribution\build-profile"; $env:NUGET_PACKAGES="$root\.packages"
if(Get-Process SailingEra -ErrorAction SilentlyContinue){ Write-Host 'Game is running. Close it and run again.'; exit 1 }
dotnet build "$here\Restitutor_Items.csproj" -c Release -p:NuGetAudit=false
if($LASTEXITCODE -ne 0){ Write-Host "build failed ($LASTEXITCODE)"; exit 1 }
dotnet run --project "$root\Cheats\items-tests\Tests.csproj" -c Release
if($LASTEXITCODE -ne 0){ Write-Host "tests failed ($LASTEXITCODE)"; exit 1 }
$dll="$here\bin\Release\net6.0\Restitutor_Items.dll"
$out="$root\Cheats\releases\items-0.1.0"; New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item $dll $out -Force
Copy-Item $dll "$game\Mods\Cheats\" -Force
Get-FileHash "$out\Restitutor_Items.dll","$game\Mods\Cheats\Restitutor_Items.dll","$game\Mods\Cheats\Restitutor_Cheats_Interface.dll" -Algorithm SHA256 | Format-List
