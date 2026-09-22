# Restitutor Base v1.0.1 (Google Drive 배포)

- 배포처: Google Drive (GitHub 릴리스 아님). 사용자 결정 2026-09-23: 1차 배포는 Base 만.
- 내용: v1.0.0 Base 의 DLL 11개 그대로(해시는 `release/v1.0.0-files.json` Base 와 대조) + `Mods/Additional_Functions/manifest.json`, `Mods/Bug_Fixes/manifest.json`(`{}` + LF, 3 B) + `README_Restitutor-Base.txt`(UTF-8 BOM, CRLF).
- 바뀐 이유: MelonLoader 0.7.2 부터 Mods 하위 폴더는 manifest.json 이 있어야 읽는다(공식 CHANGELOG). v1.0.0 zip 에는 없었다.
- 생성: `python3 tools/make_base_zip.py` → `release/v1.0.1-base/Restitutor-Base-v1.0.1.zip` (zip 은 저장소에 넣지 않음).
- zip SHA256: 9FE497BD3B6EB7A6BF87DC85019A909501F43883C9DC42967F79797C2C25872F
- 미확인: MelonLoader 0.7.3 을 새로 설치한 게임에서 이 zip 으로 로드되는지(깨끗한 설치 시험 없음).
- 포함 안 됨: Stat Rank 0.2.x, Cheats, Rebalance 전부.
- 다른 묶음 README 초안: `release/readme-draft/` (Cheats·Rebalance 는 v1.0.0 기준, 배포 전 갱신 필요).
