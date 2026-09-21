$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $PSScriptRoot 'evidence/0.6.10'
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$names=@('APPDATA','DOTNET_CLI_HOME','NUGET_PACKAGES','DOTNET_CLI_TELEMETRY_OPTOUT','DOTNET_SKIP_FIRST_TIME_EXPERIENCE')
$previous=@{}
foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
Push-Location $root
try {
 $env:APPDATA=Join-Path $PSScriptRoot 'obj/build-appdata'
 $env:DOTNET_CLI_HOME=Join-Path $PSScriptRoot 'obj/dotnet-home'
 $env:NUGET_PACKAGES=Join-Path $root '.packages'
 $env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
 $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
 dotnet restore TabCharacters/Restitutor_Additional_Tab_Characters.csproj --configfile NuGet.Config -p:NuGetAudit=false --nologo
 if($LASTEXITCODE){throw 'Mod restore failed'}
 dotnet build TabCharacters/Restitutor_Additional_Tab_Characters.csproj -c Release --no-restore --nologo
 if($LASTEXITCODE){throw 'Build failed'}
 foreach($suite in @('Tests','PopupTests')) {
  dotnet restore "TabCharacters/tests/$suite.csproj" --configfile NuGet.Config -p:NuGetAudit=false --nologo
  if($LASTEXITCODE){throw "$suite restore failed"}
  dotnet run --project "TabCharacters/tests/$suite.csproj" -c Release --no-restore --nologo | Tee-Object "$evidence/$suite-results.txt"
  if($LASTEXITCODE){throw "$suite failed"}
 }
 & "$PSScriptRoot/evidence/verify_interop.ps1" -OutputDirectory $evidence
 Copy-Item -LiteralPath "$PSScriptRoot/bin/Release/net6.0/Restitutor_Additional_Tab_Characters.dll" -Destination "$root/Restitutor_Additional_Tab_Characters.dll"
 Get-FileHash -Algorithm SHA256 -LiteralPath "$root/Restitutor_Additional_Tab_Characters.dll" | Format-List | Out-String | Set-Content "$evidence/release-hash.txt"
 Write-Output 'Verified workspace DLL. Game/Steam not launched; Mods not modified.'
} finally {
 Pop-Location
 foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name],'Process')}
}
