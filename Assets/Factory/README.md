# Factory Map and Conveyors

## 1. 기능 검증 방법

`Assets/Scenes/SampleScene.unity`을 열고 Play Mode에 진입한다. Scene에는 `Main Camera`, `Global Light 2D`, `Factory Game` 오브젝트가 있어야 하며, `Factory Game`에는 `FactoryGame` 컴포넌트가 연결되어 있어야 한다.

Play Mode가 시작되면 다음을 확인한다.

1. 16×8 픽셀 잔디 두 종류가 무작위로 섞인 50×50 등각 Tilemap이 표시된다. 각 셀의 월드 크기는 1×0.5이므로 타일의 세로:가로 비율은 1:2이다.
2. 화면 아래에 10칸 핫바가 있고 첫 슬롯에 컨베이어 아이콘이 표시된다.
3. 첫 슬롯을 클릭하면 테두리가 노란색으로 바뀌며 건설 모드가 된다.
4. 잔디 위에서 마우스 왼쪽 버튼을 누른 채 드래그하면 더 긴 축을 기준으로 수평 또는 수직 직선 미리보기만 표시된다. 버튼을 놓으면 컨베이어가 설치된다.
5. 이미 설치된 칸을 반대 방향으로 다시 드래그하면 해당 칸의 방향과 이미지가 새 방향으로 교체된다.
6. 연결되지 않은 끝은 `Conveyor?X`, 한 칸에서 바깥으로 향하는 이웃이 둘 이상이면 `Conveyor?A` 이미지가 표시된다. 여러 이웃이 한 칸으로 들어오는 합류는 도착 컨베이어 이미지를 바꾸지 않는다.
7. `WASD` 또는 방향키로 카메라를 이동하고 마우스 휠로 확대/축소할 수 있다. `Esc`를 누르면 건설 모드가 해제된다.

Edit Mode 자동 테스트는 Test Runner에서 `Maptory.Factory.Tests` 어셈블리를 실행한다. 테스트는 직선 배치와 끝 이미지, 대각선 거부, 기존 방향 덮어쓰기, 다중 출력의 A 이미지, 다중 입력의 무변형, 출력 라운드로빈 분배, 입력 반대 방향으로의 역류 방지를 검증한다.

## 2. 기능 사용법

`FactoryGame`은 맵 표현과 입력/UI 조립을 담당하는 Scene 진입점이다. Main Camera가 `MainCamera` 태그를 가지고 있어야 한다. 잔디와 컨베이어 Sprite는 `Art/Resources/Factory` 아래에서 이름으로 로드되며, `FactorySpriteImporter`가 Point 필터, 16 PPU와 컨베이어용 피벗을 적용한다.

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

`PlaceLine`은 같은 x 또는 같은 y 좌표의 끝점만 받는다. 시작점과 끝점이 같으면 오른쪽 방향 컨베이어 한 칸을 배치한다. 기존 칸을 포함하는 선을 배치하면 `SetConveyor`가 방향을 교체한다. `TrySelectNextOutput`은 가능한 출력들을 순환해 선택하므로 추후 아이템 운송 시스템에서 분배기의 공개 진입점으로 사용한다.

## 3. 코드 구조와 책임

| 파일 | 책임 |
| --- | --- |
| `FactoryGame.cs` | 50×50 잔디 Tilemap을 만들고 컨베이어 표현, 핫바, 카메라를 조립하는 Scene 진입점 |
| `GridDirection.cs` | 4방향 값과 격자 오프셋, Sprite 코드, 반대 방향 변환 정의 |
| `ConveyorNetwork.cs` | 컨베이어 방향 상태, 직선 배치/덮어쓰기, 연결 분석, Sprite 이름과 출력 분배 순서 소유 |
| `FactoryTileCatalog.cs` | Resources의 잔디/컨베이어 Sprite를 런타임 Tile로 변환하고 조회 |
| `ConveyorBuilder.cs` | 핫바가 켠 건설 모드에서 포인터를 격자에 투영하고 직선 미리보기와 배치 수행 |
| `FactoryHotbar.cs` | 화면 하단 10슬롯 UI, 첫 슬롯 선택 상태와 건설 클릭 이벤트 제공 |
| `FactoryCameraController.cs` | 키보드 이동, 휠 확대/축소와 맵 범위 제한 담당 |
| `Editor/FactorySpriteImporter.cs` | 기능 전용 픽셀 아트의 Sprite import 설정 고정 |
| `Tests/EditMode/ConveyorNetworkTests.cs` | 컨베이어 연결 및 분배 규칙의 Edit Mode 회귀 테스트 |

`ConveyorNetwork`가 정식 시뮬레이션 상태를 소유하고 `ConveyorBuilder`가 명령을 전달한다. `FactoryGame`은 상태를 Tilemap으로 표현하며 UI는 네트워크 내부를 직접 참조하지 않는다. 향후 아이템 운송은 `TrySelectNextOutput`을 통해 네트워크에 의존하고, 네트워크는 아이템이나 Unity UI에 의존하지 않는다.

현재 컨베이어 위의 아이템 이동 애니메이션, 다른 건물 연결, 저장은 구현되어 있지 않다. 건물 연결이 추가되면 건물을 출력 후보로 제공하되, 막힌 끝의 X 규칙과 컨베이어 간 A 분배 규칙은 `ConveyorNetwork` 경계 안에 유지한다.
