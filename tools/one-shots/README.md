# tools/one-shots — 일회성 설치·커밋·push 스크립트 보관

2026-09-21 ~ 09-24 에 저장소 최상위에서 만들어 실행한 `.bat`/`.ps1` 을 기록용으로 옮겨 둔 곳 (2026-09-25).

- 모두 한 번 실행하고 끝난 스크립트다. 대부분 절대 경로(`E:\Documents\ChatGPT\Sailing_era_Restitutor`)를 쓰고 `origin/main` 이 특정 상태라는 전제로 쓰였으므로 **다시 실행하지 않는다.** 설치·배포 이력(해시·버전)을 확인하는 자료로만 본다.
- 새 일회성 스크립트는 이 폴더에 만든다. 저장소 최상위의 `.bat`/`.ps1` 은 `.gitignore` 로 막혀 있다.
- 생성기 `tools/oneshot.py <설정.json>` 은 현재 폴더에 `<이름>.ps1`·`.bat` 을 쓰므로 이 폴더에서 실행한다.
- 배포 기록 규칙: `docs/mods/DEPLOYMENT_POLICY.md`.
