$ErrorActionPreference='Stop'
$game='E:\Program\steam\steamapps\common\Sailing Era'
$editor='E:\Program\Sailing_Era_Editor'
$work='E:\Program\Sailing_Era_Editor-worktrees\feat-romanos-dlc'
if (Get-Process -Name SailingEra -ErrorAction SilentlyContinue) { throw 'Game is running; no files changed' }
$sourceCore=Join-Path $work 'core\release\Restitutor_CityEditor_Core.dll'
$sourceDlc=Join-Path $work 'build\roman-dlc\bin\Release\net6.0\Restitutor_DLC_RomanRevival.dll'
$coreHash='9BADB30CC0B3E6B63C9DC0F87FFB7901601B8E794BE073E7531C9D5F2FC0BD8A'
$dlcHash='8F5BC746DD57DF3B97F2E5BD4A92441ACF67F0DB4C3D0C407FFC3860E1F8C698'
$oldHash='3743F5134F8B008879D48E8549228AFDA902B1B8862882973B0E089D25CADEB8'
$targets=@(
  @{name='game-core'; source=$sourceCore; target=(Join-Path $game 'Mods\Restitutor_CityEditor_Core.dll'); hash=$coreHash},
  @{name='editor-core'; source=$sourceCore; target=(Join-Path $editor 'core\release\Restitutor_CityEditor_Core.dll'); hash=$coreHash},
  @{name='dlc'; source=$sourceDlc; target=(Join-Path $game 'Mods\DLC\Restitutor_DLC_RomanRevival.dll'); hash=$dlcHash}
)
foreach ($t in $targets) {
  if ((Get-FileHash -LiteralPath $t.source -Algorithm SHA256).Hash -ne $t.hash) { throw 'Source hash mismatch' }
  $sv=[Version][Diagnostics.FileVersionInfo]::GetVersionInfo($t.source).FileVersion
  if (Test-Path -LiteralPath $t.target) {
    $tv=[Version][Diagnostics.FileVersionInfo]::GetVersionInfo($t.target).FileVersion
    if (-not $sv -or -not $tv -or $sv -lt $tv) { throw ('Downgrade blocked: '+$t.target) }
    $t.before=(Get-FileHash -LiteralPath $t.target -Algorithm SHA256).Hash
    if ($t.before -ne $t.hash -and ($t.name -eq 'dlc' -or $t.before -ne $oldHash)) { throw ('Unexpected installed hash: '+$t.target) }
  } else { $t.before=$null }
}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
$backup=Join-Path $game ('UserData\CityEditor\deployment-backups\roman-dlc-'+$stamp)
New-Item -ItemType Directory -Path $backup | Out-Null
foreach ($t in $targets) {
  if ($t.before) {
    $t.backup=Join-Path $backup ($t.name+'.dll')
    Copy-Item -LiteralPath $t.target -Destination $t.backup
  }
}
$written=@()
try {
  foreach ($t in $targets) {
    New-Item -ItemType Directory -Path (Split-Path $t.target) -Force | Out-Null
    Copy-Item -LiteralPath $t.source -Destination $t.target
    $written+=$t
    $t.after=(Get-FileHash -LiteralPath $t.target -Algorithm SHA256).Hash
    if ($t.after -ne $t.hash) { throw ('Installed hash mismatch: '+$t.target) }
  }
} catch {
  foreach ($t in $written) {
    if ($t.before) { Copy-Item -LiteralPath $t.backup -Destination $t.target }
    else { Remove-Item -LiteralPath $t.target }
  }
  throw
}
$receipt=@{installedAt=(Get-Date -Format o); sourceCommit='7be1949c0ebab9fdd1a573cd2f199332561434b3'; mergeCommit='e0c224dbc1710bce86bb73db43610b72752589a5'; pr='https://github.com/Restitutor2025/Sailing-Era-Editor/pull/131'; backup=$backup; files=$targets; gameTest='not run'}
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backup 'receipt.json') -Encoding utf8
$receipt | ConvertTo-Json -Depth 6
