param([Parameter(Mandatory=$true)][string]$SourceCommit,[Parameter(Mandatory=$true)][string]$PullRequest)
$ErrorActionPreference='Stop'
$taskGame='E:\Program\steam\steamapps\common\Sailing Era'
$taskEditor='E:\Program\Sailing_Era_Editor'
$taskSource='E:\Program\Sailing_Era_Editor-worktrees\feat-romanos-dlc\core\release\Restitutor_CityEditor_Core.dll'
if(Get-Process -Name SailingEra -ErrorAction SilentlyContinue){throw '게임이 실행 중이므로 설치하지 않습니다.'}
if((Get-FileHash -LiteralPath (Join-Path $taskGame 'GameAssembly.dll')).Hash.ToLowerInvariant() -ne '50d53d17829e3e77b9786ea42d998d1ad258f0653846524069f22e5e442effca'){throw '원본 기준 불일치'}
$taskInfo=[Diagnostics.FileVersionInfo]::GetVersionInfo($taskSource)
if([version]$taskInfo.FileVersion -ne [version]'0.14.0.0'){throw '배포 버전 불일치'}
if(-not $taskInfo.ProductVersion.Contains($SourceCommit)){throw '배포 제품 버전과 게시 소스 커밋 불일치'}
$taskSha=(Get-FileHash -LiteralPath $taskSource).Hash.ToLowerInvariant()
$taskExpected='5199a1318118d79507046472a7c4c419353c06cf06b3348551efe4396865e3b2'
$taskTargets=@((Join-Path $taskGame 'Mods\Restitutor_CityEditor_Core.dll'),(Join-Path $taskEditor 'core\release\Restitutor_CityEditor_Core.dll'))
foreach($taskTarget in $taskTargets){
 $taskOld=[Diagnostics.FileVersionInfo]::GetVersionInfo($taskTarget)
 if([version]$taskOld.FileVersion -gt [version]$taskInfo.FileVersion){throw '다운그레이드 차단'}
 if((Get-FileHash -LiteralPath $taskTarget).Hash.ToLowerInvariant() -ne $taskExpected){throw ('설치 전 해시가 달라짐: '+$taskTarget)}
}
$taskBackup=Join-Path $taskGame ('UserData\CityEditor\deployment-backups\city-development-'+(Get-Date -Format yyyyMMdd-HHmmss)+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6))
[IO.Directory]::CreateDirectory($taskBackup)|Out-Null
$taskRows=@();$taskIndex=0
foreach($taskTarget in $taskTargets){
 $taskTemp=$taskTarget+'.city-development.tmp'
 if(Test-Path -LiteralPath $taskTemp){throw '예상하지 않은 임시 파일 존재'}
 [IO.File]::Copy($taskSource,$taskTemp)
 if((Get-FileHash -LiteralPath $taskTemp).Hash.ToLowerInvariant() -ne $taskSha){throw '준비 파일 검증 실패'}
 $taskSaved=Join-Path $taskBackup ('core-'+$taskIndex+'.dll')
 [IO.File]::Replace($taskTemp,$taskTarget,$taskSaved)
 if((Get-FileHash -LiteralPath $taskTarget).Hash.ToLowerInvariant() -ne $taskSha){throw '설치 후 검증 실패'}
 $taskRows+=@{target=$taskTarget;before=$taskExpected;after=$taskSha;backup=$taskSaved}
 $taskIndex++
}
$taskReceipt=@{version=$taskInfo.FileVersion;productVersion=$taskInfo.ProductVersion;sourceCommit=$SourceCommit;pullRequest=$PullRequest;source=$taskSource;sha256=$taskSha;targets=$taskRows;installedAt=(Get-Date -Format o);validation='offline tests/build verified; game/Steam not launched; gameplay unverified'}
$taskReceipt|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $taskBackup 'receipt.json') -Encoding utf8
$taskReceipt|ConvertTo-Json -Depth 8
