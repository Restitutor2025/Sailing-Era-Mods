$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$log = Join-Path $PSScriptRoot 'build.log'
$env:APPDATA = Join-Path $PSScriptRoot 'obj\build-appdata'
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot 'obj\dotnet-home'
$env:NUGET_PACKAGES = Join-Path $root '.packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
"== restore $(Get-Date -Format s)" | Out-File $log -Encoding utf8
dotnet restore "$PSScriptRoot\Restitutor_Analytics_SailTrace.csproj" --configfile "$root\NuGet.Config" -p:NuGetAudit=false --nologo *>> $log
if ($LASTEXITCODE -ne 0) { "RESTORE FAILED" | Out-File $log -Append -Encoding utf8; throw 'Restore failed' }
"== build" | Out-File $log -Append -Encoding utf8
dotnet build "$PSScriptRoot\Restitutor_Analytics_SailTrace.csproj" -c Release --no-restore --nologo *>> $log
if ($LASTEXITCODE -ne 0) { "BUILD FAILED" | Out-File $log -Append -Encoding utf8; throw 'Build failed' }
$outputDir = Join-Path $root 'Analytics'
New-Item -ItemType Directory -Force $outputDir | Out-Null
Copy-Item -LiteralPath "$PSScriptRoot\bin\Release\net6.0\Restitutor_Analytics_SailTrace.dll" -Destination $outputDir -Force
$h = Get-FileHash "$outputDir\Restitutor_Analytics_SailTrace.dll" -Algorithm SHA256
"== OK $($h.Hash) $outputDir\Restitutor_Analytics_SailTrace.dll" | Out-File $log -Append -Encoding utf8
$h
