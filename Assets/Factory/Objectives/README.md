# 현재 목표

## 1. 기능 검증 방법

스테이지 오른쪽에서 현재 목표를 확인한다. 원재료 생산, 첫 몬스터 제작, 포탈 공급, 업그레이드, 사냥터 해금, 다음 몬스터 제작을 진행하면 실제 생산·경제·해금 이벤트에 따라 한 단계씩 이동해야 한다. `레시피 보기`를 누르면 목표 대상이 선택된 E 도감이 열린다. 같은 실행 중 스테이지 재입장 후에는 단계가 유지되고 앱을 다시 시작하면 초기화되는지 확인한다.

## 2. 기능 사용법

`FactoryGame`이 `FactoryObjectiveSystem`을 생성하고 현재 `FactoryItemTransport`, `PortalEconomy`, `FactoryProgression`, `FactoryCodexPanel`을 연결한다. Inspector 설정은 없다.

```csharp
var step = progression.Objectives.current_step;
```

## 3. 코드 구조와 책임

| 파일 | 책임 |
|---|---|
| `FactoryObjectiveProgressData.cs` | 현재 실행 세션의 목표 단계 소유 |
| `FactoryObjectiveSystem.cs` | 게임 이벤트 기반 진행과 목표 HUD/도감 연결 |

목표는 생산 시뮬레이션을 직접 조회하지 않고 기존 시스템 이벤트를 구독한다.
