# Roman Revival 0.1.0 패치 계약

DLC 자체는 실행 코드가 없는 .NET 정의 컨테이너. 실행 후크는 에디터 저장소 Core0.13.0의 DlcApply가 소유한다.

- 대상: TemplateUtils.InitTemplateData Prefix, Priority.First. 모든 표 적재 후 파생 캐시 생성 전.
- 공유 상태: TemplateManager의 Country/Cultural/4언어 TextLib 사전 신규 행, Cargo.CulturalPrices(177행)·Cultural.CultureDis(27행) 배열 확장. 기존27칸 보존, 이탈리아 열 복제.
- 입력: Mods/DLC/*.dll CLR 리소스의 정의, GameAssembly/표 번들 SHA256, expected 배열 값. DLL 코드 실행 없음.
- 수명주기: 전체 사전 검증·변환 후 커밋, 커밋 중 예외는 역순 복구. 같은 데이터로 재호출하면 무변경. 표 재생성 시 재적용. 검증 실패는 로그/예외로 드러내며 계속 진행 복구 UI 없음.
- 접점: TableApply 및 다른 표 수정 모드, InitTemplateData의 다른 Prefix/Postfix, 신규 문화 ID를 참조한 도시 설정. 배열을 바꾼 모드는 expected 불일치로 거부될 수 있다. 실행 순서 뒤에서 바꾸는 모드는 통제하지 못한다.
- UI/입력/세이브 후크 없음. 정의 DLL 제거 시 해당 ID를 사용하는 도시·세이브의 의존성이 사라지므로 제거 전 참조 설정을 되돌려야 한다.
- [확인: 소스/빌드/오프라인] 신규 정의·가격 배열·네 에디터 인식. [미확인: 사용자 실전] 실제 게임 적용, 병용, 저장 재로드.
- 상세 구현 계약: 에디터 저장소 docs/DLC_FORMAT.md.

## 대체 도시 설정 작성 기능 1 (2026-09-26)

- 실행 후크 추가 없음. Core Payload.FromText의 top-level ops 전용 계약을 유지하며 주석만 보완한다.
- 공유 상태: 에디터 PatchDoc.cities.<tid>.variants 및 도시 JSON editorState. 기본 도시 ID·위치·항로·입항/출항·여급은 공유한다.
- 입력: 기존 도시 값 복사와 사용자가 별도 창에서 편집한 이름·국가·문화·외관·역참·교역품·상점.
- 수명주기: 별도 작업 문서에서 편집 → 완료 시 부모 문서에 한 번 적용 → Ctrl+S로 저장. 취소 시 부모 무변경, 창 종료 시 작업 문서 해제.
- 대체 설정은 게임 실행 ops에 포함하지 않는다. bool/DoneTalkParts 읽기, 이벤트 콜백, 게임 저장/불러오기 접점 없음.
- 근거: 에디터 docs/CITY_VARIANTS.md, tests/test_city_variants.py. 실제 이벤트 전환/병용 성공을 의미하지 않는다.
