# 런타임 디버그 도구

## 1. 기능 검증 방법

`Assets/Scenes/SampleScene.unity`을 열고 Play Mode에 진입한 뒤 `F1`을 누른다. 화면 왼쪽에 전체 화면 음영이 없는 디버그 패널이 나타나야 하며, 패널 바깥에서는 우클릭 패닝과 휠 줌이 계속 동작해야 한다.

1. `맵` 탭에서 잔디 브러시를 고르고 월드를 좌클릭 드래그해 타일 이미지가 바뀌는지 확인한다.
2. 원재료 브러시를 골라 빈 3×3 영역에 배치하고, 겹치는 영역에는 추가 배치되지 않는지 확인한다.
3. `제거` 브러시로 컨베이어, 건물 또는 원재료를 지우고, `이동 아이템 제거`로 운송 중인 아이템을 정리한다.
4. `몬스터` 탭에서 대상을 전환한 뒤 기본 가치, 레벨당 합연산 값, 레벨당 곱연산 계수, 현재 레벨과 사용 가능 생산량을 바꾼다. 최종 가치 요약과 포탈·업그레이드 UI가 즉시 갱신되어야 한다.
5. `업그레이드` 탭에서 메소·누적 생산량 기본 비용과 최대 레벨을 변경하고 업그레이드 버튼의 비용·최대 레벨 상태가 즉시 반영되는지 확인한다.
6. 다시 `F1`을 눌러 패널을 닫는다.

Edit Mode의 `Maptory.Factory.Tests`는 원재료 런타임 배치·제거 이벤트, 커스텀 몬스터 가치 계산과 기존 공장 규칙의 회귀를 검증한다. 디버그 값과 맵 편집 결과는 런타임 세션에만 유지된다.

## 2. 기능 사용법

`FactoryGame`이 `FactoryDebugMapEditor`와 `FactoryDebugPanel`을 생성하고 기존 네트워크·UI 참조를 주입하므로 기본 Scene에서는 별도 Inspector 구성이 필요 없다. 다른 진입점에서 사용할 때도 동일하게 공장 상태 객체를 먼저 만든 다음 두 컴포넌트를 초기화한다.

```csharp
var mapEditor = root.AddComponent<FactoryDebugMapEditor>();
mapEditor.Initialize(camera, tilemap, grassTiles, mapSize, buildMode,
    conveyorBuilder, extractorBuilder, extractionNetwork,
    demolitionController, itemTransport);

var debugPanel = FactoryDebugPanel.Create(canvas.transform, font, roundedSprite,
    mapEditor, portalEconomy);
```

몬스터 밸런스는 `PortalEconomy.SetBaseItemValue`, `SetAdditiveValuePerLevel`, `SetProductionMultiplierPerLevel`로 바꾸며, 업그레이드 비용과 상한은 `SetUpgradeCosts`, `SetMaximumUpgradeLevel`로 바꾼다. 이 API는 디버그 UI와 동일한 정식 런타임 상태를 수정한다.

## 3. 코드 구조와 책임

| 파일 | 책임 |
|---|---|
| `FactoryDebugPanel.cs` | `F1` 패널, 맵·몬스터·업그레이드 탭, 숫자 입력과 런타임 밸런스 적용 |
| `FactoryDebugMapEditor.cs` | 잔디·원재료·제거·아이템 브러시 입력을 기존 맵·건설·운송 시스템에 연결 |

디버그 도구는 별도의 복제 상태를 만들지 않고 `ExtractionNetwork`, `PortalEconomy`, `FactoryItemTransport` 등 정식 시스템의 공개 명령만 호출한다. UI는 디버그 입력을 소유하고, 월드 편집기는 좌클릭 좌표를 격자 명령으로 변환한다. 현재는 맵 크기 변경, 저장·불러오기와 런타임 변경값의 영속화를 지원하지 않는다.
