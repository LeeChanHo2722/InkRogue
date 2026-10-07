# InkRogue Floor UX — 종료 전 체크포인트

> Historical local validation record. Angle-bracket paths are portable placeholders, not personal machine paths.

사용자의 컴퓨터 종료 준비 요청으로 새 구현과 장시간 검증을 중단했다. **새 Windows 빌드는 아직 만들지 않았다.** 이 문서를 읽고 기존 변경을 이어서 작업할 것.

## 실제 작업 위치

- 작업 루트: `<LOCAL_WORKSPACE>`
- 실제 수정 프로젝트: `FloorRouteProject` — 원본 Git 저장소의 격리 worktree.
- 작업 브랜치: `codex/floor-route-ux`. 종료 전 WIP 커밋은 옆의 `FLOOR_ROUTE_CHECKPOINT_IDENTITY.json`에 기록.
- 기준 커밋: `88b3ca1ac5c2dba835106ab0a9e60a212c69b3c8`.
- 원본: `<UNITY_PROJECT>`, 브랜치 `codex/tower-cleaning-systems`. 원본에 이번 코드 변경을 합치지 않았다. 원본을 열면 이전 UI다.
- 원본과 worktree의 사용자 변경 `Packages/manifest.json`, `Packages/packages-lock.json`은 동일하게 보존했으며 커밋에서 제외한다. 해시: manifest `93FFAA36DDC730C0C5E6BE74572F9CF4CFDE4DCFC7DF5E3D1EF68DEB4CA07ED8`, lock `69AE200238645802A54275FCB90D2FAA808EC96DFA303CE231D06946ABE9394B`.
- 사용자 원본 Unity를 강제 종료하지 않았다. 우리의 검증 runner는 취소했고, 확인 시 두 InkRogue 프로젝트를 대상으로 한 Unity 프로세스는 없었다. 컴퓨터 종료 명령은 실행하지 않았다.

## 구현 완료, 아직 최종 배포 아님

- 기존 맵 3개와 소탕/Rush/Defense를 독립 조합. Easy/Normal/Hard 3카드 유지. 동일 목표 두 개 허용, 세 개 모두 같은 목표만 금지.
- 최근 두 번 선택한 맵/목표가 다시 나오는 조합의 가중치를 낮춤. 반복을 완전히 금지하지 않는다.
- run seed + floor + catalog/order + choice history로 재현. 취소/재열기 리롤 없음. 실제 encounter seed로 예상 적 수/동시 출현/모드 시간을 표시.
- 난이도별 **서로 다른 파워 후보 최대 2/3/4개 중 정확히 1개 획득**. 최대 단계 제외, 부족한 pool 허용, pool 0은 기존 정비. 파워 소유권/성장식은 이번 변경에서 유지.
- 카드 미리보기와 출발 확정 분리. 입력 중복 방어, Esc 취소. 무기 편성은 draft, 적용 시 반영, 취소 시 원복. E=왼손, Q=오른손 실제 바인딩 표기.
- 빈/부적합 맵 데이터는 재확인/메뉴 안내. 빌드 목록의 유효 맵만 후보에 포함.
- 일반 모드 최종 20층은 엔딩 목표 표시. 무한 20/40층은 파워 보상 후 계속.
- 기존 Scene/Prefab YAML 및 아트는 수정하지 않았다. runtime uGUI layout이다.
- 세부 명세: `FloorRouteProject/Docs/FLOOR_ROUTE_UX.md`.

## 통과한 검사

`floor-route-validation-results.json`, `floor-route-extra-results.json` 및 아래 로그가 근거다.

| 검사 | 결과/로그 |
|---|---|
| RNG/catalog/reward pure | 45 checks, `floor-route-pure-final.log` |
| 1,000 seed × 19 offers | 19,000회, 144 ordered sets, 모든 27 map/mode/difficulty 조합 관측 |
| 최근 조합 반복 비교 | 7,250/35,000 = 20.7%; 동일 maps/history의 uniform counterfactual 11,767/35,000 = 33.6%; `floor-route-captures/distribution.txt` |
| 3맵 × 3목표 실제 시작 | 22 checks, 방어 대상과 quota 일치 포함; `floor-route-matrix-final.log` |
| 최신 UI 실제 입력 | 44 checks, `floor-route-ui-input-final.log`; synthetic InputSystem mouse → EventSystem → 카드/새 출발 버튼, 실제 키보드 Esc. 단순 handler 호출로 대체하지 않음 |
| 편성 draft/취소/적용 | 위 UI 검사 통과. 중복 클릭으로 파워 2개 지급되지 않음 |
| 경계층 | 12 checks, `floor-route-boundary-final.log`; campaign19→20 종료, endless19→20→21 및39→40→41 계속 |
| 기존 core | 24 checks, `floor-route-core-regression.log` |
| 기존 play/input/power/wheel/robot | 모두 exit0. 각각 `floor-route-*-regression.log`; power33/wheel39/robot42 |
| 캡처 | `floor-route-captures`: 1920×1080,1280×720,1024×768,2560×1080,편성,보상,보스/최종층 등 |

검증 경계: synthetic 입력은 실제 InputSystem/UI 경로 검사이며 물리 마우스 수동 플레이와 구분한다. 층 이동/보스 경계 테스트는 제어된 클리어 및 피해 fixture를 썼다. 완전한 무보호 20층 수동 플레이를 완료했다고 말하지 말 것.

## 해결한 검증 환경 문제와 현재 미완료

- fresh worktree Unity 6000.5 Editor의 SearchDatabase 시작 예외가 Error Pause를 유발했다. 테스트 지원 파일은 이 알려진 Editor 초기화 예외만 gameplay count에서 제외하고 원래 로그는 유지했다. 실제 게임 예외는 제외하지 않는다.
- 초기 mouse test는 Editor update의 입력 이벤트 소모 문제였다. Dynamic InputState 방식으로 수정해 카드/취소/출발까지 통과.
- 해상도 캡처 직후 출발 버튼 좌표가 임시 render projection을 가리키던 fixture는 overlay 안정화 후 raycast를 검증하도록 수정해 통과. TMP mesh 재생성으로 연속 해상도 캡처의 누락 글자도 해결.
- robot 첫 재실행은 `TOWER_ROBOT_CAPTURE_DIR` 누락이었다. 수정 후42 checks 통과. 실패 로그 `floor-route-robot-env-failed.log` 보존.
- **미완료:** 새 변경 기준 combat boss/wave/survival/death 전체 suite, Windows 빌드, 독립 실행 smoke, Graphify refresh, 최종 QA/전달 문서 및 Library 저장.
- combat boss를 시작하려던 runner를 사용자 요청으로 취소했다. 완료 결과가 없으므로 통과로 표시하지 않는다.
- `BatchValidationSupport.cs`는 임시 로컬 검증 도구다. WIP 제품 커밋에 포함하지 않음. 원본은 `floor-ui-diagnosis/BatchValidationSupport.cs`; 다음 검증 전에 재복사한다.
- `floor-ui-diagnosis/implement_routes.py`, `final_capture_patch.py`, `run-boundary-combat-build.ps1`은 이미 적용한 과거 작업 스크립트다. **다시 실행하지 말 것.**

## 다음 접속 재개

1. 이 문서와 `FloorRouteProject/Docs/FLOOR_ROUTE_UX.md`, identity JSON, git status/log를 읽는다. 원본 사용자 Packages를 stage/overwrite하지 않는다.
2. Ponytail/Graphify/관련 Unity UI/CLI 스킬 및 원본 AGENTS 지침을 준수한다. Unity 버전은 `6000.5.6f1`, Editor 경로 `<UNITY_EDITOR>`.
3. 새 테스트 시작 승인을 이미 받은 작업을 재개할 때:

```powershell
Set-Location '<LOCAL_WORKSPACE>'
& '.\floor-ui-diagnosis\resume-checkpoint.ps1'
```

이 스크립트는 열려 있는 worktree Editor가 있으면 중단한다. 사용자의 다른 Editor는 종료하지 않는다. 남은 combat4 suite만 실행하고 성공 시 임시 지원 파일을 제거한 뒤 `Builds/TowerCleanupFloorRoutes/TowerCleanup.exe`로 빌드한다. 실패하면 로그를 진단하고 필요한 부분만 재실행한다.

4. 빌드 성공 후 EXE 독립 실행 smoke, runtime `Assembly-CSharp.dll` SHA256, Graphify AST update, 최종 QA 문서와 로컬 커밋을 완료한다. 원격 push/공개 PR/배포하지 않는다. Library preview 저장은 아직 하지 않았다. 원본 프로젝트로 통합하지 않은 사실을 최종 보고한다.
5. Graphify interpreter: `<GRAPHIFY_PYTHON> -X utf8 -m graphify update .`를 실제 worktree에서 실행. 패키지 업그레이드하지 않는다.

## 사용자 최신 요구 D01–D06 (다음 작업 단위)

- D01: 일반적인 층의 이상적인 템포가90초. hard timeout 아님. Rush/Defense는 모드 고유 시간이며 일괄90초를 넣지 않는다. UI는 실제 Rush40초 설정을 표시한다.
- D02: 층마다 목숨 reset 대신 Run 공유 목숨. 보스 처치+1, cap3. **이번에 미구현**.
- D03: 수동 파워 선택 유지. 총 파워 단계만으로 기본 공격력 자동 scaling 제거/개선 필요. 현재 너무 쉬운지 QA. **성장 재설계는 이번에 미구현**.
- D04: 동등 투자 시 player100:robot 약60 목표(자동 공격 편의의 대가). 정확한 수치는 QA로 결정. 반대 계통 피해가 미미해야 한다는 조건과 별도. **이번에 재조정하지 않음**.
- D05: 보스마다 독립 개성/패턴 필요. 공유 패턴에 수치만 다른 것은 부족. **추가 보스 재설계 미구현**.
- D06: Steam 고려 중이며 출시/배포 승인이 아니다.
- 파워20종 및 효과음CC0 조사는 부모가 별도로 진행. 이번 코드에 새 파워/음원을 임의 추가하지 않는다.

부모 동기화용 직접 사용자 메시지(이 실행 스레드에 도착, message ID 미제공):
> 선택지는 3개가 같은 경우만 막으면 괜찮아
> 즉 소탕 소탕 러쉬 : 가능
> 방어 방어 방어 : 불가능

원문은 `floor-ui-diagnosis/user-clarification.txt`에도 보존했다.
