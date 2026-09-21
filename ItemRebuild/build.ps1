$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$names = @('APPDATA','DOTNET_CLI_HOME','NUGET_PACKAGES','DOTNET_CLI_TELEMETRY_OPTOUT','DOTNET_SKIP_FIRST_TIME_EXPERIENCE')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name,'Process') }
Push-Location $root
try {
    $env:APPDATA = Join-Path $PSScriptRoot 'obj/build-appdata'
    $env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot 'obj/dotnet-home'
    $env:NUGET_PACKAGES = Join-Path $root '.packages'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    dotnet restore ItemRebuild/Restitutor_Additional_Item_Rebuild.csproj --configfile NuGet.Config -p:NuGetAudit=false --nologo
    if ($LASTEXITCODE) { throw 'Mod restore failed' }
    dotnet build ItemRebuild/Restitutor_Additional_Item_Rebuild.csproj -c Release --no-restore --nologo
    if ($LASTEXITCODE) { throw 'Mod build failed' }
    dotnet restore ItemRebuild/tests/Tests.csproj --configfile NuGet.Config -p:NuGetAudit=false --nologo
    if ($LASTEXITCODE) { throw 'Test restore failed' }
    dotnet run --project ItemRebuild/tests/Tests.csproj -c Release --no-restore | Tee-Object ItemRebuild/evidence/managed-tests.txt
    if ($LASTEXITCODE) { throw 'Managed tests failed' }
    python ItemRebuild/evidence/verify.py
    if ($LASTEXITCODE) { throw 'Native metadata verification failed' }
    & "$PSScriptRoot/evidence/verify_interop.ps1"
    $built = Join-Path $PSScriptRoot 'bin/Release/net6.0/Restitutor_Additional_Item_Rebuild.dll'
    Copy-Item -LiteralPath $built -Destination (Join-Path $root 'Restitutor_Additional_Item_Rebuild.dll')
    Get-FileHash -Algorithm SHA256 -LiteralPath $built | Format-List | Out-String | Set-Content ItemRebuild/evidence/release-hash.txt
    Write-Output 'Verified DLL copied to workspace root. Game and Steam were not launched; Mods directory was not modified.'
} finally {
    Pop-Location
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$previous[$name],'Process') }
}
