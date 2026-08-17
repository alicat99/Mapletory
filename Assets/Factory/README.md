# Factory Map, Extraction, and Conveyors

## 1. 기능 검증 방법

`Assets/Scenes/SampleScene.unity`을 열고 Play Mode에 진입한다. Scene에는 `Main Camera`, `Global Light 2D`, `Factory Game` 오브젝트가 있어야 하며, `Factory Game`에는 `FactoryGame` 컴포넌트가 연결되어 있어야 한다.

Play Mode가 시작되면 다음을 확인한다.

1. 16×8 픽셀 잔디 두 종류가 무작위로 섞인 50×50 등각 Tilemap이 표시된다. 각 셀의 월드 크기는 1×0.5이므로 타일의 세로:가로 비율은 1:2이다.
2. 화면 아래에 10칸 핫바가 있고 첫 슬롯에는 컨베이어, 두 번째 슬롯에는 채굴기 아이콘이 표시된다.
3. 첫 슬롯을 클릭하면 테두리가 노란색으로 바뀌며 건설 모드가 된다.
4. 잔디 위에서 마우스 왼쪽 버튼을 누른 채 드래그하면 더 긴 축을 기준으로 수평 또는 수직 직선 미리보기만 표시된다. 버튼을 놓으면 컨베이어가 설치된다.
5. 이미 설치된 칸을 반대 방향으로 다시 드래그하면 해당 칸의 방향과 이미지가 새 방향으로 교체된다.
6. 출력 방향 바로 앞에 다른 컨베이어가 있으면 대상 컨베이어의 방향과 관계없이 연결된 출력 이미지를 사용한다. 따라서 U 방향 출력이 다른 컨베이어로 이어지면 `ConveyorUX`가 아니라 `ConveyorUU`가 표시된다. 연결되지 않은 끝은 `Conveyor?X`, 한 칸에서 바깥으로 향하는 이웃이 둘 이상이면 `Conveyor?A` 이미지가 표시된다. 현재 칸을 향하는 이웃이 정확히 하나라면 그 입력 방향과 출력 방향을 조합해 `ConveyorUR` 같은 회전 이미지를 사용한다. 여러 이웃이 한 칸으로 들어오는 합류는 도착 컨베이어 이미지를 바꾸지 않는다.
7. 컨베이어의 화면 방향은 `U=우측 상단`, `R=우측 하단`, `D=좌측 하단`, `L=좌측 상단`이다. 낮은 월드 y좌표의 컨베이어가 높은 y좌표의 컨베이어보다 앞에 그려진다. 같은 y좌표에서는 x좌표로 순서를 고정하므로 설치 순서나 재설치 횟수에 따라 겹침 순서가 바뀌지 않는다.
8. 건설 모드 여부와 관계없이 우클릭 드래그로 카메라를 패닝한다. `WASD` 또는 방향키로도 이동하며 마우스 휠로 확대/축소할 수 있다. `Esc`를 누르면 건설 모드가 해제된다.
9. 파랑 염료 `(8, 8)`, 빨강 염료 `(41, 8)`, 노랑 염료 `(8, 41)`, 버섯 `(41, 41)`, 달팽이 `(25, 25)` 중심에 3×3 원재료가 표시된다.
10. 두 번째 슬롯을 클릭하면 채굴기 건설 모드가 된다. 포인터를 이동하면 `ExtractorU`부터 시작하는 반투명 고스트가 셀 중심에 표시되고, 원재료 중심과 정확히 일치할 때만 정상 색으로 표시된다.
11. 채굴기 건설 모드에서 `R`을 누르면 출력 방향이 `U → L → D → R → U` 순서로 반시계 회전한다. 원재료 중심을 좌클릭하면 해당 방향 채굴기가 설치되며 같은 원재료에는 두 번 설치할 수 없다.
12. 채굴기는 중심 기준 3×3 셀을 점유한다. 점유 셀에 컨베이어를 드래그하면 미리보기가 붉게 표시되고 선 전체가 설치되지 않는다. 반대로 기존 컨베이어 또는 다른 채굴기의 발자국과 3×3 영역이 한 칸이라도 겹치면 채굴기를 설치할 수 없다.
13. 채굴기 중심에서 출력 방향으로 두 칸 떨어진 셀에 컨베이어를 설치한다. 채굴기가 원재료에 대응하는 아이템을 첫 컨베이어 위에 0→1 스케일 애니메이션으로 생성한다. 생성이 끝나면 `(0.5, 0.25)` 피벗을 기준으로 컨베이어 상판에 맞춘 경로를 따라 0.45초마다 다음 컨베이어로 등속 이동한다. 아이템은 컨베이어보다 로컬 Z가 `0.3` 높고, 카메라의 Y/Z 투명 정렬 축이 이 높이를 깊이 판정에 반영한다.
14. 두 컨베이어를 하나로 합쳐 아이템을 연속 공급하면 합류 승인이 입력별로 번갈아 적용된다. `Conveyor?A` 분배기에서는 `TrySelectNextOutput`의 라운드로빈 순서에 따라 출력들이 균등하게 선택된다.
15. 채굴기 출구가 향하는 첫 컨베이어는 출구 방향을 외부 입력으로 판정한다. 따라서 U 방향 채굴기에서 나온 첫 컨베이어가 R 방향으로 꺾이면 `ConveyorUR`이 표시된다. 실제 컨베이어 입력까지 여러 개가 합류하면 기존 규칙대로 별도 합류 이미지를 사용하지 않는다.
16. 원재료, 채굴기, 컨베이어, 이동 아이템은 모두 같은 Y 기반 정렬 규칙을 사용한다. 아이템은 매 프레임 보간된 격자 좌표와 `(0.5, 0.25)` Sprite 피벗을 정렬 기준으로 사용하므로 타일 경계를 지날 때도 자연스럽게 앞뒤가 바뀐다.

Edit Mode 자동 테스트는 Test Runner에서 `Maptory.Factory.Tests` 어셈블리를 실행한다. 테스트는 기존 컨베이어 규칙과 함께 채굴기 3×3 점유, 건물·컨베이어 중첩 방지, 반시계 회전, 출력 위치, 원재료별 Sprite 매핑, 출력 컨베이어 대기, 첫 컨베이어 생성과 이동, 선형 진행률, 채굴기 출구 입력 Sprite, 공정 합류 선택, Y 정렬 우선순위를 검증한다.

## 2. 기능 사용법

`FactoryGame`은 맵 표현과 입력/UI 조립을 담당하는 Scene 진입점이다. Main Camera가 `MainCamera` 태그를 가지고 있어야 한다. 모든 Sprite는 `Art/Resources/Factory` 아래에서 이름으로 로드된다. `FactorySpriteImporter`는 Point 필터와 16 PPU를 적용하고, 컨베이어·건물·아이템에는 `(0.5, 0.25)`, 원재료에는 중앙 피벗을 사용한다. 월드 객체는 `FactorySorting`의 명시적 Y 깊이 순서를 공유한다.

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

## 3. 코드 구조와 책임

| 파일 | 책임 |
| --- | --- |
| `FactoryGame.cs` | 맵, 5종 고정 원재료, 건설 도구, 채굴·운송 시스템, UI와 카메라를 조립하는 Scene 진입점 |
| `GridDirection.cs` | 4방향 값과 격자 오프셋, Sprite 코드, 반대 방향 및 반시계 회전 정의 |
| `ConveyorNetwork.cs` | 컨베이어 방향 상태, 직선 배치/덮어쓰기, 채굴기 출구를 포함한 연결 분석, Sprite 이름과 출력 분배 순서 소유 |
| `ExtractionNetwork.cs` | 원재료 종류·고정 중심, 채굴기 3×3 점유와 중첩 검증, 설치 상태·출력 위치와 설치 이벤트 소유 |
| `FactoryItemTransport.cs` | 채굴 생산 주기, 컨베이어 이동, 역압, 공정 합류와 분배 시뮬레이션 소유 |
| `FactorySorting.cs` | 컨베이어·원재료·건물·아이템이 공유하는 결정적 Y/X 정렬 순서와 높이 Z를 포함한 투명 정렬 축 정의 |
| `FactoryBuildMode.cs` | 핫바 도구 선택과 `Esc` 해제를 단일 상태로 관리 |
| `FactoryTileCatalog.cs` | 잔디, 컨베이어, 원재료, 채굴기, 아이템 Sprite와 미리보기 Tile 조회 제공 |
| `ConveyorBuilder.cs` | 핫바가 켠 건설 모드에서 포인터를 격자에 투영하고 건물 점유를 검증한 직선 미리보기·배치를 수행하며 셀별 컨베이어 SpriteRenderer를 갱신 |
| `ExtractorBuilder.cs` | 원재료 표현, 방향 고스트, 중심 일치 검증과 채굴기 SpriteRenderer 생성 |
| `FactoryItemTransportView.cs` | 운송 상태를 선형 보간해 아이템 SpriteRenderer 위치·생성 스케일·실시간 깊이를 갱신 |
| `FactoryHotbar.cs` | 화면 하단 10슬롯 UI, 컨베이어·채굴기 선택 상태와 도구 클릭 이벤트 제공 |
| `FactoryCameraController.cs` | 키보드 이동, 우클릭 패닝, 휠 확대/축소와 맵 범위 제한 담당 |
| `Editor/FactorySpriteImporter.cs` | 기능 전용 픽셀 아트의 Sprite import 설정 고정 |
| `Tests/EditMode/ConveyorNetworkTests.cs` | 컨베이어 연결 및 분배 규칙의 Edit Mode 회귀 테스트 |
| `Tests/EditMode/ExtractionAndTransportTests.cs` | 채굴기 배치·회전·생산, 운송·합류 및 정렬 규칙 회귀 테스트 |

`ConveyorNetwork`와 `ExtractionNetwork`가 영속 가능한 게임 상태를 소유한다. `FactoryItemTransport`는 두 네트워크만 참조하고 Unity UI나 Renderer에 의존하지 않는다. `ConveyorBuilder`, `ExtractorBuilder`, `FactoryItemTransportView`가 입력과 표현을 담당하며 UI는 `FactoryBuildMode`만 변경한다.

현재 채굴기 외의 건물, 아이템 소비·가공, 컨베이어 및 건물 철거, 저장은 구현되어 있지 않다. 후속 건물은 출력 후보와 아이템 목적지를 제공하되, 막힌 끝의 X 규칙과 라운드로빈 합류·분배는 현재 시뮬레이션 경계 안에 유지한다.
