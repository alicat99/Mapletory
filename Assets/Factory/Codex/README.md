# 제작 도감

## 1. 기능 검증 방법

스테이지에서 E를 눌러 도감을 열고 `몬스터 / 염색기 / 가공시설 / 조합기 / 원재료` 책갈피 탭을 전환한다. 몬스터를 선택하면 에르다 주입기부터 원재료까지 제작 과정이 재귀적으로 표시되어야 한다. 재료 버튼으로 하위 제작법에 들어간 뒤 `이전 제작법`으로 탐색 상태가 복구되는지 확인한다. 잠긴 몬스터는 잠금 표시를 유지하되 목록에서 사라지지 않아야 한다.

`TutorialAndCodexTests.CodexRecursivelyCoversEveryRecipeIngredient`가 모든 재료의 도감 엔트리 연결을 검증한다.

## 2. 기능 사용법

도감은 런타임에서 생성되며 E로 토글한다. 목표나 다른 UI는 다음처럼 특정 항목으로 바로 열 수 있다.

```csharp
codex.Open(RawMaterialType.MonsterSnailGreen);
```

## 3. 코드 구조와 책임

| 파일 | 책임 |
|---|---|
| `FactoryContentCatalog.cs` | 기존 레시피로부터 도감 항목과 공통 건물 설명 제공 |
| `FactoryCodexPanel.cs` | 탭, 항목 목록, 재귀 제작 과정과 history UI |

레시피와 이름은 기존 `DyeingRecipe`, `CombiningRecipe`, `ProcessingRecipe`, `ErdaInjectionRecipes`, `RawMaterialTypeExtensions`를 재사용한다.
