# 튜토리얼

## 1. 기능 검증 방법

`SampleScene`을 실행하고 1스테이지에 입장한다. 새 진행 데이터에서는 상단 튜토리얼이 자동으로 시작한다. 우클릭 드래그, 휠 줌, 컨베이어 선택, 회전 가능한 건물 선택 후 R, 컨베이어 설치, X, 실제 철거를 순서대로 수행한다. 각 행동 직후에만 다음 단계로 이동하고 마지막 건물 설명을 확인하면 화면 우측 하단에 다시보기 버튼이 나타나야 한다.

`TutorialAndCodexTests.TutorialOnlyAdvancesForTheExpectedRealAction`은 잘못된 입력이 단계를 넘기지 않는지와 전체 행동 순서를 검증한다. 기능 안내는 튜토리얼 종료 후 레시피 시설, 포탈, U 업그레이드, E 도감을 각각 처음 열 때 한 번만 표시되는지 확인한다.

## 2. 기능 사용법

런타임 구성은 `FactoryGame`이 담당하므로 Inspector 연결이 필요 없다. 진행 상태는 `FactoryProgression.Tutorial`에 있으며 현재 실행 세션에만 유지된다.

```csharp
var seen = progression.Tutorial.HasSeen("codex");
progression.Tutorial.MarkSeen("codex");
progression.MarkChanged();
```

## 3. 코드 구조와 책임

| 파일 | 책임 |
|---|---|
| `FactoryTutorialProgressData.cs` | 현재 실행 중 완료 단계와 최초 접근 안내 이력 소유 |
| `FactoryTutorialTracker.cs` | 행동 순서와 단계 완료 조건 판정 |
| `FactoryTutorialSystem.cs` | 게임 이벤트 구독, 안내 UI, 건너뛰기와 다시보기 |

카메라·건설·회전·철거 시스템은 실제 행동 이벤트를 발행하고, 튜토리얼은 이를 구독만 한다. 건물 설명은 `FactoryContentCatalog.Buildings`를 사용한다.
