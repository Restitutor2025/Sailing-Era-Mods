# Fleet Info 0.1.0 패치 목록

## UIBarGirlCtrl.GetNpcsBySeaAreaId(bool against)

- 함수 RVA `0xB08DC0`, 길이 `0x962`; 메타데이터 동일 RVA 별칭 1개(자기 자신).
- 원본 Owner 조회용 사전 TryInsert 호출 전 `mov r9b,2`(RVA `0xB09171`, 바이트 `41 B1 02`)의 immediate만 `00`으로 변경. 실제 쓰기 RVA `0xB09173`.
- 설치 interop의 InsertionBehavior 상수: None=0, OverwriteExisting=1, ThrowOnExisting=2. 호출 결과는 원본이 읽지 않고 다음 항목으로 진행한다.
- 저장이나 공유 WorldShip.ShipTeams를 교체·삭제하지 않는다. 함수 내부가 만드는 기존 사전에서 처음 삽입한 Owner 대표를 유지한다. 동일 소유자별 개별 위치를 추가 제공하지 않는다.
- against 전용 분기는 이 삽입 루프를 우회한다. 원본 분기 구조 유지.
- 공통 Dictionary 메서드·UI 입력·시작/완료 콜백에 Harmony 훅을 등록하지 않는다. 해당 함수 코드의 직접 수정이므로 다른 모드가 같은 함수/호출 지점을 변경하는 경우 병용을 보장하지 않는다.
- 초기화: 디스크 SHA256 및 실행 중 전체 함수 바이트 검증 → VirtualProtect → 1바이트 쓰기 → instruction cache flush → 보호 복원. 실패 시 소유한 변경 복원을 시도하고 오류 기록.
- 해제: 실행 중 함수가 자신의 패치 결과와 완전히 같을 때만 02로 복원. 다른 변경 발견 시 덮어쓰지 않고 오류 기록. 이후 다른 모드가 덮어쓴 상태의 무충돌을 보장하지 않는다.
