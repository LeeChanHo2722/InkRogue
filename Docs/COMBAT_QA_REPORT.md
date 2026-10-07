# 실제 전투 자동 검사와 밸런스 보정

후반 보스가 성장 화력에 비해 지나치게 빨리 사라지는 문제를 수정했습니다. 5층 보스 체력은 유지하고, 이후 보스에 `(1 + 0.14 × clamp(층 - 5, 0, 25))²`의 보정을 추가했습니다. 무한 모드의 기존 체력 배율은 계속 별도로 적용됩니다. 로봇 기본 공격의 최단 간격은 0.4초에서 0.7초로 조정했습니다.

## 검사 방법과 해석 범위

- Unity Play Mode, 실제 InputSystem 키보드·마우스 입력, Shooter 탄환 충돌, 적 AI, 잉크 소모, 실제 잠수 재충전을 사용했습니다. 전투 중 강제 피해·잉크 채움·순간이동을 사용하지 않았습니다.
- 보스 검사는 준비 단계에서 층을 지정하고 일반층을 건너뜁니다. 일반층 검사는 같은 첫 번째 맵의 Easy 섬멸전 계획과 시드 20261006을 해당 층 수치로 재생성합니다. 모든 맵·난이도의 수동 완주 검사는 아닙니다.
- 5/10/20층 보스는 총 4/9/19회, 100층은 총 30회 투자합니다. 플레이어/로봇 집중 또는 양쪽 분할이며, 노즐 보강·모터 보강만 사용해 기본 성장 차이를 비교합니다. 특수 파워 효과는 별도 회귀 검사 대상입니다.
- 보호 전투는 시작 시 피격 보호를 적용해 화력과 재충전 흐름을 관찰합니다. 무보호 전투는 실드·피격·목숨 소모를 정상 적용합니다. 단순 자동 조작의 패배가 사람의 클리어 불가능을 뜻하지는 않습니다.
- 피해는 보스와 소환된 적을 포함한 실제 전투 중 합계입니다. 시간에는 이동·무적 페이즈·재충전도 포함됩니다. 준비 단계와 이전 층 피해는 제외했습니다.
- 보호 검사는 180초 이내 정상 처치를 요구합니다. 준비 단계와 실시간 실행에도 별도 시간 제한을 두어 검사 자체의 무한 대기를 방지했습니다.
- 초기 검사에서 비동기 런 초기화 시점과 재충전 목적지 선택 문제를 고쳤습니다. 재충전 봇 수정 때문에 수정 전후 시간 차이를 모두 게임 수치 변경의 효과로 볼 수는 없습니다.
- 첫 일반층 검사에서 100층 플레이어 표본은 모서리에 걸린 자동 이동 때문에 시간 초과됐습니다. 실제 BoxCollider 크기를 사용하는 제한된 경로 탐색을 검사 코드에 추가하고, `combat-wave-recovery.log`에서 100층만 다시 검사했습니다. 원래 실패 로그도 보존했습니다.

## 로봇 집중 빌드의 시간 차이 해석

20층 기본 강화 비교에서는 로봇 집중이 플레이어 집중보다 빠릅니다. 로봇은 잉크 재충전 없이 확정 타격하지만 사거리가 4이고 가까운 표적을 자동 선택합니다. 플레이어는 조준·탄환·잉크 유지 비용이 있는 대신 무기 선택과 직접 표적 선택, 교대 압력·살균 촉매 등의 배율 효과를 사용할 수 있습니다. 이번 기본 강화 비교에는 그 특수 효과를 넣지 않았습니다. 보호 검사에서는 두 빌드 모두 진행 가능했고, 100층의 소환 적과 이동을 포함한 시간 차이는 약 1.6배였습니다. 특화 계통의 피해 기여가 지배적이므로 완전 동률을 만들기 위한 추가 하향은 하지 않았습니다. 무보호 생존력과 숙련자 수동 플레이에서의 조합별 체감 차이는 별도 평가 범위입니다.

## 무보호 결과와 열린 검증 과제

무보호 기본 검사 4개 중 10층 로봇 일반전은 클리어했고, 1층 일반전·20층 혼합 보스·100층 로봇 보스는 목숨을 모두 소진했습니다. 추가한 사격 없는 로봇 집중 20층 보스 검사도 13.19초에 패배했습니다(피격 14회, 사망 3회, 로봇 피해 2796.06). 따라서 무보호 20층 완주나 체감 난이도 최종 검증을 완료했다고 주장하지 않습니다.

자동 조작은 위험 예고를 해석하는 회피와 방어 특수 파워를 사용하지 않습니다. 이 패배만으로 사람의 진행 불가능 결함을 확정할 수는 없습니다. 현재 재현된 미해결 컴파일 오류·런타임 예외·진행 정지는 없으며, 수동 생존 난이도 검증은 남아 있습니다. `VALIDATION_COMPLETED ... failures=0`은 검사 실행과 지정된 불변 조건이 정상이라는 뜻입니다. 실제 승패는 표의 `clear`/`defeat`/`combat-timeout`을 기준으로 구분해야 합니다.


## combat-boss-baseline-3.log

| 시나리오 | 결과 | 초 | 플레이어 피해 | 로봇 피해 | 플레이어 % | 피격/사망 | 재충전 |
|---|---|---:|---:|---:|---:|---|---:|
| boss-5-player-protected | clear | 7.98 | 148.25 | 2.10 | 98.60 | 0/0 | 0 |
| boss-5-robot-protected | clear | 9.68 | 37.00 | 123.00 | 23.13 | 0/0 | 1 |
| boss-5-mixed-protected | clear | 15.54 | 92.49 | 62.04 | 59.85 | 0/0 | 1 |
| boss-10-player-protected | clear | 4.69 | 179.30 | 1.05 | 99.42 | 0/0 | 0 |
| boss-10-robot-protected | clear | 3.48 | 13.00 | 177.00 | 6.84 | 0/0 | 0 |
| boss-10-mixed-protected | clear | 6.03 | 126.18 | 63.82 | 66.41 | 0/0 | 0 |
| boss-20-player-protected | clear | 3.52 | 299.65 | 0.70 | 99.77 | 0/0 | 0 |
| boss-20-robot-protected | clear | 2.03 | 3.00 | 297.00 | 1.00 | 0/0 | 0 |
| boss-20-mixed-protected | clear | 3.86 | 130.65 | 179.35 | 42.14 | 0/0 | 0 |
| boss-100-player-protected | clear | 4.68 | 1248.30 | 1.05 | 99.92 | 0/0 | 0 |
| boss-100-robot-protected | clear | 3.26 | 6.00 | 1253.00 | 0.48 | 0/0 | 0 |
| boss-100-mixed-protected | clear | 4.91 | 408.04 | 850.96 | 32.41 | 0/0 | 0 |

실행 종료: TOWER_COMBAT_VALIDATION_COMPLETED cases=12 errors=0


## combat-boss-final.log

| 시나리오 | 결과 | 초 | 플레이어 피해 | 로봇 피해 | 플레이어 % | 피격/사망 | 재충전 |
|---|---|---:|---:|---:|---:|---|---:|
| boss-5-player-protected | clear | 16.00 | 168.15 | 3.85 | 97.76 | 0/0 | 1 |
| boss-5-robot-protected | clear | 9.67 | 33.00 | 127.00 | 20.63 | 0/0 | 1 |
| boss-5-mixed-protected | clear | 14.41 | 99.61 | 54.93 | 64.46 | 0/0 | 1 |
| boss-10-player-protected | clear | 15.03 | 516.85 | 3.85 | 99.26 | 0/0 | 1 |
| boss-10-robot-protected | clear | 7.91 | 33.00 | 497.00 | 6.23 | 0/0 | 1 |
| boss-10-mixed-protected | clear | 16.41 | 323.71 | 218.29 | 59.73 | 0/0 | 1 |
| boss-20-player-protected | clear | 24.84 | 2887.05 | 6.30 | 99.78 | 0/0 | 2 |
| boss-20-robot-protected | clear | 10.56 | 35.00 | 2858.00 | 1.21 | 0/0 | 1 |
| boss-20-mixed-protected | clear | 25.98 | 1218.41 | 1708.59 | 41.63 | 0/0 | 2 |
| boss-100-player-protected | clear | 121.18 | 26033.90 | 30.10 | 99.88 | 0/0 | 12 |
| boss-100-robot-protected | clear | 75.19 | 194.00 | 25856.00 | 0.74 | 0/0 | 7 |
| boss-100-mixed-protected | clear | 115.93 | 7300.19 | 19026.81 | 27.73 | 0/0 | 11 |

실행 종료: TOWER_COMBAT_VALIDATION_COMPLETED cases=12 errors=0 failures=0


## combat-wave-final.log

| 시나리오 | 결과 | 초 | 플레이어 피해 | 로봇 피해 | 플레이어 % | 피격/사망 | 재충전 |
|---|---|---:|---:|---:|---:|---|---:|
| wave-1-player-protected | clear | 83.85 | 199.00 | 15.00 | 92.99 | 0/0 | 8 |
| wave-1-robot-protected | clear | 109.81 | 193.45 | 20.55 | 90.40 | 0/0 | 10 |
| wave-1-mixed-protected | clear | 86.29 | 198.50 | 15.50 | 92.76 | 0/0 | 9 |
| wave-10-player-protected | clear | 41.41 | 383.15 | 3.85 | 99.01 | 0/0 | 4 |
| wave-10-robot-protected | clear | 47.01 | 86.00 | 301.00 | 22.22 | 0/0 | 4 |
| wave-10-mixed-protected | clear | 53.39 | 270.82 | 116.18 | 69.98 | 0/0 | 5 |
| wave-20-player-protected | clear | 50.89 | 620.85 | 3.15 | 99.50 | 0/0 | 4 |
| wave-20-robot-protected | clear | 48.86 | 127.00 | 497.00 | 20.35 | 0/0 | 6 |
| wave-20-mixed-protected | clear | 45.71 | 480.38 | 143.62 | 76.98 | 0/0 | 4 |
| wave-100-player-protected | combat-timeout | 180.00 | 1910.06 | 1.94 | 99.90 | 0/0 | 2 |
| wave-100-robot-protected | clear | 43.48 | 73.00 | 3238.00 | 2.20 | 0/0 | 3 |
| wave-100-mixed-protected | clear | 41.18 | 1832.99 | 1478.01 | 55.36 | 0/0 | 4 |

실행 종료: TOWER_COMBAT_VALIDATION_COMPLETED cases=12 errors=0 failures=1


## combat-wave-recovery.log

| 시나리오 | 결과 | 초 | 플레이어 피해 | 로봇 피해 | 플레이어 % | 피격/사망 | 재충전 |
|---|---|---:|---:|---:|---:|---|---:|
| wave-100-player-protected | clear | 38.97 | 3306.61 | 4.39 | 99.87 | 0/0 | 4 |
| wave-100-robot-protected | clear | 61.57 | 155.00 | 3156.00 | 4.68 | 0/0 | 7 |
| wave-100-mixed-protected | clear | 41.84 | 1646.08 | 1664.92 | 49.72 | 0/0 | 3 |

실행 종료: TOWER_COMBAT_VALIDATION_COMPLETED cases=3 errors=0 failures=0


## combat-survival-final.log

| 시나리오 | 결과 | 초 | 플레이어 피해 | 로봇 피해 | 플레이어 % | 피격/사망 | 재충전 |
|---|---|---:|---:|---:|---:|---|---:|
| wave-1-player-unprotected | defeat | 50.01 | 151.00 | 8.40 | 94.73 | 24/3 | 4 |
| wave-10-robot-unprotected | clear | 51.92 | 91.00 | 296.00 | 23.51 | 16/1 | 4 |
| boss-20-mixed-unprotected | defeat | 16.79 | 1062.44 | 1016.13 | 51.11 | 15/3 | 0 |
| boss-100-robot-unprotected | defeat | 10.76 | 54.00 | 6168.00 | 0.87 | 9/3 | 0 |

실행 종료: TOWER_COMBAT_VALIDATION_COMPLETED cases=4 errors=0 failures=0


## combat-death-final.log

| 시나리오 | 결과 | 초 | 플레이어 피해 | 로봇 피해 | 플레이어 % | 피격/사망 | 재충전 |
|---|---|---:|---:|---:|---:|---|---:|
| wave-1-player-unprotected-idle | defeat | 15.60 | 0.00 | 2.80 | 0.00 | 13/3 | 0 |
| wave-1-player-unprotected-restart | restart-ready | 0.01 | 0.00 | 0.00 | 0.00 | 0/0 | 0 |
| boss-15-mixed-protected | clear | 19.49 | 501.13 | 804.87 | 38.37 | 0/0 | 2 |
| wave-20-robot-protected-robot-only | clear | 39.08 | 0.00 | 624.00 | 0.00 | 0/0 | 0 |

실행 종료: TOWER_COMBAT_VALIDATION_COMPLETED cases=4 errors=0 failures=0


## combat-robot-control.log

| 시나리오 | 결과 | 초 | 플레이어 피해 | 로봇 피해 | 플레이어 % | 피격/사망 | 재충전 |
|---|---|---:|---:|---:|---:|---|---:|
| boss-20-robot-unprotected-robot-only | defeat | 13.19 | 0.00 | 2796.06 | 0.00 | 14/3 | 0 |

실행 종료: TOWER_COMBAT_VALIDATION_COMPLETED cases=1 errors=0 failures=0


## 남은 검증 범위

장시간 수동 완주, 모든 맵과 난이도에서의 체감 난이도, 모든 특수 파워 조합의 실제 DPS는 이번 자동 검사의 범위를 벗어납니다. 새 그래픽과 외부 배포는 수행하지 않았습니다. 검사 로그와 JSON 원자료는 작업 폴더에 보존했습니다.

20층 일반전·보스의 별도 표본을 합산한 player 집중 빌드의 주력 계통 피해 비중: **99.73%**.

20층 일반전·보스의 별도 표본을 합산한 robot 집중 빌드의 주력 계통 피해 비중: **95.39%**.
