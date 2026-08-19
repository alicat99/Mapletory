# 런타임 디버그 도구

## 1. 기능 검증 방법

`Assets/Scenes/SampleScene.unity`을 열고 Play Mode에 진입한 뒤 `F2`를 누른다. 화면 왼쪽에 전체 화면 음영이 없는 디버그 패널이 나타나야 하며, 패널 바깥에서는 우클릭 패닝과 휠 줌이 계속 동작해야 한다.

1. `맵` 탭에서 잔디 브러시를 고르고 월드를 좌클릭 드래그해 타일 이미지가 바뀌는지 확인한다.
2. 원재료 브러시를 골라 빈 3×3 영역에 배치하고, 겹치는 영역에는 추가 배치되지 않는지 확인한다.
3. `제거` 브러시로 컨베이어, 건물 또는 원재료를 지우고, `이동 아이템 제거`로 운송 중인 아이템을 정리한다.
4. `몬스터` 탭에서 대상을 전환한 뒤 기본 가치, 레벨당 합연산 값, 레벨당 곱연산 계수, 현재 레벨과 사용 가능 생산량을 바꾼다. 최종 가치 요약과 포탈·업그레이드 UI가 즉시 갱신되어야 한다.
5. `업그레이드` 탭에서 몬스터를 순환하며 메소·누적 생산량 기본 비용을 개별 변경하고, 두 비용 계수를 바꿔 `기본 비용 × 계수^현재 레벨`이 즉시 반영되는지 확인한다.
6. `해금` 탭에서 두 스테이지와 사냥터를 순환하며 각 메소 비용을 변경한다.
7. `변경사항 저장 후 처음부터 실행`을 누르면 잔디·원재료 맵 편집, 몬스터 가치, 업그레이드 비용·계수, 스테이지·사냥터 비용은 유지되고 메소·업그레이드·해금 진행은 초기화된 스테이지 선택 화면으로 재시작하는지 확인한다.
8. 저장한 스테이지에 재입장해 잔디와 원재료 배치가 복원되는지 확인하고, 다시 `F2`를 눌러 패널을 닫는다.

Edit Mode의 `Maptory.Factory.Tests`는 원재료 런타임 배치·제거 이벤트, 스테이지별 맵 데이터와 커스텀 몬스터·업그레이드·해금 설정의 저장·재적용, 기존 공장 규칙의 회귀를 검증한다.

## 2. 기능 사용법

`FactoryGame`이 `FactoryDebugMapEditor`와 `FactoryDebugPanel`을 생성하고 기존 네트워크·UI 참조를 주입하므로 기본 Scene에서는 별도 Inspector 구성이 필요 없다. 다른 진입점에서 사용할 때도 동일하게 공장 상태 객체를 먼저 만든 다음 두 컴포넌트를 초기화한다.

```csharp
var mapEditor = root.AddComponent<FactoryDebugMapEditor>();
mapEditor.Initialize(camera, tilemap, grassTiles, mapSize, buildMode,
    conveyorBuilder, extractorBuilder, extractionNetwork,
    demolitionController, itemTransport);

var debugPanel = FactoryDebugPanel.Create(canvas.transform, catalog,
    portalEconomy, mapEditor, progression, saveService);
```

몬스터 밸런스는 `PortalEconomy.SetBaseValue`, `SetMesoBonusPerLevel`, `SetProductionMultiplierPerLevel`로 바꾼다. 몬스터별 기본 비용은 `SetUpgradeBaseCosts`, 공통 비용 계수는 `SetUpgradeCostCoefficients`로 바꾸며 최대 레벨 제한은 없다. 이 API는 디버그 UI와 동일한 정식 런타임 상태를 수정한다.

## 3. 코드 구조와 책임

| 파일 | 책임 |
|---|---|
| `FactoryDebugPanel.cs` | `F2` 패널, 맵·몬스터·업그레이드·해금 탭, 숫자 입력, 전체 설정 저장과 새 게임 재시작 |
| `FactoryDebugMapEditor.cs` | 잔디·원재료·제거·아이템 브러시 입력과 스테이지별 맵 설정 캡처 |

디버그 도구는 별도의 복제 상태를 만들지 않고 `ExtractionNetwork`, `PortalEconomy`, `FactoryProgression`, `FactoryItemTransport` 등 정식 시스템의 공개 명령만 호출한다. 명시적 재시작 버튼은 밸런스·해금·현재 스테이지 맵 설정을 설정 세이브에 남기고 플레이 진행 세이브를 삭제한다. 맵 설정은 크기와 전체 잔디 타일 종류, 원재료 종류·중심 좌표를 저장한다.
