# 건물 입출력 표시

## 1. 기능 검증 방법

1번 컨베이어를 드래그하면 프리뷰 선의 시작과 끝에 입력/출력 아이콘이 나타나야 한다. 2~7번 건설 도구를 선택하고 마우스를 이동하면 고스트 주변 실제 컨베이어 연결 셀에 입력/출력 아이콘이 나타나야 한다. R로 회전하면 건물 스프라이트와 포트가 함께 회전해야 한다. 건설 모드를 끝내거나 UI 위로 포인터를 옮기면 아이콘이 사라지고 기존 건물이나 컨베이어를 클릭해도 다시 나타나지 않아야 한다.

## 2. 기능 사용법

`FactoryGame`이 오버레이를 자동 생성한다. 아이콘은 `Resources/Factory/BuildingPorts/InputIcon.png`와 `OutputIcon.png`의 2×2 방향 시트를 16×16로 슬라이스해 사용하므로 Inspector 연결이 없다. 방향별 조각은 `U=우상`, `R=우하`, `D=좌하`, `L=좌상`으로 직접 매핑하며 Output 조각의 뾰족한 끝이 항상 건물 바깥을 향한다.

```csharp
port_overlay.Initialize(camera, grid, world_root, build_mode, conveyor_builder, extraction);
```

## 3. 코드 구조와 책임

| 파일 | 책임 |
|---|---|
| `FactoryBuildingPortOverlay.cs` | 건설 미리보기 상태의 포트 좌표를 아이콘으로 표시 |
| `Resources/Factory/BuildingPorts/InputIcon.png` | 4방향 입력 표시 스프라이트 시트 |
| `Resources/Factory/BuildingPorts/OutputIcon.png` | 4방향 출력 표시 스프라이트 시트 |
| `../Tests/EditMode/BuildingPortOverlayTests.cs` | 방향별 시트 슬라이스 좌표 검증 |

방향과 포트 위치는 `ConveyorBuilder`의 프리뷰 구간과 `ExtractorState`, `IRecipeMachine`, `ErdaInjectorState`, `PortalState`에서 읽는다. UI에는 건물별 방향 좌표를 별도 하드코딩하지 않는다.
