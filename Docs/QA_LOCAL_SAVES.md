# 로컬 저장 검증 결과 — 2026-10-07

Unity 6000.5.6f1, Windows StandaloneWindows64. 저장 위치를 `INKROGUE_SAVE_TEST_PATH`로 격리했고 실제 사용자 저장을 테스트하지 않았다. 기존 Packages 미커밋 변경은 유지했다.

## 통과

| 검사 | 결과 | 작업 루트 근거 |
|---|---|---|
| 파일 무결성 | 38개 통과 | `save-store-validation-final.log` |
| 캠페인/무한 회귀 | 26개 통과 | `save-boundary-validation-final.log`, `save-boundary-result.json` |
| Windows 빌드 | exit 0, Success | `save-windows-build-release.log`, `save-build-result.json` |
| 실제 프로세스 종료/재개 | 네 단계 전부 통과, reader 44개 + writer 준비 확인 6개 | `save-process-results.json`, `save-tests/process-20261007-202243/` |
| 새 UI | 이어하기·교체 취소/확인·오류 재시도·기록 상세의 버튼 raycast/이벤트와 1280×720 캡처 확인 | 위 경로의 `route/` |

프로세스 쌍은 모두 서로 다른 PID다.

| 체크포인트 | writer PID | reader PID | 확인 |
|---|---:|---:|---|
| 전투 층 시작 | 28728 | 39444 | writer 강제 종료; 층 시작 복원, 추가 패널티 없음, 정상 차감 목숨 지속 |
| 파워 선택 | 12404 | 30280 | 같은 파워 후보; 한 번만 소비 |
| 경로 선택 | 45420 | 41024 | 같은 경로/맵/시드; 미확정 편성 폐기; 저장 후 메뉴·새 런 취소/교체·쓰기 오류 재시도·기록 UI |
| 확정 경로 이동 | 33228 | 47772 | 선택 경로를 유지해 다음 층으로 한 번 진행 |

복원 검사에는 기본값이 아닌 Blade/Machine Gun 편성, 플레이어·로봇의 여러 파워, 2목숨, 정상 사망 기록이 들어간다. 전투 writer에서 저장 이후 변경한 상태는 재개 시 버려졌다. 최종 8개 player 로그에 런타임 예외/오류가 없다.

파일 검사는 temp 작성/교체 전후 실패, 잘린 데이터, 고아 temp, 필수 필드 누락, 지원 불가 파일/스키마, 불가능한 값, 주파일 손상+유효 백업, 양쪽 손상, 종료 중단과 영수증 복구, 중복 기록 방지, 새 런 교체 영수증 직후 중단을 확인한다. 실패 시 이전 또는 새 완전한 상태를 읽으며 종료/중단된 런을 부활시키지 않는다.

진행 검사에서 캠페인 20층 승리와 무한 20→21 진행 후 패배를 수행했다. 저장 파일에는 `Victory / TwentyFloor / 20`, `Defeat / Endless / 21` 기록이 있고 active는 null이다. 중간 보스 +1cap3, 중복 보스 콜백, 종료 후 보스 콜백의 부활 방지도 통과했다.

## 식별

- 실행: `Builds/TowerCleanupSaves/TowerCleanup.exe`
- `Assembly-CSharp.dll` SHA-256: `F01BC27E7270C8605CF6BC5A1D8FF326657969F7591449DA8121B4E9D067E878`
- Unity launcher EXE SHA-256: `E0F2357D8E9DB65FC9F144E51124E3B366089DE2D8496979070510E2282C8596`
- 소스: `codex/floor-route-ux` 작업 트리. 최종 커밋은 작업 루트 `CHECKPOINT_LOCAL_SAVES.md`에 기록한다.

## 수정 후 재검증한 실패

- 최초 JSON 검사에서 Unity JsonUtility가 null 객체를 빈 객체로 바꾸는 문제를 확인했다. 기존 Newtonsoft 패키지로 교체 후 무결성 검사를 통과했다.
- Unity Editor SearchDatabase 시작 예외가 자동 검사를 일시정지시켰다. 기존 다른 검사와 동일하게 에디터 인덱스 예외를 런타임 오류와 구분하고 검증기를 재개하도록 수정했다. 최종 게임 검사는 통과했다.
- 첫 UI 검사는 새 화면 생성과 같은 프레임에 raycast했다. 화면 갱신 이후 입력하도록 고친 뒤 실제 버튼 raycast가 통과했다.
- 숨긴 standalone 창의 backbuffer 캡처가 null이었다. 검증용 카메라/캔버스를 별도 RenderTexture로 촬영하고 즉시 복원했다. 최종 캡처를 확인했다.

## 미실행·한계

실제 전원 차단, 저장 장치 하드웨어 고장, 장시간 사람 플레이, 모든 해상도의 신규 저장 UI를 이번 단위에서 검증하지 않았다. 자동 진행은 층 완료와 보스 체력을 제어하며 게임의 실제 전환·입력·저장 코드를 실행한다. 전체 전투 스냅샷과 온라인/다중 슬롯 저장은 기획 범위 밖이다.

원격 push, 원본 브랜치 병합, PR 공개, 배포, 유료 구매는 하지 않았다.
