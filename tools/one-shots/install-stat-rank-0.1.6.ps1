# Stat Rank 0.1.6 (on Restitutor.Core, behaviour unchanged). Run with the game closed. Needs UserLibs\Restitutor.Core.dll (installed 2026-09-22).
# Roll back: copy StatRank\releases\0.1.5\Restitutor_Additional_Stat_Rank.dll (25DFD9D0...) over the installed file.
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $g='E:\Program\steam\steamapps\common\Sailing Era'
if(Get-Process SailingEra -EA SilentlyContinue){throw '게임을 먼저 종료하세요'}
if((Get-FileHash "$g\UserLibs\Restitutor.Core.dll").Hash -ne '928E5F54948C12F769085780447F7E4647B14C86BA076EA0ABEEA7AC55E28027'){throw 'UserLibs\Restitutor.Core.dll 0.1.0 이 없거나 다릅니다'}
$src="$p\StatRank\releases\0.1.6\Restitutor_Additional_Stat_Rank.dll"; $dst="$g\Mods\Additional_Functions\Restitutor_Additional_Stat_Rank.dll"; $want='1F418BC954030712ED670CB81B8F15E15135C0CFEF5E323BB4EA000F833A830E'
if((Get-FileHash $src).Hash -ne $want){throw "원본 해시 불일치: $src"}
Copy-Item $src $dst -Force; if((Get-FileHash $dst).Hash -ne $want){throw "교체 후 불일치: $dst"}; "설치: $dst"
