# Restitutor mods v1.0.0

Sailing Era(Steam) + MelonLoader 0.7.3 용 모드 묶음. 모든 모드는 GameAssembly.dll 기준 해시 50D53D17… 판에서 만들고 확인했다.

## 설치
1. 게임을 끈다.
2. 원하는 zip 을 게임 폴더(`Sailing Era`, `SailingEra.exe` 가 있는 곳)에 압축 해제한다. zip 안의 `UserLibs\`, `Mods\...` 경로 그대로 들어간다(같은 이름 파일은 덮어쓰기).
3. **Base 는 필수**(Restitutor.Core 포함). Rebalance 와 Cheats 는 Base 위에 선택 설치.
4. 파일 확인: `SHA256SUMS.txt`.

## 묶음
### Restitutor-Base-v1.0.0.zip — 공용 라이브러리 Restitutor.Core 와 편의·버그 수정 모드
| 모드 | 버전 | 설치 위치 |
|---|---|---|
| Restitutor.Core | 0.2.0 | `UserLibs\Restitutor.Core.dll` |
| Contribution | 0.5.7 | `Mods\Additional_Functions\Restitutor_Additional_Contribution_Goods_Recognition.dll` |
| Instant Entrance | 0.1.1 | `Mods\Additional_Functions\Restitutor_Additional_Entrance.dll` |
| Intro Skip | 0.1.3 | `Mods\Additional_Functions\Restitutor_Additional_IntroSkip.dll` |
| Item Rebuild | 0.1.23 | `Mods\Additional_Functions\Restitutor_Additional_Item_Rebuild.dll` |
| Stat Rank | 0.1.6 | `Mods\Additional_Functions\Restitutor_Additional_Stat_Rank.dll` |
| Tab Characters | 0.6.12 | `Mods\Additional_Functions\Restitutor_Additional_Tab_Characters.dll` |
| Text Speed | 0.1.6 | `Mods\Additional_Functions\Restitutor_Additional_Textspeed.dll` |
| CTRL Instant | 0.1.7 | `Mods\Bug_Fixes\Restitutor_BugFixes_CTRL_Instant.dll` |
| Fleet Info | 0.1.0 | `Mods\Bug_Fixes\Restitutor_fixes_Fleet_Info.dll` |
| Map | 0.2.4 | `Mods\Bug_Fixes\Restitutor_fixes_map.dll` |

### Restitutor-Rebalance-v1.0.0.zip — 게임 규칙을 바꾸는 모드(선택). Devil Fruits 는 Base 의 Stat Rank 등급 글자를 사용
| 모드 | 버전 | 설치 위치 |
|---|---|---|
| Devil Fruits | 0.1.3 | `Mods\Rebalance\Restitutor_devil_fruits.dll` |

### Restitutor-Cheats-v1.0.0.zip — 치트 창과 기능(선택). 창 O = 전체 끄기/켜기, H = 접기
| 모드 | 버전 | 설치 위치 |
|---|---|---|
| Cheats Interface | 1.6.0 | `Mods\Cheats\Restitutor_Cheats_Interface.dll` |
| Cheats Skill | 1.0.5 | `Mods\Cheats\Restitutor_Cheats_Skill.dll` |
| Cheats Speed | 1.1.2 | `Mods\Cheats\Restitutor_Cheats_Speed.dll` |
| Cheats Battle | 1.0.1 | `Mods\Cheats\Restitutor_Cheats_Battle.dll` |
| Cheats Bargirls | 1.1.3 | `Mods\Cheats\Restitutor_Cheats_Bargirls.dll` |
| Cheats Contribution | 1.1.2 | `Mods\Cheats\Restitutor_Cheats_Contribution.dll` |
| Cheats Exp | 1.0.1 | `Mods\Cheats\Restitutor_Cheats_Exp.dll` |
| Cheats Money | 1.0.1 | `Mods\Cheats\Restitutor_Cheats_Money.dll` |
| Cheats Character | 1.1.1 | `Mods\Cheats\Restitutor_Cheats_Character.dll` |
| Cheats Items | 0.1.0 | `Mods\Cheats\Restitutor_Items.dll` |

## 제거
해당 DLL 을 지운다. Base 를 지우면 Rebalance·Cheats 도 동작하지 않는다(모두 Restitutor.Core 필요, 없으면 로그에 한 줄 남기고 그 모드만 꺼짐).
