# InkRogue 완료 단위 및 빌드 증거

> Historical local validation record. Angle-bracket paths are portable placeholders, not personal machine paths.

날짜: 2026-10-07. Unity 6000.5.6f1. 원본을 보존한 `codex/floor-route-ux` 격리 worktree.

## 실행 파일

- 최신: `<LOCAL_WORKSPACE>/Builds/TowerCleanupRunLives/TowerCleanup.exe`
- Floor UX 단위 보존본: `<LOCAL_WORKSPACE>/Builds/TowerCleanupFloorRoutes/TowerCleanup.exe`
- 실행 파일과 같은 폴더의 Data/DLL 등 전체 파일이 필요하다.
- 최신 EXE SHA256: `E0F2357D8E9DB65FC9F144E51124E3B366089DE2D8496979070510E2282C8596`
- 최신 `TowerCleanup_Data/Managed/Assembly-CSharp.dll` SHA256: `DDF5F57E5D5C72A0B2B17258725FA7B391470278BD3E6235A49228A4DA2258C8`
- Floor UX 보존본 runtime DLL SHA256: `DFD24C7F89E4DC5C5989756472BA85DD7824DB2A204604EA34B6161AAEFE7BC2`

두 Windows 빌드 모두 종료 코드 0. 두 독립 실행 파일 모두 15초 초기 실행을 유지했고 검사한 오류 로그가 없었다. 독립 실행 전체 런 플레이는 미실행이며, 진행 흐름 검증은 Editor Play Mode에서 수행했다.

## 반영

- Floor UX: 두 목표 중복은 허용하고 세 목표가 모두 같은 조합만 제외한다. 맵·목표·난이도 및 실제 생성 조건 표시, 미리보기 후 출발 확정, 무기 준비 draft 적용/취소.
- 보상: 난이도별 최대 2/3/4개 후보에서 정확히 1개만 획득. 중복 클릭 차단.
- 별도 후속 단위: 런 공통 목숨, 보스 처치 +1/최대 3/중복 회복 차단, 새 런 초기화.
- 파워 총 단계 자동 기본 화력 성장 제거. 노즐/모터 보강을 선택해야 기본 화력이 성장한다. 기존 개별 효과와 수동 선택 유지.

## 통과

- 기존 Floor UX: pure 45, map/mode matrix 22, 실제 UI 입력 44, campaign/endless 경계 12. 기존 core 24, power 33, wheel 39, robot 42 및 play/input 회귀 통과 결과 보존.
- 최신 `TowerPlayValidation.Start`: 26 checks. 피격·부활, 층 이동, 목숨 유지, 보스 회복/중복/상한, campaign 종료, endless 계속 진행, 마지막 목숨과 재시작.
- 최신 `TowerPowerValidation.Start`: 28 checks. 파워 적용·소유자 분리·무기 교체·로봇 공격·재시작. 종합 수치 검사 제외.
- 로컬 Graphify AST 갱신: 3788 nodes / 7076 edges / 184 communities. 문서 의미 분석은 미실행.
- `git diff --check` 통과. 원본과 worktree의 사용자 Packages 변경은 동일 해시로 보존, 커밋 제외.

작업 루트의 `floor-route-*-result*.json`, `floor-route-*-final.log`, `run-lives-growth-*.log`, `run-lives-growth-*-result.json`이 증거다. 기존 스크린샷은 `floor-route-captures/`에 있다.

Editor의 SearchDatabase 초기화 예외는 별도 환경 경고로 보존했다. 임시 테스트 도우미는 이 예외만 게임 오류 집계에서 제외하고 일시정지를 해제했다. 게임 예외는 제외하지 않았으며, 도우미는 최종 소스/빌드에서 제거했다.

## 미실행·후속

사용자가 지금은 간단한 수치 확인만 원하므로 장시간 boss/wave/survival/death 수치 suite, 광범위 seed 재반복, 전체 런 자연 플레이, 90초 페이스 최적화, 동등 투자 player100:robot60 상세 검증은 미뤘다. 기존 완료된 결과는 보존했다.

보스별 추가 고유 패턴, 정식 아트/사운드 제작과 새 파워 카탈로그 도입은 이번 완료 단위에 포함하지 않았다. 현재 빌드는 기능 검증 가능한 플레이 빌드이며 출시 완성본이라고 표시하지 않는다.

원본 `<UNITY_PROJECT>`에는 병합하지 않았다. 기존 원본을 열면 이전 상태가 보인다. 최신 상태는 이 격리 프로젝트 또는 위 실행 파일에서 확인한다. 원격 push, PR 공개, 배포, 에셋 구매는 하지 않았다.
