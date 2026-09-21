# 2026-09-22 resource cleanup install (functions unchanged) + HookCensus diagnostic. Run with the game closed.
# No backups are kept in Mods (MelonLoader would ignore them, but the user asked for no backups); release copies stay in the project folder.
$p='E:\Documents\ChatGPT\Sailing_era_Restitutor'; $m='E:\Program\steam\steamapps\common\Sailing Era\Mods'
if(Get-Process SailingEra -EA SilentlyContinue){throw '게임을 먼저 종료하세요'}
$jobs=@(
 @{src="$p\Map\candidates\tab-only\dist\0.2.3\Restitutor_fixes_map.dll"; dst="$m\Bug_Fixes\Restitutor_fixes_map.dll"; want='BAC59302779B02B725BC2E5B9D48FE2D146F523A4CA2526627F7BEB0D43BC9EC'},
 @{src="$p\TabCharacters\evidence\0.6.11\Restitutor_Additional_Tab_Characters.dll"; dst="$m\Additional_Functions\Restitutor_Additional_Tab_Characters.dll"; want='0BDAF1ED31B09B52B5806257336C7EE58062EE36389E83D608608526E8250F93'},
 @{src="$p\TextSpeed\releases\0.1.5\Restitutor_fixes_textspeed.dll"; dst="$m\Additional_Functions\Restitutor_Additional_Textspeed.dll"; want='8488CBAAB0D673D8DBDFBF1FD7815DDCC4F25A832DC112E2B03ABBB8BD7294B2'},
 @{src="$p\Contribution\releases\0.5.5\Restitutor_fixes_Contribution.dll"; dst="$m\Additional_Functions\Restitutor_Additional_Contribution_Goods_Recognition.dll"; want='206D8279D9BBD4F1B0F9F4AEE7F8AE7E058706E1A5934EEA7CAC8C944FC2516C'},
 @{src="$p\HookCensus\release\0.1.0\Restitutor_Analytics_HookCensus.dll"; dst="$m\Analytics\Restitutor_Analytics_HookCensus.dll"; want='077791DED7D57030BBA56FAAF3E6AE788F0E9CC63A21EE38378B20A135A52EE7'},
 @{src="$p\Cheats\releases\1.5.1\Restitutor_Cheats_Interface.dll"; dst="$m\Cheats\Restitutor_Cheats_Interface.dll"; want='5906A9A9225F68C13CB0CEB3CDC0567981B4D47EA3810238A33CF00BCA0B5D69'},
 @{src="$p\Cheats\releases\1.5.1\Restitutor_Cheats_Speed.dll"; dst="$m\Cheats\Restitutor_Cheats_Speed.dll"; want='32F6785A5418D7C078B9B239E32B498CCC82429C08DDE03CB66E1CE591716E22'},
 @{src="$p\Cheats\releases\1.5.1\Restitutor_Cheats_Battle.dll"; dst="$m\Cheats\Restitutor_Cheats_Battle.dll"; want='90125AA2253C033392BD63E92F8C90C80082D1D1F0483DB08CE00882CB8E344C'},
 @{src="$p\Cheats\releases\1.5.1\Restitutor_Cheats_Bargirls.dll"; dst="$m\Cheats\Restitutor_Cheats_Bargirls.dll"; want='1980B122A34D85A5F4F95CBEF019814895DDFD317CF3695F5FC180C790C31A04'},
 @{src="$p\Cheats\releases\1.5.1\Restitutor_Cheats_Contribution.dll"; dst="$m\Cheats\Restitutor_Cheats_Contribution.dll"; want='7CB78E5362C3A46ACAF79EA0A99338CEE175FAA08FD7BCC177A02A29C7B121FB'},
 @{src="$p\Cheats\releases\1.5.1\Restitutor_Cheats_Exp.dll"; dst="$m\Cheats\Restitutor_Cheats_Exp.dll"; want='D6CC769A875DC9F23284CCE55399CCFD43C0BA631267E72B04C89F4D9C98F8C5'},
 @{src="$p\Cheats\releases\1.5.1\Restitutor_Cheats_Money.dll"; dst="$m\Cheats\Restitutor_Cheats_Money.dll"; want='92CD5ACEE595488FE786C24BE0AB460A8B065714AD8D61C1B6CC7B923830B053'},
 @{src="$p\Cheats\releases\1.5.1\Restitutor_Cheats_Character.dll"; dst="$m\Cheats\Restitutor_Cheats_Character.dll"; want='CB1175B8DD1C4F5B2279BEF368018430FA2F61B7CA1B658BA5697E790B7E4A64'},
 @{src="$p\Cheats\releases\1.5.1\Restitutor_Cheats_Skill.dll"; dst="$m\Cheats\Restitutor_Cheats_Skill.dll"; want='745DBB192574A32F42840DA0C390A2A86B6E29A905C990C4758D1ADF0B581680'})
foreach($j in $jobs){ if((Get-FileHash $j.src).Hash -ne $j.want){throw "원본 해시 불일치: $($j.src)"} }
foreach($j in $jobs){ Copy-Item $j.src $j.dst -Force; if((Get-FileHash $j.dst).Hash -ne $j.want){throw "교체 후 불일치: $($j.dst)"}; "설치: $(Split-Path $j.dst -Leaf)" }
$probe="$m\Analytics\Restitutor_Analytics_HarbourProbe.dll"; if(Test-Path $probe){ Remove-Item $probe; '삭제: Restitutor_Analytics_HarbourProbe.dll' }
