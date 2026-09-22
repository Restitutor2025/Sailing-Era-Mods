# Builds Restitutor_Rebalance_Growth.dll, runs rule checks, installs to Mods\Rebalance.
# Close the game first. Usage: powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference='Stop'
$here=$PSScriptRoot; $root=Split-Path $here -Parent
$game='E:\Program\steam\steamapps\common\Sailing Era'
$version='0.3.0'
$env:APPDATA="$root\Contribution\build-profile"; $env:NUGET_PACKAGES="$root\.packages"
if(Get-Process SailingEra -ErrorAction SilentlyContinue){ Write-Host 'Game is running. Close it and run again.'; exit 1 }
dotnet build "$here\Restitutor_Rebalance_Growth.csproj" -c Release -p:NuGetAudit=false
if($LASTEXITCODE -ne 0){ Write-Host "build failed ($LASTEXITCODE)"; exit 1 }
dotnet run --project "$here\tests\Tests.csproj" -c Release
if($LASTEXITCODE -ne 0){ Write-Host "tests failed ($LASTEXITCODE)"; exit 1 }
$dll="$here\bin\Release\net6.0\Restitutor_Rebalance_Growth.dll"
$out="$here\releases\$version"; New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item $dll $out -Force
$dest="$game\Mods\Rebalance"; New-Item -ItemType Directory -Force $dest | Out-Null
if(-not (Test-Path "$dest\manifest.json")){ [IO.File]::WriteAllText("$dest\manifest.json","{}`n") }
Copy-Item $dll $dest -Force
Get-FileHash "$out\Restitutor_Rebalance_Growth.dll","$dest\Restitutor_Rebalance_Growth.dll" -Algorithm SHA256 | Format-List
