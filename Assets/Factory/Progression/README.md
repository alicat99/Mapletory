# 스테이지와 사냥터 진행

## 1. 기능 검증 방법

`Assets/Scenes/SampleScene.unity`을 열고 Play Mode에 진입한다. 첫 화면에는 2개 스테이지와 보유 메소가 표시되며 1스테이지만 `입장`, 2스테이지는 `해금`으로 표시되어야 한다.

1. 메소가 부족한 잠긴 스테이지를 누르면 비용과 부족 메소가 표시되고 구매 버튼이 비활성화되는지 확인한다.
2. 1스테이지에 컨베이어·건물·포탈을 설치하고 레시피와 포탈 아이템을 선택한 뒤 `돌아가기`로 복귀한다. 다시 입장했을 때 위치·방향·선택 상태가 복원되어야 한다.
3. 스테이지 선택 화면에 머물거나 다른 스테이지에 입장한 동안에도 저장된 기존 공장의 포탈 생산량과 메소가 계속 증가하는지 확인한다.
4. 포탈을 설치하고 클릭한다. 현재 스테이지의 사냥터만 나타나며 최초 사냥터 외의 행은 `[잠금]`과 별도 `해금` 버튼으로 구분되어야 한다.
5. 잠긴 사냥터의 `해금`을 눌러 해금 몬스터와 메소 비용을 확인한다. 메소가 부족하면 붉게 표시되고 구매할 수 없어야 한다.
6. 조건을 만족해 구매하면 메소만 한 번 차감되고 사냥터 행이 즉시 선택 가능 상태로 바뀌는지 확인한다.
7. Play Mode를 다시 시작해 공장 배치, 스테이지·사냥터 해금, 메소와 몬스터 진행도가 복원되는지 확인한다.

Edit Mode의 `ProgressionTests`는 기본 해금, 스테이지·사냥터 구매 원자성, 중복 구매, 부족 조건의 부분 차감 방지, 잠긴 몬스터 직접 선택 차단, 설정 직렬화, 선택 화면의 UI 입력 시스템과 자동 저장 지연을 검증한다.

## 2. 기능 사용법

콘텐츠 값은 `Resources/Factory/Progression/FactoryContentConfig`의 `FactoryContentConfig` 에셋에서 편집한다. 각 `FactoryStageDefinition`은 스테이지 ID·표시명·메소 비용·맵 시드를, 각 `HuntingGroundDefinition`은 몬스터·초기 해금 여부·메소 비용을 정의한다. 1스테이지는 달팽이→빨간 달팽이→파란 달팽이, 2스테이지는 파란 버섯→주황 버섯→초록 버섯→주황 뿔버섯→초록 뿔버섯 순서다.

기본 사냥터 해금 비용은 1스테이지가 `0 / 50 / 300`, 2스테이지가 `0 / 1,000 / 3,000 / 5,000 / 10,000` 메소다. 디버그에서 변경한 비용은 설정 세이브가 우선한다.

```csharp
var configAsset = Resources.Load<FactoryContentConfig>(
    "Factory/Progression/FactoryContentConfig");
var config = configAsset.CreateRuntimeCopy();
var save = new FactorySaveService();
var economy = new PortalEconomy();
save.LoadSettings().Apply(config, economy);
var progression = new FactoryProgression(
    config, economy, save, save.LoadProgress());
gameObject.AddComponent<FactoryProgressAutosave>().Initialize(progression);

if (progression.CanUnlockHuntingGround("lith_harbor_outskirts"))
{
    progression.TryUnlockHuntingGround("lith_harbor_outskirts");
}
```

사냥터 구매는 `FactoryProgression`만 호출하며 메소를 확인하고 차감한다. UI가 아닌 코드에서 포탈 품목을 지정해도 `PortalState`의 허용 조건이 잠긴 몬스터 선택을 거부한다.

## 3. 코드 구조와 책임

| 파일 | 책임 |
|---|---|
| `FactoryContentConfig.cs` | ScriptableObject 기반 스테이지·사냥터 정의와 런타임 편집 가능한 메소 비용 |
| `FactorySaveService.cs` | PlayerPrefs JSON에 진행 데이터, 스테이지별 공장 배치, 디버그 설정 오버라이드를 분리 저장·불러오기 |
| `FactoryProgression.cs` | 해금 상태, 구매 검증·원자적 소비, 저장 요청과 현재 스테이지 세션 소유 |
| `FactoryProgressAutosave.cs` | 변경된 진행 데이터를 2초 간격 및 앱 비활성화·종료 시 저장 |
| `StageSelectionPanel.cs` | 스테이지 목록·잠금·구매 팝업, 입장과 우측 상단 돌아가기 UI |
| `Resources/Factory/Progression/FactoryContentConfig.asset` | 빌드에서 사용하는 2개 스테이지와 순서화된 사냥터·비용 원본 |

`FactoryGame`은 저장 설정을 적용한 `PortalEconomy`와 `FactoryProgression`을 먼저 만든 뒤, 스테이지 선택 화면 또는 해당 스테이지의 공장 런타임을 조립한다. 컨베이어, 건물 방향, 레시피와 포탈 선택은 스테이지별 플레이 진행으로 저장한다. 현재 보이지 않는 저장 공장은 `FactoryHeadlessRuntime`이 같은 운송 규칙으로 계속 갱신하므로 스테이지 선택 화면과 다른 스테이지에서도 메소 생산이 유지된다. 이동 중인 개별 아이템과 기계 내부 대기 재료는 저장하지 않고 재입장 시 빈 상태에서 다시 흐르기 시작한다.
