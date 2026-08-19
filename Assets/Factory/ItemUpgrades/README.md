# Item Upgrades

## 1. 기능 검증 방법

`Assets/Scenes/SampleScene.unity`을 열고 Play Mode에 진입한다. 화면 좌측의 `아이템 업그레이드 [U]` 버튼을 누르거나 `U` 키를 눌러 창을 연다.

1. 중앙 패널 위에 `메소`, `누적 생산량` 책갈피 탭이 있고 선택 탭만 본문과 이어진 밝은 테두리로 표시되는지 확인한다.
2. 세로 목록 한 화면에 여러 몬스터가 보이고, 각 행에 아이콘·이름·레벨과 효과·비용·강화 버튼이 표시되는지 확인한다.
3. 자원이 모자란 행은 어두운 `부족` 버튼, 강화 가능한 행은 노란 `강화` 버튼으로 즉시 구분되는지 확인한다.
4. 포탈에 같은 몬스터 20개를 공급한 뒤 `누적 생산량` 탭으로 전환한다. 해당 행이 `20 / 20`과 `강화`를 표시하고 좌측 단축 버튼에 알림 배지가 생기는지 확인한다.
5. `강화`를 누르면 생산량 20개가 소비되고 레벨이 1, 개체 가치 배수가 `×1.25`로 바뀌는지 확인한다. 업그레이드는 조건 달성만으로 자동 실행되지 않아야 한다.
6. 메소를 20 이상 모은 뒤 `메소` 탭에서 강화한다. 20메소가 소비되고 합연산 보너스가 `+0.5메소`로 바뀌는지 확인한다.
7. 창이 열린 동안 키보드 카메라 이동, 우클릭 패닝과 휠 줌이 동작하지 않는지 확인한다. `Esc` 또는 우측 상단 `×`로 닫는다.
8. 레시피 선택 창이나 포탈 선택 창이 열려 있을 때 `U`를 눌러도 업그레이드 창이 겹쳐 열리지 않는지 확인한다.

Edit Mode 자동 검증은 Test Runner에서 `Maptory.Factory.Tests` 어셈블리의 `PortalTests`와 `ItemUpgradeUiTests`를 실행한다. 몬스터별 공유 진행도, 두 비용의 소비와 가치 계산, 행 수·탭 전환과 모달 배타성을 검증한다.

## 2. 기능 사용법

`PortalEconomy`가 메소, 몬스터별 누적/사용 가능 생산량과 두 업그레이드 레벨을 소유한다. 포탈 소비가 완료될 때 `RecordSupply`를 호출하면 현재 개체 가치만큼 메소를 지급하고 두 생산량을 1씩 증가시킨다. 업그레이드 가능 여부를 먼저 조회하고 명시적 구매 메서드를 호출한다.

```csharp
var economy = new PortalEconomy();
var monster = RawMaterialType.MonsterSnailRed;

for (var count = 0; count < 20; count++)
{
    economy.RecordSupply(monster);
}

if (economy.CanPurchaseProductionUpgrade(monster))
{
    economy.TryPurchaseProductionUpgrade(monster);
}

var value_per_item = economy.GetUnitValue(monster);
```

메소 업그레이드는 기본 가치에 레벨당 0.5메소를 더하고, 생산량 업그레이드는 합연산 결과에 레벨당 1.25배를 적용한다. 메소 비용은 `20 × 다음 레벨`, 생산량 비용은 `20 × 2^현재 레벨`이며 각 카테고리 최대 레벨은 20이다.

런타임 UI는 `ItemUpgradePanel.Create`로 만들고 다른 모달의 열림 상태를 `SetOtherModalCheck`에 연결한다. `ItemUpgradeShortcut.Create`는 같은 `PortalEconomy`와 패널을 받아 좌측 버튼과 알림 배지를 만든다. `FactoryGame`이 이 초기화 순서를 담당한다.

## 3. 코드 구조와 책임

| 파일 | 책임 |
| --- | --- |
| `ItemUpgradePanel.cs` | 업그레이드 모달, 책갈피 탭, 스크롤 목록, `U`/`Esc` 입력과 구매 요청 조립 |
| `ItemUpgradeRow.cs` | 몬스터 한 종의 아이콘·이름·레벨·효과·비용·강화 상태 표시와 클릭 전달 |
| `ItemUpgradeShortcut.cs` | 화면 좌측 열기 버튼과 생산량 강화 가능 몬스터 수 배지 표시 |

진행 상태와 계산은 `PortalSystem.cs`의 `PortalEconomy`가 소유하고 이 폴더의 코드는 표현과 입력만 담당한다. `FactoryGame`은 포탈 선택·레시피 선택·업그레이드 모달이 동시에 열리지 않도록 연결하고, `FactoryCameraController`에는 세 모달의 통합 차단 상태를 제공한다.

현재 업그레이드 진행도는 저장되지 않으며 게임 실행을 종료하면 초기화된다. 비용과 효과 상수는 프로토타입 밸런스 값이다.
