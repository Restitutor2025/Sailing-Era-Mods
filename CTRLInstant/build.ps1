$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$env:APPDATA = Join-Path $PSScriptRoot 'obj\build-appdata'
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot 'obj\dotnet-home'
$env:NUGET_PACKAGES = Join-Path $root '.packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
dotnet restore "$PSScriptRoot\Restitutor_BugFixes_CTRL_Instant.csproj" --configfile "$root\NuGet.Config" -p:NuGetAudit=false --nologo
if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
dotnet build "$PSScriptRoot\Restitutor_BugFixes_CTRL_Instant.csproj" -c Release --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
dotnet restore "$PSScriptRoot\tests\Tests.csproj" --configfile "$root\NuGet.Config" -p:NuGetAudit=false --nologo
if ($LASTEXITCODE -ne 0) { throw 'Test restore failed' }
dotnet run --project "$PSScriptRoot\tests\Tests.csproj" -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
python "$PSScriptRoot\evidence\extract.py"
if ($LASTEXITCODE -ne 0) { throw 'Native verification failed' }
& "$PSScriptRoot\evidence\verify_interop.ps1"
$outputDir = Join-Path $root 'Bug_Fixes'
New-Item -ItemType Directory -Force $outputDir | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot\bin\Release\net6.0\Restitutor_BugFixes_CTRL_Instant.dll" -Destination $outputDir
Get-FileHash "$outputDir\Restitutor_BugFixes_CTRL_Instant.dll" -Algorithm SHA256
