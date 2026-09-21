# NavtoCity — 0.2.0

DLL: `Restitutor_fixes_NavtoCity.dll`. 항해 → 도시 진단용. **0.2.0 사용자 원본 로그 3개 입항에서 새 계측 기록 확인**. 2개는 tail_complete, 1개는 ready 후 mod_shutdown으로 tail 단축. 오류/누락 보고 0, operation 10개 완료 성공 관측. 모든 환경의 후킹/계측 부하/병용 무결함을 보장하지 않는다. 이전 0.1.0 processId 39064의 입항 8건도 직접 집계했다.

[다섯 로그 리뷰](../../../analysis/user-reports/2026-09-18-nav-to-city/FIVE-LOG-REVIEW.md): 새 unload 6회 모두 장면 2→1, 제거 1개, 게임 GC 카운터 +1. 전환 원본의 직접 GC.Collect 호출 2곳도 추가 확인. 목록 개수 증가보다 반복 수집/정리 비용의 분해가 우선이나 GC별 시간/누수 미확정. 14분 실행도 입항은 처음 151초 내에만 있어 장시간 비교 자료는 부족하다. DLL 변경 없음.

0.2.0: 장면 정리 전후 개수·제거 선택 수·게임 IL2CPP GC/힙, 코루틴 상태·호출 ID 연결, 실제 반환 operation 완료/진행률 관측을 추가했다. 정확한 GC 소요 시간과 렌더링 완료 시각은 계측하지 않는다. `schemaVersion=2`.

[사용자 전달 분석과 원본 후속 확인](../../../analysis/user-reports/2026-09-18-nav-to-city/REPORT.md): UnloadSceneWithout 시간 증가 보고. 원본에는 장면 목록 순회 외 미사용 리소스 정리 및 GC.Collect 계열과 같은 네이티브 수집 경로가 있다. 그 검토 당시에는 DLL을 바꾸지 않았고, 이번 0.2.0에서 관측을 보강했다. 각 작업의 병목 비중/누수 여부는 미확정이다.

원본 로그 후속: unload 합 T1 112ms→T8 247ms. 도시 진입 후 T4의 513ms Update 간격은 Pub.unity 로드 요청 동기 호출 508ms와 겹친다. T2 tail 899ms는 현재 계측 호출로 설명되지 않는다. 관측 창 내 2~3초 단일 Update 간격은 없었다. [재현 집계](../../../analysis/user-reports/2026-09-18-nav-to-city/verified-log-summary.json).

[사용법](../../../NavtoCity/README.md) · [패치](PATCHES.md) · [현재 버전/근거](0.2.0.md) · [이전 버전](0.1.0.md)

바다에서 PortManager.EnterHarbour 요청 시 전환별 기록을 시작한다. 도시 준비 상태 확인 + 10초, 거절/다른 장면/예외/120초 제한/새 요청/종료 시 기록 범위를 정리한다. 원본 생략, 인수·반환값 변경, 완료 콜백 교체, 게임 상태·캐시·GC·저장 변경은 없다.

15개 메서드의 호출 및 코루틴 단계 시간, 100ms 이상 Update 간격, 실행 전체 5초 간격 프로세스 메모리/CPU 표본을 JSONL에 기록한다. CoreCLR GC는 게임 IL2CPP GC가 아니다. 비동기 요청 반환 시간과 작업 완료를 구별한다.

Release 빌드 경고/오류 0, 대역 46개 통과, 기존 설치 interop 17개 및 추가 조회 13개와 필드 확인. 0.2.0 위 관측 실행은 확인했으나 실제 전체 부담/사용자 2~3초 증상 원인은 미확인이다. 루트 DLL 제공, 게임 Mods 폴더에는 자동 설치하지 않았다. 이전 DLL은 NavtoCity/releases/0.1.0에 보존했다.
