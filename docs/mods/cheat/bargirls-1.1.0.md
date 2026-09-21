# Cheats Bargirls 1.1.0 — 여급별 퀘스트 보호와 상승 버튼

2026-09-19. 사용자의 변경 요청에 따라 `여급 호감도` 제목과 `상승` 버튼 하나로 교체했다. 클릭마다 다음 하트 완성 단계(현재 기준 50/250/650)로 상승한다. 3단계에서는 비활성화한다. 하락 입력과 임의 단계 선택은 없다.

여급 없는 도시/술집 밖의 취소선 및 비활성 정책 유지. 술집/현재 여급 화면에서만 사용하며 플레이어·도시·여급 일치를 적용 직전 재확인한다. 현재 단계, 비활성 이유를 표시한다.

## 퀘스트 보호

현재 여급의 RoleId와 같은 PortFacilityNPC 항목에서 topicId를 읽고 topicType=8 과제만 검사한다. 시작 대화가 완료되었거나 현재 실행 중이고 최종 완료 대화가 미완료이면 막는다. 목표 달성 후 완료 보고 전도 차단한다. 다른 여급의 항목은 검사 대상에서 제외한다. 일반 DoingTasks에 현재 여급 roleId를 직접 지정한 임무가 있으면 추가로 차단한다.

과제 진행 수치 CompleteTask 쓰기를 **제거**했다. 다음 하트가 원본 과제 상한을 초과하면 `이 여급의 단계 해금 과제 완료 필요`로 막는다. 아직 퀘스트를 수락하지 않았더라도 이 제한에 해당하면 먼저 해당 여급 과제를 진행해야 한다. 과제 완료 후 다음 단계 상승이 가능하다. 1.0.0에서 이미 저장한 과제 수치를 되돌리는 마이그레이션은 하지 않는다.

정상 적용은 `PlayerSocialDB.UpdateFavorability(role, target-current)` 한 번이다. 양수 증가량만 허용하며 원본 dirty 경로를 사용한다. 미등록 관계는 적용할 때만 AddSocialRole로 생성한다. Favorability/CompleteTask/보상/선물/대화 완료 플래그 직접 setter가 없다. 새 Harmony/네이티브 패치 0개, Interface 1.1.1 의존 유지.

## 확인된 근거

- [선택 원본 본문과 설치 SHA256](../../../Cheats/evidence/bargirls-1.1.0/native.json): 기존 설치 기준 `50D53D17829E3E77B9786EA42D998D1AD258F0653846524069F22E5E442EFFCA`와 일치. [추출기](../../../Cheats/evidence/bargirls-1.1-native.py).
- `PlayerInquireClueDB.UpdataFacilityRole` RVA 0x46CBD0: 0x46E624에서 Topic.finalEndTalkpart를 PlayerTalk.DoneTalkParts에 조회한다. 완료 시 0x46E776에서 RoleInFacility.TaskStatus를 0으로 정리한다. 미완료면 endCondition이 달성될 때 상태 3/보고 대화, 그렇지 않으면 0x46E71B에서 openTalkpart 완료 여부를 검사하여 상태 2/대기 대화를 선택한다. 이 본문에 맞춰 시작 완료~최종 대화 완료 구간을 보호한다.
- `SelectTaskTopic` 0x46C080은 지정 PortFacilityNPC.topicId 배열로 Topic을 얻는다. 일반 Task.roleId만으로 원본 여급 과제를 식별할 수 없음을 현재 table에서 확인했다.
- [여급별 과제 연결](../../../Cheats/evidence/bargirls-1.1.0/quest-topics.json): 설치 table 사본에서 여급 20명의 기본 과제 64개 모두 시작/최종 대화 ID를 확인했다. 추가 NPC 항목도 같은 roleId로 묶었다. table은 현재 실행 상태가 아닌 정적 설정이다.
- `InnerUpdateFavorability` 0x682020는 CompleteTask 0/1에서 각각 50/250 상한을 적용한다. 과제를 수정하지 않으면서 한 단계의 정확한 상승을 보장하기 위해 적용 전에 다음 목표가 이 상한 안인지 검사한다. `UpdateFavorability` 0x681C80의 dirty 호출도 재대조했다.

## 검증·배포와 한계

DLL 빌드 0 경고/0 오류. 순수 정책 검사 135개(단계 경계, 양수 상승, 최대 단계, A/B 여급 구분, 시작/완료 조합), Host 수명주기 검사 61개 통과. IL 감사에서 원본 증가 호출의 소유권, 직접 수치/과제/보상 setter 부재를 확인했다. 과제 연결 정적 검사 20명/64개 통과. 실제 IL2CPP 객체, 화면 렌더, 이벤트 발동, 저장 재로드의 실행 결과를 뜻하지 않는다.

루트 `Restitutor_Cheats_Bargirls.dll` 및 `Cheats/releases/bargirls-1.1.0/`에 배포. 1.0.0 DLL/문서는 보존하며 변경 전 소스도 해당 release/source에 보존했다. 게임 설치 폴더는 수정하지 않았다. 게임/Steam 실행·조작 없음.

사용자 실전 확인: 술집 밖/여급 없는 도시 취소선, 0→1 및 퀘스트 정상 완료 후 1→2→3, 3단계 비활성, A 퀘스트 중 A 차단/B 허용, 목표 달성 후 보고 전 차단/보고 후 재활성, 저장 재로드. 다른 모드가 원본 함수 또는 대화 기록을 바꾸는 경우 병용 결과는 미확인이다.
