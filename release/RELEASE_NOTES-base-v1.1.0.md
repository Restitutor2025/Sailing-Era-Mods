# Restitutor Base v1.1.0

Sailing Era(Steam) + MelonLoader 0.7.3 용 기본 모드 묶음. **필수 묶음**이다. Rebalance·Cheats 는 이 묶음 위에 설치한다.
모든 모드는 GameAssembly.dll SHA256 `50D53D17…` 판에서 만들고 게임에서 테스트했다.

이전: v1.0.1(Google Drive, Base 만).

## 설치
1. 게임을 끈다.
2. zip 을 게임 폴더(`SailingEra.exe` 가 있는 `Sailing Era` 폴더)에 압축 해제한다. zip 안의 `UserLibs\`, `Mods\...` 경로가 그대로 들어간다. 같은 이름 파일은 덮어쓴다.
3. 파일 확인은 zip 안 `FILES-SHA256.txt` 로 한다.

### manifest.json
MelonLoader 0.7.2 부터 `Mods` 하위 폴더는 `manifest.json` 이 있어야 읽힌다. 이 zip 은 `Mods\Additional_Functions\`, `Mods\Bug_Fixes\`, `Mods\Rebalance\` 에 각각 넣어 둔다(내용 `{}`).

## 구성
| 모드 | 버전 | 설치 위치 | v1.0.1 대비 |
|---|---|---|---|
| Restitutor.Core | 0.2.0 | `UserLibs\Restitutor.Core.dll` | 같음 |
| Contribution | 0.5.7 | `Mods\Additional_Functions\Restitutor_Additional_Contribution_Goods_Recognition.dll` | 같음 |
| Instant Entrance | 0.1.1 | `Mods\Additional_Functions\Restitutor_Additional_Entrance.dll` | 같음 |
| Intro Skip | **0.1.4** | `Mods\Additional_Functions\Restitutor_Additional_IntroSkip.dll` | 0.1.3 → 0.1.4 |
| Item Rebuild | 0.1.23 | `Mods\Additional_Functions\Restitutor_Additional_Item_Rebuild.dll` | 같음 |
| Stat Rank | **0.2.2** | `Mods\Additional_Functions\Restitutor_Additional_Stat_Rank.dll` | 0.1.6 → 0.2.2 |
| Tab Characters | 0.6.12 | `Mods\Additional_Functions\Restitutor_Additional_Tab_Characters.dll` | 같음 |
| Text Speed | 0.1.6 | `Mods\Additional_Functions\Restitutor_Additional_Textspeed.dll` | 같음 |
| CTRL Instant | 0.1.7 | `Mods\Bug_Fixes\Restitutor_BugFixes_CTRL_Instant.dll` | 같음 |
| Fleet Info | 0.1.0 | `Mods\Bug_Fixes\Restitutor_fixes_Fleet_Info.dll` | 같음 |
| Map | **0.2.6** | `Mods\Bug_Fixes\Restitutor_fixes_map.dll` | 0.2.4 → 0.2.6 |
| SaveSlots | **0.2.5** | `Mods\Rebalance\Restitutor_Rebalance_SaveSlots.dll` | 새로 추가 |

- Intro Skip 0.1.4: 파일 버전(FileVersion)을 0.1.4 로 맞춰 다시 빌드. 코드는 게임에서 테스트한 0.1.4 와 같다(버전 값 외 차이 없음 확인).
- SaveSlots: 세이브 슬롯 10 → 101(자동 2 + 수동 99), 페이지 이동 Q/E. 파일 이름·위치는 기존 설치본과 같게 `Mods\Rebalance\` 에 둔다(다른 폴더에 두면 기존 설치본과 두 번 로드됨).

## 연동 관계
- Fleet Info 를 뺀 모든 모드는 Restitutor.Core 0.1.0 이상이 필요하다(없으면 로그 한 줄 남기고 그 모드만 꺼짐).
- Stat Rank 는 단독으로 동작한다. Rebalance 의 Growth 가 동작하면 등급 팁에 '레벨당 %·누적 %', Devil Fruits 가 있으면 열매 안내 줄이 추가된다(없어도 오류 없음).
- GameAssembly 해시가 `50D53D17…` 이 아니면 꺼지는 모드: Stat Rank, Item Rebuild, Tab Characters, Fleet Info.

## 알려진 제한
- Map 0.2.6: 항로 계획 지도에서 항구에 마우스를 올리거나 지도를 끄는 동안 빨간 항구 표시가 잠시 사라진다. 경유지를 바꿀 때마다 판정에 약 60 ms.
- SaveSlots 0.2.5: 진단 로그 줄(`[diag] nav`)이 남아 있다. 동작에는 영향 없다.

## 제거
해당 DLL 을 지운다. `UserLibs\Restitutor.Core.dll` 을 지우면 Fleet Info 를 뺀 모든 Restitutor 모드가 동작하지 않는다. SaveSlots 를 지우면 11번째 이후 슬롯은 화면에 안 보인다(파일은 남음).
