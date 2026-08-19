# 스테이지와 사냥터 진행

## 1. 기능 검증 방법

`Assets/Scenes/SampleScene.unity`을 열고 Play Mode에 진입한다. 첫 화면에는 3개 스테이지와 보유 메소가 표시되며 1스테이지만 `입장`, 나머지는 `해금`으로 표시되어야 한다.

1. 메소가 부족한 잠긴 스테이지를 누르면 비용과 부족 메소가 표시되고 구매 버튼이 비활성화되는지 확인한다.
2. 1스테이지에 입장한 뒤 우측 상단 `돌아가기`가 스테이지 선택 화면으로 복귀시키는지 확인한다.
3. 포탈을 설치하고 클릭한다. 현재 스테이지의 사냥터만 나타나며 최초 사냥터 외의 행은 `[잠금]`과 별도 `해금` 버튼으로 구분되어야 한다.
4. 잠긴 사냥터의 `해금`을 눌러 메소, 해금 몬스터, 필요 재료·수량과 현재 보유량을 확인한다. 부족한 항목은 붉게 표시되고 구매할 수 없어야 한다.
5. 조건을 만족해 구매하면 메소와 사용 가능 생산량이 한 번만 차감되고 사냥터 행이 즉시 선택 가능 상태로 바뀌는지 확인한다.
6. Play Mode를 다시 시작해 스테이지·사냥터 해금, 메소와 몬스터 진행도가 복원되는지 확인한다.

Edit Mode의 `ProgressionTests`는 기본 해금, 스테이지·사냥터 구매 원자성, 중복 구매, 부족 조건의 부분 차감 방지, 잠긴 몬스터 직접 선택 차단, 설정 직렬화와 UI 잠금 상태를 검증한다.

## 2. 기능 사용법

콘텐츠 값은 `Resources/Factory/Progression/FactoryContentConfig`의 `FactoryContentConfig` 에셋에서 편집한다. 각 `FactoryStageDefinition`은 스테이지 ID·표시명·메소 비용·맵 시드를, 각 `HuntingGroundDefinition`은 몬스터·초기 해금 여부·메소와 필요 생산 재화를 정의한다.

```csharp
var configAsset = Resources.Load<FactoryContentConfig>(
    "Factory/Progression/FactoryContentConfig");
var config = configAsset.CreateRuntimeCopy();
var save = new FactorySaveService();
var economy = new PortalEconomy();
save.LoadSettings().Apply(config, economy);
var progression = new FactoryProgression(
    config, economy, save, save.LoadProgress());

if (progression.CanUnlockHuntingGround("lith_harbor_outskirts"))
{
    progression.TryUnlockHuntingGround("lith_harbor_outskirts");
}
```

사냥터 필요 재료는 별도 인벤토리를 복제하지 않고 기존 `PortalEconomy`의 몬스터별 `AvailableProduction`을 사용한다. 구매는 `FactoryProgression`만 호출하며 내부에서 메소와 생산 재화를 모두 확인한 뒤 함께 차감한다. UI가 아닌 코드에서 포탈 품목을 지정해도 `PortalState`의 허용 조건이 잠긴 몬스터 선택을 거부한다.

## 3. 코드 구조와 책임

| 파일 | 책임 |
|---|---|
| `FactoryContentConfig.cs` | ScriptableObject 기반 스테이지·사냥터 정의와 런타임 편집 가능한 비용·요구량 |
| `FactorySaveService.cs` | PlayerPrefs JSON에 진행 데이터와 디버그 설정 오버라이드를 분리 저장·불러오기 |
| `FactoryProgression.cs` | 해금 상태, 구매 검증·원자적 소비, 저장 요청과 현재 스테이지 세션 소유 |
| `StageSelectionPanel.cs` | 스테이지 목록·잠금·구매 팝업, 입장과 우측 상단 돌아가기 UI |
| `Resources/Factory/Progression/FactoryContentConfig.asset` | 빌드에서 사용하는 기본 스테이지·사냥터 비용과 조건 원본 |

`FactoryGame`은 저장 설정을 적용한 `PortalEconomy`와 `FactoryProgression`을 먼저 만든 뒤, 스테이지 선택 화면 또는 해당 스테이지의 공장 런타임을 조립한다. 입장과 돌아가기는 같은 Scene을 다시 로드해 스테이지 런타임을 분리한다. 현재 컨베이어·건물 배치 자체는 저장하지 않으므로 스테이지를 나갔다가 재입장하면 공장 맵은 초기 상태로 시작한다.
