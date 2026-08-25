# Factory Map, Extraction, Conveyors, and Processing

## 1. 기능 검증 방법

`Assets/Scenes/SampleScene.unity`을 열고 Play Mode에 진입한다. Scene에는 `Main Camera`, `Global Light 2D`, `Factory Game` 오브젝트가 있어야 하며, `Factory Game`에는 `FactoryGame` 컴포넌트가 연결되어 있어야 한다.

Play Mode가 시작되면 스테이지 선택 화면에 2개 스테이지가 표시되고 1스테이지만 처음부터 입장 가능한지 확인한다. 잠긴 스테이지는 해금 비용과 구매 팝업을 표시하며, 메소가 충분할 때만 영구 해금 후 입장한다. 1스테이지에 입장한 뒤 다음을 확인한다.

1. 16×8 픽셀 잔디 두 종류가 무작위로 섞인 50×50 등각 Tilemap이 표시된다. 각 셀의 월드 크기는 1×0.5이므로 타일의 세로:가로 비율은 1:2이다.
2. 화면 아래에 10칸 핫바가 있고 `1 컨베이어 / 2 추출기 / 3 에르다 주입기 / 4 염색기 / 5 조합기 / 6 가공기계 / 7 포탈` 순서로 표시된다. 상단 숫자키 `1`~`9`, `0`은 각각 1~10번째 슬롯을 클릭한 것과 같은 선택·해제 동작을 하며 빈 슬롯은 무시한다.
3. 첫 슬롯을 클릭하면 테두리가 노란색으로 바뀌며 건설 모드가 된다.
4. 컨베이어·채굴기·염색기·조합기·에르다 주입기·가공기계 중 하나의 건설 도구를 선택하면 각 설치 셀 중심을 둘러싸는 마름모 경계에 화면상 약 1픽셀 굵기의 낮은 알파 검은색 격자선이 표시되고, 도구를 해제하면 사라진다. 격자는 맵 크기와 무관하게 꼭짓점 4개의 단일 메시로 그려진다.
5. 잔디 위에서 마우스 왼쪽 버튼을 누른 채 드래그하면 더 긴 축을 기준으로 수평 또는 수직 직선 미리보기만 표시된다. 버튼을 놓으면 컨베이어가 설치된다.
6. 이미 설치된 칸을 반대 방향으로 다시 드래그하면 해당 칸의 방향과 이미지가 새 방향으로 교체된다.
7. 출력 방향 바로 앞에 다른 컨베이어가 있으면 대상 컨베이어의 방향과 관계없이 연결된 출력 이미지를 사용한다. 따라서 U 방향 출력이 다른 컨베이어로 이어지면 `ConveyorUX`가 아니라 `ConveyorUU`가 표시된다. 연결되지 않은 끝은 `Conveyor?X`, 한 칸에서 바깥으로 향하는 이웃이 둘 이상이면 `Conveyor?A` 이미지가 표시된다. 현재 칸을 향하는 이웃이 정확히 하나라면 그 입력 방향과 출력 방향을 조합해 `ConveyorUR` 같은 회전 이미지를 사용한다. 여러 이웃이 한 칸으로 들어오는 합류는 도착 컨베이어 이미지를 바꾸지 않는다.
8. 컨베이어의 화면 방향은 `U=우측 상단`, `R=우측 하단`, `D=좌측 하단`, `L=좌측 상단`이다. 낮은 월드 y좌표의 컨베이어가 높은 y좌표의 컨베이어보다 앞에 그려진다. 같은 y좌표에서는 x좌표로 순서를 고정하므로 설치 순서나 재설치 횟수에 따라 겹침 순서가 바뀌지 않는다.
9. 건설 모드 여부와 관계없이 우클릭 드래그로 카메라를 패닝한다. `WASD` 또는 방향키로도 이동하며 마우스 휠로 확대/축소할 수 있다. `Esc`를 누르면 건설 모드가 해제된다. 염색기 레시피 모달이 열려 있는 동안에는 키보드 이동, 우클릭 패닝과 휠 줌이 모두 비활성화된다.
10. Stage1에는 좌측 하단 달팽이 6개, 좌측 상단 빨간 염료 3개, 우측 상단 파란 염료 3개가 표시된다. Stage2에는 상단 버섯 6개, 좌측 상단 빨간 염료 4개, 우측 상단 파란 염료 3개, 좌측 하단 노란 염료 6개, 하단 달팽이 3개가 표시된다. 각 노드는 독립된 3×3 원재료이며 같은 군락 안의 중심 간격은 3셀이다.
11. 두 번째 슬롯을 클릭하면 채굴기 건설 모드가 된다. 포인터를 이동하면 `ExtractorU`부터 시작하는 반투명 고스트가 셀 중심에 표시되고, 원재료 중심과 정확히 일치할 때만 정상 색으로 표시된다.
12. 채굴기 건설 모드에서 `R`을 누르면 출력 방향이 `U → L → D → R → U` 순서로 반시계 회전한다. 원재료 중심을 좌클릭하면 해당 방향 채굴기가 설치되며 같은 원재료에는 두 번 설치할 수 없다.
13. 채굴기는 중심 기준 3×3 셀을 점유한다. 점유 셀에 컨베이어를 드래그하면 미리보기가 붉게 표시되고 선 전체가 설치되지 않는다. 반대로 기존 컨베이어 또는 다른 채굴기의 발자국과 3×3 영역이 한 칸이라도 겹치면 채굴기를 설치할 수 없다.
14. 채굴기 중심에서 출력 방향으로 두 칸 떨어진 셀에 컨베이어를 설치한다. 채굴기는 원재료에 대응하는 아이템을 정확히 1초마다 1개씩 첫 컨베이어 위에 0→1 스케일 애니메이션으로 생성한다. 생성이 끝나면 `(0.5, 0.25)` 피벗을 기준으로 컨베이어 상판에 맞춘 경로를 따라 0.45초마다 다음 컨베이어로 등속 이동한다. 아이템은 컨베이어보다 로컬 Z가 `0.3` 높다.
15. 두 컨베이어를 하나로 합쳐 아이템을 연속 공급하면 합류 승인이 입력별로 번갈아 적용된다. 입력 하나에서 갈라지는 `Conveyor?A` 분배기는 출력을 라운드로빈으로 균등하게 선택한다. 입력 2개·출력 2개가 교차하는 지점은 각 아이템의 진입 방향을 기억해 회전하지 않고 반대편 직선 출력으로만 보낸다.
16. 채굴기 출구가 향하는 첫 컨베이어는 출구 방향을 외부 입력으로 판정한다. 따라서 U 방향 채굴기에서 나온 첫 컨베이어가 R 방향으로 꺾이면 `ConveyorUR`이 표시된다. 실제 컨베이어 입력까지 여러 개가 합류하면 기존 규칙대로 별도 합류 이미지를 사용하지 않는다.
17. 컨베이어와 건물·원재료의 마스크 하단은 `ConveyorLevel`, 아이템과 건물·원재료 상단은 `ItemLevel` Sorting Layer를 사용한다. `ItemLevel`은 항상 `ConveyorLevel` 위에 그려진다. 채굴기와 5종 64×64 원재료는 `BuildingLowerMask.png`의 알파와 겹치는 부분만 Lower SpriteRenderer로, 나머지를 Upper SpriteRenderer로 표시하며 두 조각의 합은 원본과 같다.
18. 같은 Sorting Layer 안에서는 원재료, 채굴기 조각, 컨베이어와 이동 아이템이 Y 기반 정렬 규칙을 사용한다. 아이템은 매 프레임 보간된 격자 좌표와 `(0.5, 0.25)` Sprite 피벗을 정렬 기준으로 사용한다.
19. 네 번째 슬롯을 선택하면 3×3 염색기 고스트가 표시된다. `R`은 채굴기와 동일하게 출력 방향을 반시계 회전하며, 원재료·건물·컨베이어와 3×3 점유 영역이 겹치면 설치할 수 없다.
20. 사용자 좌표축은 `x+ = 화면 우하단`, `y+ = 화면 우상단`이다. 이 좌표계에서 U 방향 염색기의 중심이 `(0, 0)`일 때 내부 입력 포트는 `(-1, -1)`, `(1, -1)`, 내부 출력 포트는 `(0, 1)`이다. Unity 내부 격자로 변환한 포트는 각각 `(-1, 1)`, `(-1, -1)`, `(1, 0)`이며, 연결 컨베이어는 `(-2, 1)`, `(-2, -1)`, `(2, 0)`에 둔다. R/D/L은 이 배치를 화면 방향에 맞게 회전한다.
21. 새 염색기 위에는 어두운 월드 UI 배경의 `(레시피 선택)` 툴팁이 건물 상단에 가깝게 붙어 표시된다. 건설 도구를 해제한 뒤 건물을 좌클릭하면 Cafe24PROSlimFit TMP 폰트와 `RoundedRectangle.png` 9-slice로 만든 레시피 창이 열린다. 참조 UI와 같이 제목·닫기·구분선, `달팽이`/`버섯`/`뿔버섯` 아이콘 목록과 선택 테두리, 필요 재료 카드, `소요 시간 1.0초`, 선택 결과 하단 바와 `확인` 버튼을 표시한다. 레시피를 확정하면 툴팁이 사라진다.
22. 레시피는 빨강·파랑 달팽이 껍질, 파랑·주황·초록 버섯 갓, 파랑·주황·초록 뿔버섯 갓이다. 뿔버섯 원재료 생산자는 아직 없지만 레시피와 결과 아이템은 등록되어 있다.
23. 선택한 레시피의 바탕 재료와 염료를 두 입력 컨베이어로 공급하면 각 아이템은 내부 포트로 빠르게 이동하면서 0.12초 스케일 아웃되고, 두 재료가 모이면 결과가 출력 컨베이어 위에 0.12초 스케일 인으로 생성된다. 그 뒤의 컨베이어 이동 속도는 기존과 같이 셀당 0.45초이다.
24. 다섯 번째 슬롯은 3×3 조합기 건설 모드다. `R`로 출력 방향을 반시계 회전하며 입력 2개와 출력 1개의 위치·컨베이어 연결 규칙은 염색기와 같다. 원재료·건물·컨베이어와 점유 영역이 겹치면 설치할 수 없다.
25. 새 조합기에는 `(레시피 선택)` 툴팁이 표시된다. 조합기를 클릭하면 염색기와 동일한 공용 레시피 창이 `조합기` 제목과 `염료` 대분류로 열리고, 빨강+노랑→주황, 빨강+파랑→보라, 파랑+노랑→초록 중 하나를 선택할 수 있다. 레시피 확정 뒤 툴팁이 사라진다.
26. 선택한 두 원색 염료를 조합기의 두 입력 컨베이어로 공급하면 염색기와 같은 빠른 입력 소멸 연출 뒤 출력 컨베이어에 혼합 염료가 생성된다. 입력 순서는 결과에 영향을 주지 않는다.
27. 세 번째 슬롯은 1×1 에르다 주입기 건설 모드다. 방향 반대편 한 칸의 컨베이어가 입력이고 방향 앞 한 칸의 컨베이어가 출력이다. `R`로 방향을 회전하며 주입기 셀은 다른 건물·원재료·컨베이어와 겹칠 수 없다.
28. 입력 컨베이어가 주입기 방향을 향할 때 초록·빨강·파랑 달팽이 껍질, 파랑·주황·초록 버섯 갓, 파랑·주황·초록 뿔버섯 갓을 넣으면 각각 대응하는 몬스터 아이템이 출력 컨베이어에 0.12초 스케일 인으로 생성된다. 주황·초록 뿔버섯 몬스터는 현재 파란 뿔버섯 몬스터 Sprite를 공유한다. 결과는 달팽이 껍질과 동일하게 셀당 0.45초로 이동하고 합류·분배·정렬 규칙을 공유한다. 출력 컨베이어가 없거나 점유되어 있으면 내부 결과를 보관하고 생산을 대기한다. 에르다 주입기는 별도의 레시피 선택 UI가 없고 등록되지 않은 아이템을 소비하지 않는다.
29. Combiner는 `BuildingLowerMask.png`, 32×64 에르다 주입기는 `BuildingLowerMask1x1.png`로 Lower/Upper를 각각 전처리한다. 두 건물과 모든 운송 아이템은 기존 `ConveyorLevel`/`ItemLevel` 및 Y 기반 정렬 규칙을 따른다.
30. 여섯 번째 슬롯은 3×3 가공기계 건설 모드다. 설치 방향 반대쪽 중앙에 입력 컨베이어 한 개, 설치 방향 중앙에 출력 컨베이어 한 개를 연결한다. `R`로 출력 방향을 반시계 회전하며 다른 3×3 건물과 동일한 점유·고스트·Lower/Upper 정렬 규칙을 사용한다.
31. 새 가공기계에는 `(레시피 선택)` 툴팁이 표시된다. 클릭하면 염색기·조합기와 같은 공용 레시피 창이 `가공기계` 제목과 `가공` 대분류로 열리고, 필요 재료에는 초록 달팽이 껍질 한 줄만 표시된다.
32. 초록 달팽이 껍질 한 개를 입력하면 빠른 스케일 아웃 뒤 `Horn.png` 뿔 아이템이 출력 컨베이어에 스케일 인으로 생성된다. 조합기의 `뿔버섯` 대분류에서 뿔 한 개와 기본 버섯 갓 한 개를 조합하면 염색 전 뿔버섯 갓이 생산된다.
33. 일곱 번째 슬롯은 2×2 포탈 건설 모드다. 포인터 셀을 발자국의 좌하단 앵커로 사용하고 네 칸의 중심에 고스트와 건물을 표시한다. 네 변에 붙는 컨베이어 두 칸씩, 총 8개 위치에서 포탈 방향으로 들어오는 아이템을 받을 수 있다. 발자국은 원재료·건물·컨베이어와 겹칠 수 없고 입력 위치까지 맵 안에 있을 때만 설치할 수 있다.
34. 새 포탈에는 `(아이템 선택)` 툴팁이 표시된다. 건설 도구를 해제하고 포탈을 클릭하면 현재 스테이지에 속한 사냥터만 남색 선택창에 표시된다. 초기 사냥터는 즉시 선택할 수 있고 나머지는 `[잠금]` 행과 `해금` 버튼으로 구분된다. 해금 팝업은 필요 메소, 사용 가능 몬스터, 필요 몬스터 생산 재화와 현재 수량을 표시하며 부족 조건은 붉게 표시한다. 조건을 모두 만족해야 두 재화를 함께 차감하고 영구 해금한다. 팝업이 열린 동안 카메라 이동·우클릭 패닝·휠 줌은 비활성화된다.
35. 선택된 포탈은 선택한 버섯·달팽이·뿔버섯 몬스터와 일치하고 포탈을 향하는 컨베이어 아이템만 빠른 스케일 아웃으로 소비한다. 가공 전 버섯 갓·달팽이 껍질·뿔버섯 갓은 받지 않는다. 기본 개체 가치는 몬스터별 `1 / 2 / 3 / 5 / 7 / 10 / 20 / 30`메소이며 총 메소는 화면 좌측 상단 `<누적량> 메소` HUD에서 실시간으로 확인한다.
36. 선택 후 월드 툴팁은 `<아이템> | x메소/개`로 바뀐다. 포탈에 공급한 수량은 몬스터 종류별 누적 생산량과 업그레이드에 쓸 수 있는 생산량에 함께 더해지며, 같은 몬스터를 선택한 모든 포탈이 하나의 진행도를 공유한다.
37. 포탈은 `BuildingLowerMaskPortal.png` 전용 마스크로 Lower/Upper Sprite를 만들며 기존 건물과 같은 `ConveyorLevel`/`ItemLevel`, `(0.5, 0.25)` 피벗 및 Y 기반 정렬 규칙을 사용한다.
38. 추출기·염색기·조합기·가공기계·에르다 주입기의 출력 방향에 다음 공장이나 포탈의 입력 포트가 바로 닿아 있으면 중간 컨베이어 없이 생산물이 빠른 이동·스케일 아웃으로 전달된다. 공장끼리는 진행 방향이 같아야 하며, 포탈은 맞닿은 면의 모든 방향 입력을 사용한다. 다음 목적지가 해당 아이템을 받을 수 없으면 생산물은 이전 공장에 대기한다.
39. `X`를 누르면 현재 건설 도구가 해제되고 철거 모드가 켜진다. 철거 중에는 하단 핫바 전체가 어두워지고 입력을 받지 않으며 격자선이 유지된다. 컨베이어를 좌클릭하거나 좌클릭 드래그하면 포인터 프레임 사이의 모든 셀을 보간해 연속 제거한다. 건물의 점유 칸을 지나면 건물 전체·미선택 툴팁·입출력 연결과 해당 건물로 이동 중인 아이템이 제거된다. `X`를 다시 누르거나 `Esc`를 누르면 철거 모드가 끝난다.
40. 화면 좌측의 `아이템 업그레이드 [U]` 버튼 또는 `U`를 누르면 화면 오른쪽의 폭 620 성장 패널이 열린다. 전체 화면 음영은 없으며 패널 바깥에서 키보드 이동·우클릭 패닝·휠 줌을 계속 사용할 수 있다. 패널 내부 휠은 목록 스크롤에만 사용하고 `Esc`로 창을 닫는다.
41. 상단의 책갈피형 `메소`와 `누적 생산량` 탭을 바꾸면 아래 세로 스크롤 목록의 8종 스테이지 몬스터 행이 즉시 갱신된다. 각 행은 아이콘, 이름, 레벨·효과, 비용, 강화 가능 상태를 한 줄에 표시하며 한 화면에서 여러 대상을 빠르게 비교하고 반복 강화할 수 있다.
42. 메소 업그레이드는 메소를 소비해 개체 기본 가치에 몬스터별 레벨당 값을 합연산한다. 누적 생산량 업그레이드는 해당 몬스터의 사용 가능한 생산량을 소비해 개체 가치에 몬스터별 배율을 곱연산한다. 최대 레벨 제한은 없으며 다음 비용은 `몬스터별 기본 비용 × 비용 계수^현재 레벨`이다. 생산량 업그레이드가 가능해지면 좌측 단축 버튼에 가능한 몬스터 수 배지가 표시된다.
43. `F2`를 누르면 화면 왼쪽에 전체 화면 음영 없는 런타임 디버그 패널이 열린다. `맵` 탭은 잔디 두 종류 페인트, 5종 원재료 배치, 셀 제거, 셀/전체 이동 아이템 제거를 제공하며 월드에서 좌클릭 드래그로 적용한다. 건물과 컨베이어는 기존 핫바 또는 숫자키로 배치한다.
44. 디버그 `몬스터` 탭은 몬스터를 순환 선택해 기본 가치, 레벨당 합연산 값, 레벨당 곱연산 계수, 두 현재 레벨과 사용 가능 생산량을 즉시 수정한다. `업그레이드` 탭은 몬스터별 두 기본 비용과 전역 메소·생산량 비용 계수를 조정한다. 변경된 최종 가치와 비용은 포탈·업그레이드 UI에 즉시 반영된다.
45. 공장 화면 우측 상단의 `돌아가기`를 누르면 현재 실행 세션의 공장 상태를 보관하고 스테이지 선택 화면으로 복귀한다. 1스테이지 외 스테이지의 잠금과 구매 비용을 확인할 수 있으며 같은 실행 중 해금된 스테이지는 재입장 시 비용을 다시 요구하지 않는다.
46. 디버그 `해금` 탭은 스테이지·사냥터별 메소 비용을 수정한다. `변경사항 저장 후 처음부터 실행`은 이 설정과 몬스터·업그레이드 밸런스, 현재 스테이지의 잔디·원재료 맵 편집을 현재 실행 세션에 유지하고 메소·생산량·업그레이드·스테이지·사냥터 진행을 초기화해 새 게임 상태로 재시작한다.
47. 스테이지를 나가면 컨베이어, 설치 건물의 위치·방향, 선택한 레시피와 포탈 아이템이 실행 중 메모리에 보관되어 재입장 시 복원된다. 스테이지 선택 화면이나 다른 스테이지에 있는 동안에도 보관된 공장은 화면 없이 같은 운송 규칙으로 계속 작동한다. 앱을 다시 시작하면 모든 상태가 초기화된다.
48. 최초 플레이 튜토리얼은 실제 우클릭 패닝, 휠 줌, 컨베이어 선택, R 회전, 실제 건설, X 철거 모드, 실제 철거 이벤트를 순서대로 감지한다. 건너뛰기와 다시보기를 제공하며 완료 단계와 기능별 최초 안내 여부는 현재 실행 세션에만 유지된다.
49. E 도감은 몬스터·염색기·가공시설·조합기·원재료 책갈피 탭, 잠금 항목, 재귀 제작 과정과 이전 탐색 history를 제공한다. 현재 목표의 `레시피 보기`는 목표 항목을 선택한 채 도감을 연다.
50. 건설 고스트가 표시되는 동안에만 제공된 입력·출력 아이콘이 실제 상태 객체가 가진 컨베이어 연결 셀에 표시된다. R 회전 시 건물 방향과 포트 표시가 함께 갱신되며 건설 모드를 끝내면 포트 표시도 즉시 사라진다.

Edit Mode 자동 테스트는 Test Runner에서 `Maptory.Factory.Tests` 어셈블리를 실행한다. 테스트는 기존 컨베이어·채굴·정렬 규칙과 함께 맵 크기와 무관한 격자 메시, 염색기와 조합기의 포트·점유·레시피·생산, 가공기계와 에르다 주입기, 포탈 경제와 두 업그레이드, 스테이지·사냥터 기본 잠금과 영구 해금 구매, 두 재화의 원자적 차감·중복 구매 방지·직접 진입 차단, 설정 직렬화와 잠금 UI, 우측 업그레이드 창·숫자키 핫바, 원재료 런타임 편집, 철거 드래그, Lower/Upper Sprite를 검증한다.

## 2. 기능 사용법

`FactoryGame`은 맵 표현과 입력/UI 조립을 담당하는 Scene 진입점이다. Main Camera가 `MainCamera` 태그를 가지고 있어야 한다. 스테이지별 기본 원재료 종류와 중심 좌표는 `FactoryContentConfig`에서 읽고, 디버그 맵 저장이 있으면 해당 배치를 사용한다. 모든 런타임 Sprite는 `Art/Resources/Factory` 아래에서 이름으로 로드된다. `FactorySpriteImporter`는 Point 필터와 16 PPU를 적용하고, 컨베이어·건물·아이템에는 `(0.5, 0.25)`, 원재료에는 중앙 피벗을 사용한다. 월드 객체는 `FactorySorting`의 명시적 Y 깊이 순서를 공유한다.

건물·원재료 원본이나 `Art/BuildingProcessing`의 마스크가 변경되면 `BuildingSpriteLayerGenerator`가 각 원본의 `Lower`와 `Upper` PNG를 해당 `Generated` 폴더에 다시 만든다. 64×64 건물과 원재료는 `BuildingLowerMask.png`, 32×64 1×1 건물은 `BuildingLowerMask1x1.png`, 32×32 포탈은 `BuildingLowerMaskPortal.png`를 사용한다. 수동 갱신은 Unity 메뉴 `Tools > Maptory > Regenerate Building Layers`를 사용한다.

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

`PlaceLine`은 같은 x 또는 같은 y 좌표의 끝점만 받는다. 시작점과 끝점이 같으면 우측 상단(U) 방향 컨베이어 한 칸을 배치한다. 기존 칸을 포함하는 선을 배치하면 `SetConveyor`가 방향을 교체한다. `TrySelectNextOutput`은 일반 분배기에서 가능한 출력을 순환하고 2입력·2출력 교차로에서는 전달받은 진입 방향과 같은 직선 출력을 선택한다.

채굴과 운송은 `ExtractionNetwork`와 `FactoryItemTransport`가 소유한다. 채굴기는 반드시 등록된 원재료 중심에만 배치하며 출력 셀은 중심에서 방향 오프셋의 두 배만큼 떨어져 있다. 생산 타이머는 컨베이어 스텝과 독립적인 `EXTRACTOR_PRODUCTION_INTERVAL = 1f`를 사용하므로 초당 1개를 유지한다.

공장 출력 위치가 다음 공장이나 포탈의 입력 포트와 바로 맞닿으면 `FactoryItemTransport`가 컨베이어 대신 해당 소비자를 목적지로 예약한다. 아이템은 생산 공장의 출력 포트에서 목적지 입력 포트로 이동하며, 목적지가 받을 수 있을 때만 생산 측 재료를 소비한다.

철거 입력은 `FactoryBuildMode.IsDemolitionMode`가 소유한다. 코드에서 모드를 바꾸려면 `ToggleDemolitionMode` 또는 `SetDemolitionMode`를 사용한다. `ExtractionNetwork.RemoveBuilding`은 클릭 셀이 속한 건물 상태를 반환하고 `BuildingRemoved`를 발행하며, `ConveyorNetwork.RemoveConveyor`는 단일 컨베이어 상태를 제거한다. 런타임 화면 제거와 운송 취소는 `FactoryDemolitionController`가 조립하고 `GetLineCells`로 프레임 사이 포인터 셀을 빠짐없이 보간한다.

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

포탈 좌표는 2×2 발자국의 좌하단 앵커다. `InputPorts`는 컨베이어 위치, 포탈 내부 도착 위치와 필수 진행 방향을 함께 제공한다. 선택 품목을 공급하면 네트워크 공용 `PortalEconomy`가 메소, 몬스터별 누적 생산량과 업그레이드에 사용할 수 있는 생산량을 갱신한다. 업그레이드는 자동으로 실행되지 않으며 `TryPurchaseMesoUpgrade` 또는 `TryPurchaseProductionUpgrade` 호출 때만 자원을 소비한다.

```csharp
var portal = extraction.PlacePortal(new Vector2Int(30, 20));
portal.SelectMaterial(RawMaterialType.MonsterSnailRed);

foreach (var input in portal.InputPorts)
{
    conveyors.SetConveyor(input.ConveyorPosition, input.Direction);
    conveyors.AddExternalOutput(input.ConveyorPosition, input.Direction);
}

var transport = new FactoryItemTransport(conveyors, extraction);
transport.SpawnItem(RawMaterialType.MonsterSnailRed, portal.InputPorts[0].ConveyorPosition);
transport.Step();
transport.Step();

var economy = extraction.PortalEconomy;
if (economy.CanPurchaseProductionUpgrade(RawMaterialType.MonsterSnailRed))
{
    economy.TryPurchaseProductionUpgrade(RawMaterialType.MonsterSnailRed);
}
```

## 3. 코드 구조와 책임

| 파일 | 책임 |
| --- | --- |
| `FactoryGame.cs` | 실행 중 스테이지 세션을 먼저 초기화하고 선택 화면 또는 해당 스테이지의 맵·건설·운송·포탈 UI를 조립하는 Scene 진입점 |
| `GridDirection.cs` | 4방향 값과 격자 오프셋, Sprite 코드, 반대 방향 및 반시계 회전 정의 |
| `ConveyorNetwork.cs` | 컨베이어 방향 상태, 직선 배치/덮어쓰기, 건물 입출력을 포함한 연결 분석, Sprite 이름과 출력 분배 순서 소유 |
| `ExtractionNetwork.cs` | 아이템·가변 재료 공용 레시피 계약, 원재료와 채굴기·염색기·조합기·가공기계·에르다 주입기 상태, 3×3/1×1 점유와 포트 좌표, 설치 이벤트 소유 |
| `FactoryItemTransport.cs` | 채굴 생산, 공장 간 직접 전달, 염색·조합·가공·에르다·포탈 입력 소비와 출력, 컨베이어 이동·역압·합류·분배 시뮬레이션 소유 |
| `PortalSystem.cs` | 스테이지 몬스터 공급 항목, 2×2 포탈과 잠금 선택 검증, 메소·생산량·무제한 합연산/곱연산 업그레이드 상태 소유 |
| `ErdaInjectionRecipes.cs` | 에르다 주입기가 받는 달팽이·버섯·뿔버섯 재료와 대응 몬스터 운송 아이템 정의 |
| `FactorySorting.cs` | 컨베이어·아이템 레벨 Sorting Layer 이름, 결정적 Y/X 정렬 순서와 높이 Z를 포함한 투명 정렬 축 정의 |
| `FactoryBuildMode.cs` | 핫바 건설 도구, `X` 철거 모드와 `Esc` 해제를 단일 상태로 관리 |
| `FactoryDemolitionController.cs` | `X` 철거 입력, 드래그 셀 보간, 컨베이어·건물 뷰 제거와 상태 시스템 연결 |
| `ConstructionGridOverlay.cs` | 건설 모드 동안 맵 크기와 무관한 단일 메시와 화면 픽셀 굵기 셰이더로 아이소메트릭 격자선을 표시 |
| `Art/Resources/Factory/Construction/ConstructionGridOverlay.shader` | 단일 메시의 보간된 격자 좌표로 셀 경계와 화면상 일정한 선 굵기를 계산 |
| `FactoryTileCatalog.cs` | 잔디, 컨베이어, 원재료, 건물 원본·Lower·Upper, 일반·몬스터 아이템·UI Sprite와 런타임 TMP 폰트 조회 제공 |
| `ConveyorBuilder.cs` | 핫바가 켠 건설 모드에서 포인터를 격자에 투영하고 건물 점유를 검증한 직선 미리보기·배치를 수행하며 셀별 컨베이어 SpriteRenderer를 갱신 |
| `ExtractorBuilder.cs` | 원재료·채굴기 Lower/Upper 표현, 방향 고스트와 중심 일치 검증 |
| `DyeingMachineBuilder.cs` | 염색기 방향 고스트·설치·클릭, Lower/Upper 렌더러와 미선택 툴팁 생성 |
| `CombinerBuilder.cs` | 3×3 조합기 방향 고스트·설치·클릭, Lower/Upper 렌더러와 미선택 툴팁 생성 |
| `ErdaInjectorBuilder.cs` | 1×1 에르다 주입기 방향 고스트·설치 및 전용 마스크 Lower/Upper 렌더러 생성 |
| `ProcessingMachineBuilder.cs` | 3×3 가공기계 방향 고스트·설치·클릭, Lower/Upper 렌더러와 미선택 툴팁 생성 |
| `PortalBuilder.cs` | 2×2 포탈 고스트·점유 검증·설치·클릭과 Lower/Upper 렌더러 생성 |
| `RecipeSelectionPanel.cs` | 염색기·조합기·가공기계가 공유하는 TMP 기반 레시피 모달, 동적 대분류·1~2개 필요 재료·결과 표시와 확정 처리 |
| `RecipeTooltip.cs` | 레시피 기반 건물이 공유하는 `(레시피 선택)` 월드 UI 생성 |
| `PortalSelectionPanel.cs` | 현재 스테이지 사냥터 목록, 잠금 행, 메소 구매 팝업과 포탈 품목 선택 처리 |
| `PortalTooltip.cs` | 포탈의 미선택 안내 또는 선택 몬스터와 현재 개체 가치 월드 UI 표시 |
| `MesoHud.cs` | 화면 좌측 상단 `<누적량> 메소` 표시 |
| `ItemUpgrades/ItemUpgradePanel.cs` | 오른쪽 비모달 성장 패널, 책갈피형 카테고리 탭, 세로 스크롤 목록과 업그레이드 요청 조립 |
| `ItemUpgrades/ItemUpgradeRow.cs` | 몬스터별 아이콘·레벨·효과·비용·강화 가능 상태 표시와 구매 입력 |
| `ItemUpgrades/ItemUpgradeShortcut.cs` | 좌측 `U` 단축 버튼과 생산량 강화 가능 몬스터 수 알림 배지 표시 |
| `FactoryItemTransportView.cs` | 운송 상태를 선형 보간해 아이템 위치·빠른 입출력 스케일·실시간 깊이를 갱신하고 소비된 렌더러 제거 |
| `FactoryHotbar.cs` | 화면 하단 10슬롯 UI, 숫자키 1~0 입력, 일곱 건설 도구의 선택 상태와 공용 클릭 이벤트 제공 |
| `FactoryCameraController.cs` | 키보드 이동, UI 바깥 우클릭 패닝·휠 확대/축소와 맵 범위 제한을 담당하고 선택 모달 입력 차단 함수를 적용 |
| `Codex/FactoryContentCatalog.cs` | 기존 레시피에서 도감 항목과 핫바·튜토리얼 공통 건물 정보를 구성 |
| `Codex/FactoryCodexPanel.cs` | E 도감의 탭·항목·재귀 제작 과정과 탐색 history 표시 |
| `Objectives/FactoryObjectiveSystem.cs` | 생산·공급·업그레이드·해금 이벤트 기반 현재 목표와 도감 바로가기 |
| `Tutorial/FactoryTutorialSystem.cs` | 실제 조작 이벤트 기반 초반 튜토리얼과 기능 최초 접근 안내 |
| `BuildingPorts/FactoryBuildingPortOverlay.cs` | 건설 미리보기 중 실제 입출력 포트 표시 |
| `DebugTools/FactoryDebugPanel.cs` | F2 런타임 디버그 UI, 맵·몬스터·업그레이드·해금 설정과 세션 설정 유지 후 새 게임 재시작 |
| `Progression/FactoryContentConfig.cs` | ScriptableObject 기반 2개 스테이지·기본 자원 배치·순서화된 사냥터와 메소 비용 정의 |
| `FactoryUiEventSystem.cs` | 스테이지 선택과 공장 UI가 공유하는 Input System UI 입력 초기화 |
| `Progression/FactorySaveService.cs` | Scene 전환용 플레이 진행, 스테이지별 공장 배치와 디버그 설정의 실행 중 메모리 복사본 관리 |
| `FactoryStagePersistence.cs` | 컨베이어·건물·레시피·포탈 선택 캡처/복원과 비활성 스테이지의 화면 없는 운송 시뮬레이션 |
| `Progression/FactoryProgression.cs` | 해금 상태·구매 검증·원자적 재화 소비와 스테이지 세션 소유 |
| `Progression/StageSelectionPanel.cs` | 스테이지 목록·구매·입장과 공장 우측 상단 돌아가기 UI |
| `DebugTools/FactoryDebugMapEditor.cs` | 잔디·원재료·제거·아이템 브러시와 스테이지별 맵 설정 캡처를 기존 상태에 연결 |
| `Editor/FactorySpriteImporter.cs` | 기능 전용 픽셀 아트의 Sprite import 설정 고정 |
| `Editor/BuildingSpriteLayerGenerator.cs` | 3×3/1×1/포탈 건물과 64×64 원재료에 맞는 하단 마스크를 선택해 Lower/Upper PNG로 전처리하고 변경 시 재생성 |
| `Tests/EditMode/ConveyorNetworkTests.cs` | 컨베이어 연결 및 분배 규칙의 Edit Mode 회귀 테스트 |
| `Tests/EditMode/ExtractionAndTransportTests.cs` | 채굴기 배치·회전·생산, 운송·합류 및 정렬 규칙 회귀 테스트 |
| `Tests/EditMode/ConstructionGridOverlayTests.cs` | 초대형 맵에서도 메시 크기가 일정하고 건설 모드에만 표시되는지 검증 |
| `Tests/EditMode/CombinerAndErdaInjectorTests.cs` | 3종 염료 조합, 3×3/1×1 점유, 9종 몬스터 아이템 변환·출력 대기·이동과 런타임 Sprite 검증 |
| `Tests/EditMode/ProcessingMachineTests.cs` | 가공기계 중앙 포트·뿔 생산, 조합기 뿔버섯 레시피와 신규 Sprite 검증 |
| `Tests/EditMode/PortalTests.cs` | 포탈 2×2 점유·8개 입력·운송 소비·품목 필터·메소·몬스터별 공유 생산량·두 업그레이드·Sprite 검증 |
| `Tests/EditMode/ItemUpgradeUiTests.cs` | 업그레이드 행 구성·책갈피 탭 전환과 다른 모달이 열린 동안의 진입 차단 검증 |
| `Tests/EditMode/DemolitionTests.cs` | 철거 모드 상태, 건물 발자국 제거, 컨베이어 연결 제거, 이동 중 아이템 취소와 드래그 셀 보간 검증 |
| `Tests/EditMode/ProgressionTests.cs` | 스테이지·사냥터 해금, 원자적/중복 구매, 잠긴 몬스터 직접 선택 차단, 설정과 UI 상태 검증 |

`ConveyorNetwork`와 `ExtractionNetwork`가 Scene 전환 가능한 게임 상태를 소유한다. `FactoryItemTransport`는 두 네트워크만 참조하고 Unity UI나 Renderer에 의존하지 않는다. 건설 Builder와 `FactoryItemTransportView`가 입력과 표현을 담당하며 레시피 UI는 선택 결과만 `DyeingMachineState`에 전달한다.

메소, 몬스터별 누적·사용 가능 생산량, 두 업그레이드 레벨, 스테이지·사냥터 해금과 스테이지별 공장 배치는 같은 실행 중 Scene 전환을 위해 메모리에만 유지된다. `PlayerPrefs`와 파일 저장은 사용하지 않으므로 앱을 종료하고 다시 시작하면 진행과 디버그 설정이 모두 초기화된다.
