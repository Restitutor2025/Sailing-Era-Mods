# Restitutor.Core 0.1.0 (UserLibs) + Instant Entrance 0.1.1 (first mod on Core, behaviour unchanged). Run with the game closed.
# Roll back: copy the project-root Restitutor_fixes_Instant_Entrance.dll (0.1.0, 5B963FB9...) over the installed file and delete UserLibs\Restitutor.Core.dll.
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
if(Get-Process SailingEra -EA SilentlyContinue){throw '게임을 먼저 종료하세요'}
$jobs=@(
 @{src="$p\Core\releases\0.1.0\Restitutor.Core.dll"; dst="$g\UserLibs\Restitutor.Core.dll"; want='928E5F54948C12F769085780447F7E4647B14C86BA076EA0ABEEA7AC55E28027'},
 @{src="$p\InstantEntrance\releases\0.1.1\Restitutor_fixes_Instant_Entrance.dll"; dst="$g\Mods\Additional_Functions\Restitutor_Additional_Entrance.dll"; want='431F5B1AC88835BC479F565B22148B02BB581E31E068C4E56F14E3DE647096E1'})
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){throw "원본 해시 불일치: $($j.src)"} }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){throw "교체 후 불일치: $($j.dst)"}; "설치: $($j.dst)" }
