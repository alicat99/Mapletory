# Factory Map, Extraction, Conveyors, and Processing

## 1. 기능 검증 방법

`Assets/Scenes/SampleScene.unity`을 열고 Play Mode에 진입한다. Scene에는 `Main Camera`, `Global Light 2D`, `Factory Game` 오브젝트가 있어야 하며, `Factory Game`에는 `FactoryGame` 컴포넌트가 연결되어 있어야 한다.

Play Mode가 시작되면 다음을 확인한다.

1. 16×8 픽셀 잔디 두 종류가 무작위로 섞인 50×50 등각 Tilemap이 표시된다. 각 셀의 월드 크기는 1×0.5이므로 타일의 세로:가로 비율은 1:2이다.
2. 화면 아래에 10칸 핫바가 있고 첫 슬롯부터 컨베이어, 채굴기, 염색기, 조합기, 에르다 주입기, 가공기계 아이콘이 순서대로 표시된다.
3. 첫 슬롯을 클릭하면 테두리가 노란색으로 바뀌며 건설 모드가 된다.
4. 컨베이어·채굴기·염색기·조합기·에르다 주입기·가공기계 중 하나의 건설 도구를 선택하면 각 설치 셀 중심을 둘러싸는 마름모 경계에 화면상 약 1픽셀 굵기의 낮은 알파 검은색 격자선이 표시되고, 도구를 해제하면 사라진다. 격자는 맵 크기와 무관하게 꼭짓점 4개의 단일 메시로 그려진다.
5. 잔디 위에서 마우스 왼쪽 버튼을 누른 채 드래그하면 더 긴 축을 기준으로 수평 또는 수직 직선 미리보기만 표시된다. 버튼을 놓으면 컨베이어가 설치된다.
6. 이미 설치된 칸을 반대 방향으로 다시 드래그하면 해당 칸의 방향과 이미지가 새 방향으로 교체된다.
7. 출력 방향 바로 앞에 다른 컨베이어가 있으면 대상 컨베이어의 방향과 관계없이 연결된 출력 이미지를 사용한다. 따라서 U 방향 출력이 다른 컨베이어로 이어지면 `ConveyorUX`가 아니라 `ConveyorUU`가 표시된다. 연결되지 않은 끝은 `Conveyor?X`, 한 칸에서 바깥으로 향하는 이웃이 둘 이상이면 `Conveyor?A` 이미지가 표시된다. 현재 칸을 향하는 이웃이 정확히 하나라면 그 입력 방향과 출력 방향을 조합해 `ConveyorUR` 같은 회전 이미지를 사용한다. 여러 이웃이 한 칸으로 들어오는 합류는 도착 컨베이어 이미지를 바꾸지 않는다.
8. 컨베이어의 화면 방향은 `U=우측 상단`, `R=우측 하단`, `D=좌측 하단`, `L=좌측 상단`이다. 낮은 월드 y좌표의 컨베이어가 높은 y좌표의 컨베이어보다 앞에 그려진다. 같은 y좌표에서는 x좌표로 순서를 고정하므로 설치 순서나 재설치 횟수에 따라 겹침 순서가 바뀌지 않는다.
9. 건설 모드 여부와 관계없이 우클릭 드래그로 카메라를 패닝한다. `WASD` 또는 방향키로도 이동하며 마우스 휠로 확대/축소할 수 있다. `Esc`를 누르면 건설 모드가 해제된다. 염색기 레시피 모달이 열려 있는 동안에는 키보드 이동, 우클릭 패닝과 휠 줌이 모두 비활성화된다.
10. 파랑 염료 `(8, 8)`, 빨강 염료 `(41, 8)`, 노랑 염료 `(8, 41)`, 버섯 `(41, 41)`, 달팽이 `(25, 25)` 중심에 3×3 원재료가 표시된다.
11. 두 번째 슬롯을 클릭하면 채굴기 건설 모드가 된다. 포인터를 이동하면 `ExtractorU`부터 시작하는 반투명 고스트가 셀 중심에 표시되고, 원재료 중심과 정확히 일치할 때만 정상 색으로 표시된다.
12. 채굴기 건설 모드에서 `R`을 누르면 출력 방향이 `U → L → D → R → U` 순서로 반시계 회전한다. 원재료 중심을 좌클릭하면 해당 방향 채굴기가 설치되며 같은 원재료에는 두 번 설치할 수 없다.
13. 채굴기는 중심 기준 3×3 셀을 점유한다. 점유 셀에 컨베이어를 드래그하면 미리보기가 붉게 표시되고 선 전체가 설치되지 않는다. 반대로 기존 컨베이어 또는 다른 채굴기의 발자국과 3×3 영역이 한 칸이라도 겹치면 채굴기를 설치할 수 없다.
14. 채굴기 중심에서 출력 방향으로 두 칸 떨어진 셀에 컨베이어를 설치한다. 채굴기가 원재료에 대응하는 아이템을 첫 컨베이어 위에 0→1 스케일 애니메이션으로 생성한다. 생성이 끝나면 `(0.5, 0.25)` 피벗을 기준으로 컨베이어 상판에 맞춘 경로를 따라 0.45초마다 다음 컨베이어로 등속 이동한다. 아이템은 컨베이어보다 로컬 Z가 `0.3` 높다.
15. 두 컨베이어를 하나로 합쳐 아이템을 연속 공급하면 합류 승인이 입력별로 번갈아 적용된다. `Conveyor?A` 분배기에서는 `TrySelectNextOutput`의 라운드로빈 순서에 따라 출력들이 균등하게 선택된다.
16. 채굴기 출구가 향하는 첫 컨베이어는 출구 방향을 외부 입력으로 판정한다. 따라서 U 방향 채굴기에서 나온 첫 컨베이어가 R 방향으로 꺾이면 `ConveyorUR`이 표시된다. 실제 컨베이어 입력까지 여러 개가 합류하면 기존 규칙대로 별도 합류 이미지를 사용하지 않는다.
17. 컨베이어와 건물의 마스크 하단은 `ConveyorLevel`, 아이템과 건물 상단은 `ItemLevel` Sorting Layer를 사용한다. `ItemLevel`은 항상 `ConveyorLevel` 위에 그려진다. 채굴기는 `BuildingLowerMask.png`의 알파와 겹치는 부분만 Lower SpriteRenderer로, 나머지를 Upper SpriteRenderer로 표시하며 두 조각의 합은 원본과 같다.
18. 같은 Sorting Layer 안에서는 원재료, 채굴기 조각, 컨베이어와 이동 아이템이 Y 기반 정렬 규칙을 사용한다. 아이템은 매 프레임 보간된 격자 좌표와 `(0.5, 0.25)` Sprite 피벗을 정렬 기준으로 사용한다.
19. 세 번째 슬롯을 선택하면 3×3 염색기 고스트가 표시된다. `R`은 채굴기와 동일하게 출력 방향을 반시계 회전하며, 원재료·건물·컨베이어와 3×3 점유 영역이 겹치면 설치할 수 없다.
20. 사용자 좌표축은 `x+ = 화면 우하단`, `y+ = 화면 우상단`이다. 이 좌표계에서 U 방향 염색기의 중심이 `(0, 0)`일 때 내부 입력 포트는 `(-1, -1)`, `(1, -1)`, 내부 출력 포트는 `(0, 1)`이다. Unity 내부 격자로 변환한 포트는 각각 `(-1, 1)`, `(-1, -1)`, `(1, 0)`이며, 연결 컨베이어는 `(-2, 1)`, `(-2, -1)`, `(2, 0)`에 둔다. R/D/L은 이 배치를 화면 방향에 맞게 회전한다.
21. 새 염색기 위에는 어두운 월드 UI 배경의 `(레시피 선택)` 툴팁이 건물 상단에 가깝게 붙어 표시된다. 건설 도구를 해제한 뒤 건물을 좌클릭하면 Cafe24PROSlimFit TMP 폰트와 `RoundedRectangle.png` 9-slice로 만든 레시피 창이 열린다. 참조 UI와 같이 제목·닫기·구분선, `달팽이`/`버섯`/`뿔버섯` 아이콘 목록과 선택 테두리, 필요 재료 카드, `소요 시간 1.0초`, 선택 결과 하단 바와 `확인` 버튼을 표시한다. 레시피를 확정하면 툴팁이 사라진다.
22. 레시피는 빨강·파랑 달팽이 껍질, 파랑·주황·초록 버섯 갓, 파랑·주황·초록 뿔버섯 갓이다. 뿔버섯 원재료 생산자는 아직 없지만 레시피와 결과 아이템은 등록되어 있다.
23. 선택한 레시피의 바탕 재료와 염료를 두 입력 컨베이어로 공급하면 각 아이템은 내부 포트로 빠르게 이동하면서 0.12초 스케일 아웃되고, 두 재료가 모이면 결과가 출력 컨베이어 위에 0.12초 스케일 인으로 생성된다. 그 뒤의 컨베이어 이동 속도는 기존과 같이 셀당 0.45초이다.
24. 네 번째 슬롯은 3×3 조합기 건설 모드다. `R`로 출력 방향을 반시계 회전하며 입력 2개와 출력 1개의 위치·컨베이어 연결 규칙은 염색기와 같다. 원재료·건물·컨베이어와 점유 영역이 겹치면 설치할 수 없다.
25. 새 조합기에는 `(레시피 선택)` 툴팁이 표시된다. 조합기를 클릭하면 염색기와 동일한 공용 레시피 창이 `조합기` 제목과 `염료` 대분류로 열리고, 빨강+노랑→주황, 빨강+파랑→보라, 파랑+노랑→초록 중 하나를 선택할 수 있다. 레시피 확정 뒤 툴팁이 사라진다.
26. 선택한 두 원색 염료를 조합기의 두 입력 컨베이어로 공급하면 염색기와 같은 빠른 입력 소멸 연출 뒤 출력 컨베이어에 혼합 염료가 생성된다. 입력 순서는 결과에 영향을 주지 않는다.
27. 다섯 번째 슬롯은 1×1 에르다 주입기 건설 모드다. 방향 반대편 한 칸의 컨베이어가 입력이고 방향 앞 한 칸의 컨베이어가 출력이다. `R`로 방향을 회전하며 주입기 셀은 다른 건물·원재료·컨베이어와 겹칠 수 없다.
28. 입력 컨베이어가 주입기 방향을 향할 때 초록·빨강·파랑 달팽이 껍질, 파랑·주황·초록 버섯 갓, 파란 뿔버섯 갓을 넣으면 각각 대응하는 7종 몬스터 아이템이 출력 컨베이어에 0.12초 스케일 인으로 생성된다. External 원본의 `MonsterSpikeMushroomGray.png`는 실제 파란 뿔버섯 몬스터이므로 프로젝트에서는 `MonsterSpikeMushroomBlue`로 명명한다. 결과는 달팽이 껍질과 동일하게 셀당 0.45초로 이동하고 합류·분배·정렬 규칙을 공유한다. 출력 컨베이어가 없거나 점유되어 있으면 내부 결과를 보관하고 생산을 대기한다. 에르다 주입기는 별도의 레시피 선택 UI가 없고 등록되지 않은 아이템을 소비하지 않는다.
29. Combiner는 `BuildingLowerMask.png`, 32×64 에르다 주입기는 `BuildingLowerMask1x1.png`로 Lower/Upper를 각각 전처리한다. 두 건물과 모든 운송 아이템은 기존 `ConveyorLevel`/`ItemLevel` 및 Y 기반 정렬 규칙을 따른다.
30. 여섯 번째 슬롯은 3×3 가공기계 건설 모드다. 설치 방향 반대쪽 중앙에 입력 컨베이어 한 개, 설치 방향 중앙에 출력 컨베이어 한 개를 연결한다. `R`로 출력 방향을 반시계 회전하며 다른 3×3 건물과 동일한 점유·고스트·Lower/Upper 정렬 규칙을 사용한다.
31. 새 가공기계에는 `(레시피 선택)` 툴팁이 표시된다. 클릭하면 염색기·조합기와 같은 공용 레시피 창이 `가공기계` 제목과 `가공` 대분류로 열리고, 필요 재료에는 초록 달팽이 껍질 한 줄만 표시된다.
32. 초록 달팽이 껍질 한 개를 입력하면 빠른 스케일 아웃 뒤 `Horn.png` 뿔 아이템이 출력 컨베이어에 스케일 인으로 생성된다. 조합기의 `뿔버섯` 대분류에서 뿔 한 개와 기본 버섯 갓 한 개를 조합하면 염색 전 뿔버섯 갓이 생산된다.

Edit Mode 자동 테스트는 Test Runner에서 `Maptory.Factory.Tests` 어셈블리를 실행한다. 테스트는 기존 컨베이어·채굴·정렬 규칙과 함께 맵 크기와 무관한 격자 메시, 염색기와 조합기의 포트·점유·레시피·생산, 가공기계의 중앙 1입력·1출력과 뿔 생산, 조합기의 뿔버섯 생산, 에르다 주입기의 1×1 점유와 7종 운송 아이템 변환·출력 대기·후속 이동, 두 마스크로 생성한 Lower/Upper Sprite 및 런타임 에셋을 검증한다.

## 2. 기능 사용법

`FactoryGame`은 맵 표현과 입력/UI 조립을 담당하는 Scene 진입점이다. Main Camera가 `MainCamera` 태그를 가지고 있어야 한다. 모든 런타임 Sprite는 `Art/Resources/Factory` 아래에서 이름으로 로드된다. `FactorySpriteImporter`는 Point 필터와 16 PPU를 적용하고, 컨베이어·건물·아이템에는 `(0.5, 0.25)`, 원재료에는 중앙 피벗을 사용한다. 월드 객체는 `FactorySorting`의 명시적 Y 깊이 순서를 공유한다.

건물 원본이나 `Art/BuildingProcessing`의 마스크가 변경되면 `BuildingSpriteLayerGenerator`가 각 원본의 `Lower`와 `Upper` PNG를 `Art/Resources/Factory/Buildings/Generated`에 다시 만든다. 64×64 건물은 `BuildingLowerMask.png`, 32×64 1×1 건물은 `BuildingLowerMask1x1.png`를 사용한다. 수동 갱신은 Unity 메뉴 `Tools > Maptory > Regenerate Building Layers`를 사용한다.

검증 Scene의 Global Light 2D는 `Default`, `ConveyorLevel`, `ItemLevel`을 모두 대상으로 한다. 새 레이어를 추가할 때 Lit Sprite가 검게 표시되지 않도록 조명 대상에도 함께 등록해야 한다.

다른 시뮬레이션 기능은 Unity 표현 계층 대신 `ConveyorNetwork`를 사용한다. `GridDirection`은 각 컨베이어가 아이템을 받아 이동시키는 기본 방향이다. 현재 칸의 출력 후보는 인접 칸 중 현재 칸에서 바깥을 향하는 컨베이어이며, 입력의 반대 방향은 후보에서 제외된다.

```csharp
using Maptory.Factory;
using UnityEngine;

var network = new ConveyorNetwork();
network.PlaceLine(new Vector2Int(3, 4), new Vector2Int(8, 4));

if (network.TrySelectNextOutput(new Vector2Int(4, 4), out var output_direction))
{
    MoveItem(output_direction);
}
```

`PlaceLine`은 같은 x 또는 같은 y 좌표의 끝점만 받는다. 시작점과 끝점이 같으면 우측 상단(U) 방향 컨베이어 한 칸을 배치한다. 기존 칸을 포함하는 선을 배치하면 `SetConveyor`가 방향을 교체한다. `TrySelectNextOutput`은 가능한 출력들을 순환해 선택하므로 추후 아이템 운송 시스템에서 분배기의 공개 진입점으로 사용한다.

채굴과 운송은 `ExtractionNetwork`와 `FactoryItemTransport`가 소유한다. 채굴기는 반드시 등록된 원재료 중심에만 배치하며 출력 셀은 중심에서 방향 오프셋의 두 배만큼 떨어져 있다.

```csharp
var conveyors = new ConveyorNetwork();
var extraction = new ExtractionNetwork(new[]
{
    new RawMaterialDeposit(RawMaterialType.Mushroom, new Vector2Int(10, 10))
}, conveyors);
extraction.PlaceExtractor(new Vector2Int(10, 10), GridDirection.Up);

conveyors.PlaceLine(new Vector2Int(12, 10), new Vector2Int(18, 10));
conveyors.AddExternalInput(new Vector2Int(12, 10), GridDirection.Up);
var transport = new FactoryItemTransport(conveyors, extraction);
transport.Update(Time.deltaTime);
```

염색기는 `ExtractionNetwork.PlaceDyeingMachine`으로 설치하고 `DyeingRecipe.All`의 정식 레시피 객체를 선택한다. 입력 컨베이어는 `GetInputConveyorPosition(0/1)`, 출력은 `OutputConveyorPosition`으로 조회한다.

```csharp
var machine = extraction.PlaceDyeingMachine(new Vector2Int(20, 20), GridDirection.Up);
machine.SelectRecipe(DyeingRecipe.All[DyeingRecipeId.MushroomBlue]);

var first_input = machine.GetInputConveyorPosition(0);
var second_input = machine.GetInputConveyorPosition(1);
var output = machine.OutputConveyorPosition;
```

조합기는 염색기와 같은 `IRecipeMachine` 계약을 구현하므로 입력 포트와 생산 흐름을 동일하게 연결한다. 에르다 주입기는 방향 반대편 입력 컨베이어에서 유효 아이템 하나를 소비하고 방향 앞 출력 컨베이어에 일반 `FactoryItemState` 결과를 생성한다.

```csharp
var combiner = extraction.PlaceCombiner(new Vector2Int(20, 20), GridDirection.Up);
combiner.SelectRecipe(CombiningRecipe.All[CombiningRecipeId.DyePurple]);

var injector = extraction.PlaceErdaInjector(new Vector2Int(30, 20), GridDirection.Up);
conveyors.SetConveyor(injector.InputConveyorPosition, GridDirection.Up);
conveyors.SetConveyor(injector.OutputConveyorPosition, GridDirection.Up);
var transport = new FactoryItemTransport(conveyors, extraction);
transport.SpawnItem(RawMaterialType.SnailRed, injector.InputConveyorPosition);
transport.Step();
transport.Step();
var spawned_item = transport.Items[0];
```

`IRecipe`는 재료 목록을 소유하므로 공용 레시피 UI와 운송 시스템은 1재료 가공기계와 2재료 염색기·조합기를 함께 처리한다. 가공기계는 중앙 입력 한 개만 사용한다.

```csharp
var processor = extraction.PlaceProcessingMachine(
    new Vector2Int(24, 20),
    GridDirection.Up);
processor.SelectRecipe(ProcessingRecipe.All[ProcessingRecipeId.Horn]);

conveyors.SetConveyor(processor.GetInputConveyorPosition(0), GridDirection.Up);
conveyors.SetConveyor(processor.OutputConveyorPosition, GridDirection.Up);
```

## 3. 코드 구조와 책임

| 파일 | 책임 |
| --- | --- |
| `FactoryGame.cs` | 맵, 5종 고정 원재료, 여섯 건설 도구, 채굴·가공·운송 시스템, UI와 카메라를 조립하는 Scene 진입점 |
| `GridDirection.cs` | 4방향 값과 격자 오프셋, Sprite 코드, 반대 방향 및 반시계 회전 정의 |
| `ConveyorNetwork.cs` | 컨베이어 방향 상태, 직선 배치/덮어쓰기, 건물 입출력을 포함한 연결 분석, Sprite 이름과 출력 분배 순서 소유 |
| `ExtractionNetwork.cs` | 아이템·가변 재료 공용 레시피 계약, 원재료와 채굴기·염색기·조합기·가공기계·에르다 주입기 상태, 3×3/1×1 점유와 포트 좌표, 설치 이벤트 소유 |
| `FactoryItemTransport.cs` | 채굴 생산, 염색·조합·가공·에르다 입력 소비와 출력, 컨베이어 이동·역압·합류·분배 시뮬레이션 소유 |
| `ErdaInjectionRecipes.cs` | 에르다 주입기가 받는 7종 재료와 대응하는 몬스터 운송 아이템 정의 |
| `FactorySorting.cs` | 컨베이어·아이템 레벨 Sorting Layer 이름, 결정적 Y/X 정렬 순서와 높이 Z를 포함한 투명 정렬 축 정의 |
| `FactoryBuildMode.cs` | 핫바 도구 선택과 `Esc` 해제를 단일 상태로 관리 |
| `ConstructionGridOverlay.cs` | 건설 모드 동안 맵 크기와 무관한 단일 메시와 화면 픽셀 굵기 셰이더로 아이소메트릭 격자선을 표시 |
| `Art/Resources/Factory/Construction/ConstructionGridOverlay.shader` | 단일 메시의 보간된 격자 좌표로 셀 경계와 화면상 일정한 선 굵기를 계산 |
| `FactoryTileCatalog.cs` | 잔디, 컨베이어, 원재료, 건물 원본·Lower·Upper, 일반·몬스터 아이템·UI Sprite와 런타임 TMP 폰트 조회 제공 |
| `ConveyorBuilder.cs` | 핫바가 켠 건설 모드에서 포인터를 격자에 투영하고 건물 점유를 검증한 직선 미리보기·배치를 수행하며 셀별 컨베이어 SpriteRenderer를 갱신 |
| `ExtractorBuilder.cs` | 원재료 표현, 방향 고스트, 중심 일치 검증과 채굴기 Lower/Upper SpriteRenderer 생성 |
| `DyeingMachineBuilder.cs` | 염색기 방향 고스트·설치·클릭, Lower/Upper 렌더러와 미선택 툴팁 생성 |
| `CombinerBuilder.cs` | 3×3 조합기 방향 고스트·설치·클릭, Lower/Upper 렌더러와 미선택 툴팁 생성 |
| `ErdaInjectorBuilder.cs` | 1×1 에르다 주입기 방향 고스트·설치 및 전용 마스크 Lower/Upper 렌더러 생성 |
| `ProcessingMachineBuilder.cs` | 3×3 가공기계 방향 고스트·설치·클릭, Lower/Upper 렌더러와 미선택 툴팁 생성 |
| `RecipeSelectionPanel.cs` | 염색기·조합기·가공기계가 공유하는 TMP 기반 레시피 모달, 동적 대분류·1~2개 필요 재료·결과 표시와 확정 처리 |
| `RecipeTooltip.cs` | 레시피 기반 건물이 공유하는 `(레시피 선택)` 월드 UI 생성 |
| `FactoryItemTransportView.cs` | 운송 상태를 선형 보간해 아이템 위치·빠른 입출력 스케일·실시간 깊이를 갱신하고 소비된 렌더러 제거 |
| `FactoryHotbar.cs` | 화면 하단 10슬롯 UI, 여섯 건설 도구의 선택 상태와 클릭 이벤트 제공 |
| `FactoryCameraController.cs` | 키보드 이동, 우클릭 패닝, 휠 확대/축소와 맵 범위 제한을 담당하고 모달 입력 차단 함수를 적용 |
| `Editor/FactorySpriteImporter.cs` | 기능 전용 픽셀 아트의 Sprite import 설정 고정 |
| `Editor/BuildingSpriteLayerGenerator.cs` | 3×3/1×1 하단 마스크를 건물 폭에 맞춰 선택해 Lower/Upper PNG로 전처리하고 변경 시 재생성 |
| `Tests/EditMode/ConveyorNetworkTests.cs` | 컨베이어 연결 및 분배 규칙의 Edit Mode 회귀 테스트 |
| `Tests/EditMode/ExtractionAndTransportTests.cs` | 채굴기 배치·회전·생산, 운송·합류 및 정렬 규칙 회귀 테스트 |
| `Tests/EditMode/ConstructionGridOverlayTests.cs` | 초대형 맵에서도 메시 크기가 일정하고 건설 모드에만 표시되는지 검증 |
| `Tests/EditMode/CombinerAndErdaInjectorTests.cs` | 3종 염료 조합, 3×3/1×1 점유, 7종 몬스터 아이템 변환·출력 대기·이동과 신규 런타임 Sprite 검증 |
| `Tests/EditMode/ProcessingMachineTests.cs` | 가공기계 중앙 포트·뿔 생산, 조합기 뿔버섯 레시피와 신규 Sprite 검증 |

`ConveyorNetwork`와 `ExtractionNetwork`가 영속 가능한 게임 상태를 소유한다. `FactoryItemTransport`는 두 네트워크만 참조하고 Unity UI나 Renderer에 의존하지 않는다. 건설 Builder와 `FactoryItemTransportView`가 입력과 표현을 담당하며 레시피 UI는 선택 결과만 `DyeingMachineState`에 전달한다.

현재 뿔버섯 원재료 생산자, 컨베이어 및 건물 철거, 저장은 구현되어 있지 않다. 에르다 결과는 별도 월드 개체가 아니라 기존 아이템 운송 상태를 사용하므로 막힌 끝 X 규칙과 라운드로빈 합류·분배를 그대로 따른다.
