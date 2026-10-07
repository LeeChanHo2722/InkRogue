# 보스·파워 확장 전달 및 체크포인트

> Historical local validation record. Angle-bracket paths are portable placeholders, not personal machine paths.

2026-10-07 / Unity 6000.5.6f1 / `FloorRouteProject`, `codex/floor-route-ux`.

## 실행

최신 Windows: `<LOCAL_WORKSPACE>/Builds/TowerCleanupBossPowers/TowerCleanup.exe`

옆의 Data/DLL 등 폴더 전체를 함께 유지한다. 이전 `TowerCleanupRunLives`, `TowerCleanupFloorRoutes` 빌드는 보존했다.

- EXE SHA256: `E0F2357D8E9DB65FC9F144E51124E3B366089DE2D8496979070510E2282C8596`
- Runtime Assembly-CSharp.dll SHA256: `992B3ADE18225D4CE5DD1B610A20855F61062355DF19E74E5432CADC1FA8732B`
- 컴파일/Windows 빌드 exit 0.
- 독립 실행 15초 유지, 검사한 오류 로그 없음. 독립 실행 전체 런 플레이는 미실행.

## 기능

- 오수 저장체: 느린 접근과 플레이어 발밑 위치를 고정한 시간차 오염 구역.
- 환풍구 군주: 측면 이동 후 조준 고정, 예고된 부채꼴 연사.
- 폐기물 압축기: 축을 고정한 긴 예고 구역, 돌진 후 반격 가능한 정지 시간.
- 타워 오염핵: 초록 안전 방향을 남긴 방사형 탄막과 교대하는 십자 위험 구역.
- 노란 위험 범위/초록 안전 방향 예고와 등장 화면 대응 힌트. 정지·사망·페이즈 전환 시 보스 소유 경고 구역 취소.
- 신규 파워 5개: 부상 세척, 역류 세척, 청소 궤적, 탄막 필터, 예비 탱크. 기존 15종과 합쳐 20종.
- 기존 수동 보상 선택, 최대 단계 제외, 중복 지급 방지, 런 공통 목숨, 노즐·모터 직접 선택 성장 유지.

## 검증

- `boss-power-variety-final.log`: **27 checks PASS**. 4종 실제 전투 코루틴 한 사이클, 이동/경고/사격/취소, 신규 파워 효과·쿨다운·소유자·재귀 방지·최대 단계·수동 보상 중복 방지. 파워 검사는 제어된 타깃 및 직접 호출도 사용한다.
- `boss-power-play.log`: **26 checks PASS**. 피격·부활·층 이동·보스 회복·보상·campaign 종료·endless 진행·게임오버·재시작.
- `boss-power-powers.log`: **28 checks PASS**. 기존 파워 효과, 무기 교체, 소유자 분리, 로봇 공격, 재시작.
- `boss-power-build-result.json`, `boss-power-standalone-result.json`: 빌드/초기 실행 증거.
- `boss-power-captures/warning-*.png`, `variety-*.png`: 경고/패턴 화면. 캡처의 최고 강화 수치는 테스트 fixture이며 시작 상태가 아니다.
- Graphify AST: **3848 nodes / 7206 edges / 197 communities**. 문서 의미 분석 미실행.
- Unity SearchDatabase 초기화 예외는 알려진 Editor 환경 예외로 별도 기록했다. 게임 예외는 제외하지 않았다. 임시 도우미는 제품 소스/빌드에서 제거했다.

## 보존

원본 프로젝트는 병합하지 않았으며 기존 사용자 Packages 변경은 그대로 유지했다. 원격 push, 공개 PR, 배포, 구매, 새 아트 제작은 하지 않았다. 최신 소스 커밋과 파일 식별자는 `LATEST_DELIVERY_IDENTITY.json`에 기록한다.

## 다음 작업

- 진행 저장/이어하기는 아직 미구현이다.
- 모든 체력 페이즈 및 맵별 이동 경계, 전체 런 자연 플레이, 플레이어 실제 회피에 대한 확인이 남았다.
- 90초 페이스, player100:robot60 화력 목표, 장시간 전투/수치 최적화는 사용자 지시대로 본격 QA로 보류했다.
- 정식 그래픽·음원과 Steam 게시/배포는 별도 범위다.
- 보스 이름과 공통 런 상태 HUD가 일부 해상도에서 겹치는 기존 표시 문제는 후속 UI 정리 항목이다.

재개 시 `Docs/BOSSES_AND_POWERS.md`, `Docs/TOWER_GAME_GUIDE_KO.md`와 최신 git status를 읽는다. 승인된 기능 단위만 이어가며 과거 체크포인트의 장시간 수치 suite를 자동 재개하지 않는다.
