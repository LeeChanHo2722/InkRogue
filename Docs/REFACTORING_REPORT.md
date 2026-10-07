# 동작 보존 리팩터링

기준 소스는 `2207b39`다. 새로운 기능이나 밸런스 변경 없이, 씬과 프레임 실행에 섞여 있던 정책을 독립적으로 확인할 수 있게 분리했다.

## 범위 선택

현재 큰 클래스는 `FloorManager.Encounter.cs` 2,066줄, `FloorTransitionManager.cs` 1,678줄, `PlayerHandInkMorphController.cs` 2,220줄이다. 파일 길이만으로 분할하지 않았다. 전투·전환 코루틴과 시각 효과의 순서를 바꾸면 조작감이나 씬 참조가 영향을 받을 수 있다. 이번에는 호출 경계가 분명하고 기존 회귀가 있는 세 영역을 선택했다.

| 영역 | 이전 | 이후 |
|---|---|---|
| 무기 편성 중복 규칙 | RunManager 저장, UI 드래프트 배치, 목록 비활성화가 각각 검사 | 세 호출부 모두 `WeaponLoadoutRules.ContainsInOtherSlot` 사용 |
| 경로 생성 | RunManager가 캐시·층 상태·맵 유효성과 난수 알고리즘까지 관리 | RunManager는 상태·유효성·캐시를 유지. `FloorRouteGenerator`가 입력을 받아 후보 계산 |
| 파워 성장 | TowerPowers가 레벨 저장·후보 추첨·소유자 합계와 전투 효과를 함께 관리 | `TowerPowerProgression`이 성장 상태와 후보 계산 소유. TowerPowers는 기존 공개 API와 전투 실행 유지 |

새 정책 클래스에는 GameObject 탐색, Time, Physics, Unity 전역 난수 호출이 없다. 경로 생성은 기존 에셋 데이터 참조를 입력으로 받고 결과 배열을 반환한다. 파워 성장은 인스턴스별 상태와 명시적 시드를 사용하므로 씬을 띄우지 않고 검사할 수 있다. 공통 기반 클래스나 의존성 주입 프레임워크를 추가하지 않았다.

## 보존한 계약

- MonoBehaviour·ScriptableObject 이름, 기존 파일 경로와 `.meta` GUID, 직렬화 필드 및 열거형 값은 유지한다.
- RunManager와 TowerPowers의 공개 호출 방식은 그대로다. 호출자는 새 구현을 알 필요가 없다.
- 경로 시드 공식, 난수 호출 순서, 최근 경로 가중치, 목표 두 개 중복 허용·세 개 중복 금지, 후보 캐시와 2/3/4 보상 규칙은 동일하다.
- 파워 카탈로그 순서, 최대 단계, 양쪽 소유자 후보 보장, 후보 정렬 방식과 성장 공식은 동일하다.
- 파워의 피해 소유자, 연쇄 방지, 피격 처리, 구독 해제, 쿨다운, Time 사용은 변경하지 않았다.
- 현재 Floor/무기 UX, 양손 드래프트, 취소·확정, Q/E 조작, 아이콘과 에셋 참조는 유지한다.

## 동작 보존 근거

런타임 수정 전에 `RefactorBehaviorValidation.Capture`로 실제 공개 API 결과를 기록했다. `Docs/refactor-behavior-baseline.json`이 기준이며 `Verify`가 동일 결과를 확인한다.

- 경로: 두 런 모드 × 64개 시드 × 39단계 = 4,992단계. 맵·난이도·목표·전투 시드·보스 여부·보상 수를 비교한다.
- 파워: 24개 시드 × 120단계 = 2,880단계. 후보 순서·선택 결과·각 파워 단계·소유자 합계·표시 요약과 성장 공식을 비교한다. 최대 단계 이후도 포함한다.
- 직렬화: 기존 `TowerCleanupWeaponUX` 실행본 DLL의 SHA256을 검증한 뒤 현재 게임 타입의 필드 이름/형식과 열거형을 직접 비교했다.
- 기존 씬·프리팹·에셋·메타 1,212개 파일의 변경 전 해시는 작업 루트 `refactor-assets-before.json`에 보관한다.

처음 만든 직렬화 검사에는 컴파일러가 `[Serializable]`을 붙인 람다 보조 클래스까지 포함되어, 코드 추출 자체를 스키마 변경으로 잘못 판단했다. 이를 제외하고 이전 DLL과 대조했다. TMP 예제 디버그 도구의 에디터 전용 필드도 플레이어 빌드와 원래 다르므로 게임 스키마 비교에서 제외했다. 새 코드로 기준을 덮어쓴 것이 아니라, 보존된 이전 DLL에서 기준을 얻었다. 경로·파워 지문은 최초 캡처 값을 유지했다.

기존 `TowerPowerValidation`은 다른 배치 검사와 동일하게 알려진 UnityEditor.Search.SearchDatabase 초기화 예외만 제외하고 에디터 Error Pause를 해제하도록 정리했다. 게임 오류 검사는 유지한다.

최종 통과/실패 기록과 실행본 식별자는 작업 루트 `LATEST_DELIVERY.md` 및 `LATEST_DELIVERY_IDENTITY.json`을 따른다.

최종 소스의 회귀 결과:

- `refactor-loadout.log`: 편성 실제 입력·취소·확정·Q/E 및 세 해상도 반복 검사 **1,200개 통과**.
- `refactor-routes.log`: 경로 정책·잘못된 맵·캐시·최대 단계 후보 검사 **45개 통과**.
- `refactor-powers-final.log`: 경로 4,992단계·파워 2,880단계 지문 및 수정된 게임 스키마 일치. 실제 피해·효과 소유자·재귀 방지·로봇·재시작 검사 **28개 통과**.
- `refactor-core-flow.log`: 메뉴·보상·경로·보스 4종 12개 페이즈 상태·승패·재시작 실제 입력/렌더 검사 **322개 통과**. 전투 결과와 페이즈 전환은 통제된 테스트이며 전체 런 자연 플레이가 아니다.
- `refactor-assets-result.json`: 기존 참조 관련 파일 **1,212개 중 변경 0개**.
- `refactor-schema-final.log`: 이전 실행본 DLL과 게임 소유 직렬화 필드/열거형 일치.
- Graphify AST 갱신: 4,022 nodes / 7,560 edges / 200 communities. 의미 분석은 실행하지 않았다.
- `refactor-build-result.json`: 최종 컴파일·Windows 빌드 종료 코드 0. 실행 파일은 `Builds/TowerCleanupRefactored/TowerCleanup.exe`다.
- `refactor-standalone-result.json`: 실행본 15초 초기 실행 유지, 검사한 오류 로그 없음. 실행본 전체 런은 미실행이다.

초기 파워 검사와 첫 DLL 스키마 비교는 위에 설명한 검사 범위 문제로 실패했다. 검사를 수정한 후 최종 회귀는 통과했다. 이를 게임 동작 실패로 숨기거나 최종 검증 전체가 처음부터 통과한 것으로 기록하지 않는다.

## 남긴 부채

- FloorManager와 FloorTransitionManager의 긴 코루틴·상태 전이, 여러 컴포넌트의 시간 배율 소유권은 그대로다. 별도의 전이 순서 회귀를 확보한 후 다룰 수 있다.
- 런 UI의 화면 생성·표시 상태 결합과 압축된 코드 스타일은 남아 있다. 이번에는 외형·입력 흐름을 바꾸지 않았다.
- 파워 전투 효과는 여전히 TowerPowers에 모여 있다. 성장 정책을 분리했지만 효과별 클래스를 일괄 도입하지 않았다.
- 넓은 static 접근과 FindAnyObjectByType 사용을 전역 교체하지 않았다. 성능 개선을 측정하거나 주장하지 않는다.
- 전체 런 자연 플레이와 광범위 수치 밸런스 QA는 미뤘다. 저장·이어하기, 새 패키지, 새 아트와 배포는 추가하지 않았다.
