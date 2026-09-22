# Contribution 0.5.0 UI artwork

사용자 제공 두 이미지로부터 built-in `image_gen`으로 생성했다. CLI/API fallback은 사용하지 않았다. PNG alpha를 유지한 채 아래 프로젝트 경로에 복사했고 DLL 리소스로 포함한다. 글자와 현재 상태는 이미지에 고정하지 않고 Overlay가 그린다.

| 파일 | 입력/용도 | 최종 프롬프트 요구사항 (정리) |
|---|---|---|
| city-panel.png | 2번 `ChatGPT Image 2026년 9월 19일 오후 08_25_48.png` | 금색 장식·타륜·청록색 제목판·양피지·옅은 범선 배경을 유지한 빈 UI 패널. 글자, 아이콘, 상태 배지와 행을 제거. 바깥 배경은 실제 투명 alpha. |
| notice-panel.png | 1번 `ChatGPT Image 2026년 9월 19일 오후 08_30_24.png` | 금색 밧줄 테두리·나침반·남색 제목판·양피지·하단 빈 확인 버튼 유지. 모든 글자, 행 아이콘, 구분선, 스크롤바를 제거한 빈 알림 패널. 바깥은 투명. |
| icons.png | 두 참고 이미지와 같은 게임 UI 화풍 | 상자, 두루마리, 상회 건물, 금화, 저울 다섯 아이콘을 동일 간격의 가로 다섯 칸에 배치. 분리 가능한 크기, 투명 배경, 글자 없음. |

원본 생성 파일은 Codex generated_images/01a0b70d-b187-7140-b961-1a2edb6f7fde의 exec-0f1d5693-b6a2-4c03-b251-c8a46a9ce3db.png, exec-64a67b98-da5f-4102-bf69-be2b93295d51.png, exec-342357e2-40fa-491c-8d74-0c5ccca3ba05.png 순서다.

`UiAssets`가 최초 표시 때만 로드하고 화면 재구성에서는 캐시를 재사용한다. 모드 해제 시 자체 texture만 Dispose한다. `preview/Preview.csproj`는 실제 Overlay 소스를 GDI 대역으로 렌더링하는 오프라인 검증 도구다. PNG 미리보기는 게임 스크린샷이 아니며 FairyGUI의 실제 폰트/터치/텍스처 수명주기 검증을 대신하지 않는다.
