# 로봇 탐색·복귀·공격 효과 업데이트 — 2026-10-06

> Historical local validation record. Angle-bracket paths are portable placeholders, not personal machine paths.

최신 실행본: `Builds/TowerCleanupRobotCompanion/TowerCleanup.exe`.

로봇 전용 42개와 Q/E 포함 회귀 118개, 총 **160개 검증 통과**. 탐색·추격·복귀 및 공격 효과의 실제 렌더 캡처 8장과 효과 픽셀 비교를 확보했습니다. Windows 빌드 성공. 세부 내용과 한계는 `ROBOT_QA_REPORT.md`에 기록했습니다.

---

# Q/E 휠 슬로우 업데이트 — 2026-10-06

최신 실행본: `Builds/TowerCleanupWheelSlow/TowerCleanup.exe`.

- Q/E 무기 휠 동안 0.1배 감속, 커서·선택 즉시 반응, 닫을 때 이전 배속/물리 간격 복원.
- 휠 전용 39개와 기존 회귀 79개 검증 통과. 일시정지·사망·층 이동·재시작·결과 화면 충돌 검증 포함.
- Windows 빌드 성공. 자세한 범위와 한계는 `WHEEL_QA_REPORT.md`.
- 기존 생존 밸런스 검증 한계는 아래 기록과 `COMBAT_QA_REPORT.md`대로 유지됩니다.

---

# 후속 밸런스 보정 검증 — 2026-10-06

최신 실행본: `Builds/TowerCleanupBalanced/TowerCleanup.exe`. 이전 `TowerCleanupFinal` 폴더는 보존했습니다.

- 게임 변경: 5층 체력 유지, 이후 보스 체력 보강, 로봇 최소 공격 간격 0.7초. 소유권 분리·기존 그래픽 유지.
- 보호 전투: 보스 12개와 일반층 12개 클리어 확인. 초기 100층 일반전의 검사 봇 경로 실패는 몸체 충돌 경로 탐색을 보강한 뒤 해당 층 3개를 재검증해 해결.
- 추가 확인: 사망 3회와 패배 화면, 깨끗한 런 재시작, 15층 보스, 플레이어 사격 없는 로봇의 20층 일반전 클리어.
- 무보호 결과: 기본 4개 중 10층 일반전 클리어, 나머지 3개 패배. 추가 20층 로봇 이동 중심 표본도 패배. **무보호 20층 완주와 수동 생존 난이도는 미검증**이며, 해당 패배를 성공으로 집계하지 않았습니다.
- 회귀: `balanced-TowerPowerValidation.log` 33개, `balanced-TowerPlayValidation.log` 16개, `balanced-TowerInputValidation.log` 30개, 합계 **79개 통과**.
- 빌드: `balanced-windows-build.log`, `Build Finished, Result: Success.`, exit 0.
- 실행: `balanced-standalone-smoke.log`, Direct3D12 초기화·프로세스 응답 정상, 확인한 런타임 예외 없음. 소유한 스모크 프로세스는 확인 후 정상적으로 정리했습니다.
- 런타임 어셈블리 SHA256: `97F812478294602B168F748B87FCD9E3172527F7923B2F75BEF6AC7B3E257880`.
- 현재 재현된 미해결 컴파일 오류·런타임 예외·진행 정지는 없습니다. 수동 생존 밸런스 평가가 남아 있습니다.

전체 표본·검사 한계는 `COMBAT_QA_REPORT.md`, 원자료 48행(수정 전 비교와 재검증 중복 포함)은 `combat-results.json`에 기록했습니다. 사용자 Packages 변경은 커밋에서 제외하며, 원격 push·배포·새 그래픽 작업은 수행하지 않았습니다.

---

아래는 보정 전 기능 완성 단계의 검증 기록입니다.

# Tower Cleanup 최종 검증 기록

- 프로젝트: `<UNITY_PROJECT>`
- Unity: `6000.5.6f1`
- 브랜치: `codex/tower-cleaning-systems`
- 시스템 커밋: `8a69ee6`
- 파워·동료·최종 QA 커밋: `344dd9a`
- 최종 실행 파일: `Builds/TowerCleanupFinal/TowerCleanup.exe`
- 원본 및 신규 아트: 기존 아트 유지. 다운로드한 새 로봇 PNG는 프로젝트에 적용하지 않음.

## 최종 통과 결과

| 검사 | 결과 | 근거 |
|---|---|---|
| 모드·보상·보스·전환 | 16개 통과 | `final-play-validation-3.log` |
| 파워·동료·소유권·중첩·초기화 | 33개 통과 | `final-power-validation.log` |
| 입력·실제 탄환 피해·잠수 강화·음원·일시정지 | 30개 통과 | `final-input-validation.log` |
| Windows Standalone | Build Success, exit 0 | `final-windows-build.log` |
| 실행 파일 그래픽 시작 | Direct3D 12 초기화, 정상 응답, 미처리 예외 없음 | `final-standalone-smoke.log` |
| 코드 공백 검사 | 통과 | `git diff --cached --check -- '*.cs'` |
| 지식 그래프 | AST 갱신 성공, 3474 nodes / 6709 edges | graphify update |

최종 실행 파일은 테스트 후 종료했습니다. 공개 배포·원격 push·PR 생성은 하지 않았습니다. 사용자가 기존에 수정한 `Packages/manifest.json`, `Packages/packages-lock.json`은 커밋 대상에서 제외했고 각각 +1줄, +17줄 변경 상태를 보존했습니다.

## 실제로 확인한 범위

- InputSystem 상태 주입으로 WASD 이동, 오른손 Shooter 사격, 왼손 SplashBomb 충전·투척, 탄환 충돌 피해와 잉크 생성 확인
- Shift 잠수 충전·복귀, 잠수 후 강화 명중 확인
- Esc 일시정지·재개 및 정지 중 런 타이머 유지 확인
- 효과음 8종의 유효한 샘플·신호·클리핑 부재 확인
- 세 가지 서로 다른 파워 선택지에 양쪽 소유자 포함, 효과 상한과 전체 최대 강화 시 정비 보상 확인
- 연쇄·처치 폭발·회복·필터·교대 강화·근접 흡수·잉크 촉매 확인
- 로봇 처치가 플레이어 회복을 발동하지 않음, 추가 피해 재귀 차단, 과잉 피해 제외 집계 확인
- 기존 5종 무기의 양손 교체 후 파워 적용 유지 확인
- 로봇의 추종·대상 공격·장애물 시야 차단·대상 소멸·층 변경·재시작 초기화 확인
- 20층 최종 승리, 중간 보스 후 계속 진행, 무한 10층 보스 이후 11층 진행과 적 HP 성장 확인

## 해석 한계

진행 검사는 층 강제 이동·보스 즉시 처치·플레이어 보호 상태를 사용합니다. 입력 검사는 자동화된 가상 장치로 진행했습니다. Windows 실행 파일은 그래픽 초기화와 시작 상태를 확인했으며, 전체 플레이는 Unity Play Mode에서 자동 검사했습니다.

집중 빌드 피해 비율은 실제 기본 Shooter 6 DPS를 이용한 기준 계산이며 명중 실패·특수 효과·무적·이동 시간은 제외합니다. 플레이어 12회 투자 시 플레이어 99.77%, 로봇 12회 투자 시 로봇 96.55%, 6회씩 혼합하면 플레이어 55.29% / 로봇 44.71%입니다. 장시간 사람 플레이에 따른 난이도 조정은 별도입니다.

## 화면 증거

- `final-captures/shoot.png`: 기존 캐릭터 사격과 노란 임시 로봇
- `final-captures/dive.png`: 잠수 상태
- `final-captures/pause.png`: 한글 UI, 재개/복귀, 소유자별 보유 파워 표시

사용 방법과 파워 목록은 `TOWER_GAME_GUIDE_KO.md`를 참고하세요.
