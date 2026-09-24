# Dialogue audio preview v1 — 2026-09-24

대화 탭/별도 대화 창의 Bgm·Voice 선택 옆 재생/정지 및 볼륨 슬라이더. 기본 50%, 게임/문서 설정과 별개. 한 에디터당 미리보기 하나, 선택 변경·숨김·창 닫힘에 정지. Sound.Tid -> AssetPath -> Addressables AudioClip.samples, GUI 밖 해독, 64 MiB 메모리 캐시. 디스크 음원 추출/게임 변경 없음.

[확인: 스키마] Talk.Bgm/Voice 각각 String vector=false: 한 줄에 각 하나. 줄별 곡 전환 가능. 엔진 전체 믹싱 한계 미확인, 동시 다중 BGM 기능 미구현.

검사 Character 248/248, Dialogue 47/47, GUI 165/165. 실제 게임 BGM/효과음 해독, 무음 Qt 재생 시간 진행/취소/정지/볼륨 확인. 사용자 청음 미확인. PR https://github.com/Restitutor2025/Sailing-Era-Editor/pull/73 병합, 실제 에디터 main d68e9fd pull 완료. 해시 output/audio-preview-install.txt 및 에디터 docs/character-editor/dialogue-audio-preview-v1.json. Core0.7.7 유지.
