# release-v1.1.0-drafts: Base v1.1.0 zip + GitHub 초안(draft) 릴리스 1개. (Rebalance·Cheats·Scenario 는 아직 안 냄 — 사용자 2026-09-25)
# 1) 원본 해시 확인 2) Intro 0.1.4·Cheats Speed 1.2.0 파일 버전 맞춘 DLL 게임 설치 3) zip 4) 브랜치 커밋·push·PR 5) gh 초안 릴리스.
# 게임을 끈 상태에서 실행. 다시 실행해도 안전(같은 태그 초안이 있으면 파일·노트만 갱신).
# 되돌리기: Intro = archive-intro\0.1.4\Restitutor_Additional_IntroSkip.dll, Speed = Cheats\releases\speed-1.2.0\Restitutor_Cheats_Speed.dll 을 게임에 덮어쓰기.
$ErrorActionPreference = 'Continue'
$p   = 'E:\Documents\ChatGPT\Sailing_era_Restitutor'
$g   = 'E:\Program\steam\steamapps\common\Sailing Era'
$ud  = "$g\UserData\CityEditor"
$repo = 'Restitutor2025/Sailing-Era-Mods'
$branch = 'chore/release-v1.1.0'
$out = "$p\release\v1.1.0"
function Fail($m){ Write-Host "FAILED: $m" -ForegroundColor Red; Read-Host '엔터를 누르면 닫힙니다'; exit 1 }
function Retry([scriptblock]$b, [string]$what){ for($i=1;$i -le 5;$i++){ & $b; if($LASTEXITCODE -eq 0){ return }; Write-Host "$what 실패 ($i/5), 3초 뒤 재시도"; Start-Sleep 3 }; Fail $what }
function Sha($f){ (Get-FileHash -Algorithm SHA256 -LiteralPath $f).Hash }

if(Get-Process SailingEra -EA SilentlyContinue){ Fail '게임을 먼저 종료하세요' }
Set-Location $p
if(Test-Path '.git\index.lock'){ Fail '.git\index.lock 이 있습니다. 다른 git 이 없는지 확인 후 지우고 다시 실행' }
$cfg = Get-Content -Raw -Encoding UTF8 'release\v1.1.0-files.json' | ConvertFrom-Json

# ---- gh 준비 ----
if(-not (Get-Command gh -EA SilentlyContinue)){
  Write-Host 'GitHub CLI(gh) 설치 중 (winget)...' -ForegroundColor Cyan
  winget install --id GitHub.cli -e --accept-source-agreements --accept-package-agreements
  $env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')
  if(-not (Get-Command gh -EA SilentlyContinue)){ Fail 'gh 설치 실패. https://cli.github.com 에서 설치 후 다시 실행' }
}
gh auth status -h github.com 2>$null | Out-Null
if($LASTEXITCODE -ne 0){ Write-Host 'GitHub 로그인이 필요합니다. 브라우저에서 승인하세요.' -ForegroundColor Yellow; gh auth login -h github.com -p https -w; if($LASTEXITCODE -ne 0){ Fail 'gh auth login' } }

# ---- 1/5 원본 해시 ----
Write-Host '== 1/5 원본 해시 확인' -ForegroundColor Cyan
foreach($b in $cfg.bundles.PSObject.Properties){ foreach($d in $b.Value.dlls){ $s = Join-Path $p $d[0]; if(-not (Test-Path -LiteralPath $s)){ Fail "없음: $s" }; if((Sha $s) -ne $d[2]){ Fail "원본 해시 불일치: $s" } } }
'원본 OK'

# ---- 2/5 파일 버전 맞춘 두 DLL 게임 설치 ----
Write-Host '== 2/5 Intro 0.1.4 · Cheats Speed 1.2.0 (FileVersion 수정본) 설치' -ForegroundColor Cyan
$inst = @(
 @{src="$p\releases\intro\0.1.4\Restitutor_fixes.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_IntroSkip.dll"; old='1043F4B0074C574DF7CDA617CD0705F8D14A9F0451D08CE1C60284D2F0B73259'},
 @{src="$p\Cheats\releases\speed-1.2.0-fv\Restitutor_Cheats_Speed.dll"; dst="$g\Mods\Cheats\Restitutor_Cheats_Speed.dll"; old='D1EF6B0D037D03CBB5E20589E5369569FB3F0626248D3677E520107B97E9D1F6'})
foreach($j in $inst){
  $want = Sha $j.src; $now = Sha $j.dst
  if($now -eq $want){ "이미 설치됨: $(Split-Path $j.dst -Leaf)"; continue }
  if($now -ne $j.old){ Fail "게임 설치본이 예상과 다름(다운그레이드 방지로 중단): $($j.dst)" }
  Copy-Item -LiteralPath $j.src -Destination $j.dst -Force
  if((Sha $j.dst) -ne $want){ Fail "교체 후 불일치: $($j.dst)" }
  "설치: $(Split-Path $j.dst -Leaf) $($want.Substring(0,16))"
}

# ---- 3/5 zip ----
Write-Host '== 3/5 zip 만들기' -ForegroundColor Cyan
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
New-Item -ItemType Directory -Force $out | Out-Null
$sums = New-Object System.Collections.Generic.List[string]
$utf8 = New-Object System.Text.UTF8Encoding($false)
function AddBytes($zip, [string]$name, [byte[]]$bytes){ $e = $zip.CreateEntry($name.Replace('\','/'), [System.IO.Compression.CompressionLevel]::Optimal); $s = $e.Open(); $s.Write($bytes, 0, $bytes.Length); $s.Close() }
foreach($b in $cfg.bundles.PSObject.Properties){
  $k = $b.Name; $v = $b.Value; $zp = Join-Path $out $v.zip
  if(Test-Path -LiteralPath $zp){ Remove-Item -LiteralPath $zp -Force }
  $files = New-Object System.Collections.Generic.List[object]   # @(src, zipPath)
  foreach($d in $v.dlls){ $files.Add(@((Join-Path $p $d[0]), $d[1])) }
  foreach($rel in $v.data){
    $src = Join-Path $ud $rel
    if(-not (Test-Path -LiteralPath $src)){ Fail "데이터 없음: $src" }
    if((Get-Item -LiteralPath $src).PSIsContainer){
      Get-ChildItem -LiteralPath $src -Recurse -File | Sort-Object FullName | ForEach-Object { $files.Add(@($_.FullName, ('UserData\CityEditor\' + $_.FullName.Substring($ud.Length + 1)))) }
    } else { $files.Add(@($src, "UserData\CityEditor\$rel")) }
  }
  $zip = [System.IO.Compression.ZipFile]::Open($zp, 'Create')
  $list = New-Object System.Collections.Generic.List[string]
  $dirs = @{}
  foreach($f in $files){
    $bytes = [System.IO.File]::ReadAllBytes($f[0]); AddBytes $zip $f[1] $bytes
    $h = (Sha $f[0]); $list.Add("$h  $($f[1])"); $sums.Add("$k  $h  $($f[1])")
    $zn = $f[1].Replace('/','\'); $i = $zn.LastIndexOf('\')
    if($i -gt 0){ $dir = $zn.Substring(0, $i); if($dir -like 'Mods\*'){ $dirs[$dir] = 1 } }
  }
  foreach($dir in $dirs.Keys){ AddBytes $zip "$dir\manifest.json" $utf8.GetBytes("{}`n") }
  AddBytes $zip 'FILES-SHA256.txt' $utf8.GetBytes(($list -join "`r`n") + "`r`n")
  AddBytes $zip ("README_" + $v.zip.Replace('.zip','.md')) ([System.IO.File]::ReadAllBytes((Join-Path $p $v.notes)))
  $zip.Dispose()
  $zh = Sha $zp; $sums.Insert(0, "zip  $zh  $($v.zip)")
  "{0}: 파일 {1}개 + manifest {2}개 → {3} ({4})" -f $k, $files.Count, $dirs.Count, $v.zip, $zh.Substring(0,16)
}
[System.IO.File]::WriteAllText("$out\SHA256SUMS.txt", (($sums | Where-Object { $_ -like 'zip  *' }) -join "`r`n") + "`r`n", $utf8)
[System.IO.File]::WriteAllText("$p\release\v1.1.0-SHA256SUMS.txt", "# Base v1.1.0 zip·내부 파일 해시 (생성: release-v1.1.0-drafts.ps1, $(Get-Date -Format 'yyyy-MM-dd HH:mm'))`r`n" + ($sums -join "`r`n") + "`r`n", $utf8)

# ---- 4/5 커밋·push ----
Write-Host '== 4/5 브랜치 커밋·push' -ForegroundColor Cyan
Retry { git fetch -q origin } 'git fetch'
git switch -q $branch 2>$null
if($LASTEXITCODE -ne 0){ Retry { git switch -q -c $branch origin/main } 'git switch' }
function SetText($f, [string]$old, [string]$new){ $t = [System.IO.File]::ReadAllText($f); if($t.Contains($new)){ return }; if(-not $t.Contains($old)){ Fail "$f 에 '$old' 없음" }; [System.IO.File]::WriteAllText($f, $t.Replace($old, $new), $utf8) }
SetText "$p\Restitutor_fixes.csproj" '<Version>0.1.3</Version>' '<Version>0.1.4</Version>'
SetText "$p\Cheats\Speed\Restitutor_Cheats_Speed.csproj" '<Version>1.1.2</Version>' '<Version>1.2.0</Version>'
function Prepend($f, [string]$mark, [string]$text){ $t = [System.IO.File]::ReadAllText($f); if($t.Contains($mark)){ return }; [System.IO.File]::WriteAllText($f, $text + "`n`n" + $t, $utf8) }
Prepend "$p\docs\mods\intro\CURRENT.md" 'Intro 0.1.4 — FileVersion' "# Intro 0.1.4 — FileVersion 0.1.4 재빌드 (2026-09-25)`n`n0.1.4 코드 그대로, csproj Version 0.1.3 → 0.1.4 만 바꿔 다시 빌드(이전 설치본은 파일 버전이 0.1.3 으로 찍혔음). 버전 값 외 바이트 차이 없음 확인(빌드 식별값 제외). 설치: Mods\Additional_Functions\Restitutor_Additional_IntroSkip.dll SHA256 F5F53A4C1599AFA7…, 사본 releases\intro\0.1.4\. 이전 0.1.4 = archive-intro\0.1.4\ (1043F4B0…). 설치 기록·기능 설명은 [0.1.4](0.1.4.md). Base v1.1.0 에 포함."
Prepend "$p\docs\mods\cheat\CURRENT.md" 'Cheats Speed 1.2.0 — FileVersion' "# Cheats Speed 1.2.0 — FileVersion 1.2.0 재빌드 (2026-09-25)`n`n코드 그대로, csproj Version 1.1.2 → 1.2.0 만 바꿔 다시 빌드. 버전 값 외 바이트 차이 없음 확인. 설치 SHA256 F32877D04C51BC92…, 사본 Cheats\releases\speed-1.2.0-fv\. 이전 = Cheats\releases\speed-1.2.0\ (D1EF6B0D…). 다음 Cheats 릴리스에 포함 예정."
$add = @('Restitutor_fixes.csproj','Cheats/Speed/Restitutor_Cheats_Speed.csproj','docs/mods/intro/CURRENT.md','docs/mods/cheat/CURRENT.md',
  'release/v1.1.0-files.json','release/v1.1.0-SHA256SUMS.txt','release/RELEASE_NOTES-base-v1.1.0.md','tools/one-shots/release-v1.1.0-drafts.ps1')
Retry { git add -f -- @add } 'git add'
git diff --cached --quiet
if($LASTEXITCODE -ne 0){
  Retry { git commit -q -m 'chore(release): Base v1.1.0 draft, FileVersion fix for Intro 0.1.4 and Cheats Speed 1.2.0' -m 'Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>' -m 'Claude-Session: https://claude.ai/code/session_01MhLrAwRZdtVkVhzZadiEDt' } 'git commit'
}
Retry { git push -q -u origin $branch } 'git push'
$sha = (git rev-parse HEAD).Trim()
git --no-pager log --oneline origin/main..HEAD

# ---- 5/5 GitHub 초안 릴리스 ----
Write-Host '== 5/5 GitHub 초안(draft) 릴리스' -ForegroundColor Cyan
foreach($b in $cfg.bundles.PSObject.Properties){
  $v = $b.Value; $zp = Join-Path $out $v.zip; $notes = Join-Path $p $v.notes
  gh release view $v.tag -R $repo 2>$null | Out-Null
  if($LASTEXITCODE -eq 0){
    Retry { gh release upload $v.tag $zp "$out\SHA256SUMS.txt" --clobber -R $repo } "gh upload $($v.tag)"
    Retry { gh release edit $v.tag --title $v.title --notes-file $notes --draft=true -R $repo } "gh edit $($v.tag)"
    "갱신: $($v.tag)"
  } else {
    Retry { gh release create $v.tag $zp "$out\SHA256SUMS.txt" --draft --title $v.title --notes-file $notes --target $sha -R $repo } "gh create $($v.tag)"
    "초안 생성: $($v.tag)"
  }
}
$pr = "https://github.com/$repo/compare/main...$branch" + '?expand=1'
Write-Host "완료. Base 초안 릴리스(비공개) + PR 페이지를 엽니다 → PR: Create pull request → Merge / 릴리스: 확인 후 Publish" -ForegroundColor Green
Start-Process "https://github.com/$repo/releases"
Start-Process $pr
Read-Host '엔터를 누르면 닫힙니다'
