# Project Workflow

- The final target platform is PC (Standalone Windows).
- During development, use a WebGL build followed by `uniweb` for every playtest.
- Create WebGL builds in sequential directories under `Builds/`, using the form `Builds/Web001`, `Builds/Web002`, and so on. Do not overwrite a previous playtest build.
- Start a playtest with `uniweb run "<WebGL build directory>"`. The directory must directly contain Unity's `index.html` and its generated `Build`, `TemplateData`, and `StreamingAssets` directories.
- Before using `uniweb`, run `uniweb help` when its behavior or supported options are relevant.
- Optimize both WebGL iteration build time and initial download size. Use incremental IL2CPP builds, `OptimizeSize` IL2CPP code generation, High managed stripping, WebAssembly 2023, and Gzip compression. Disable debug symbols, development build, debugger attachment, profiler connection, deep profiling, and build-size analysis. Keep Data Caching and build file hashes enabled, and keep Decompression Fallback disabled.
- Make a Git commit after each self-contained unit of work. Do not combine unrelated work in one commit.

## Installed External Packages

- DOTween (Demigiant) is installed for tween and animation sequencing.

## 구현 원칙

프로토타입 기능을 별도의 임시 구현으로 만들지 않는다. 핵심 게임 규칙은 정식 게임에서 유지할 데이터 모델과 시스템 경계를 사용해 구현한다. 프로토타입 범위는 콘텐츠 수, UI 완성도, 연출 및 맵 수를 제한하는 방식으로 줄인다.

## Codex Unity 작업

Unity Test Runner 작업이 초기화되지 않거나, 테스트 수가 0으로 나오거나, 진행 없이
시간 초과되는 등 예상과 다른 결과가 발생하면 재시도하기 전에 Unity Console의
Error 항목을 먼저 확인한다. 컴파일 오류가 있으면 테스트 실행 문제로 취급하지
말고 해당 오류를 우선 해결한 뒤 컴파일 성공을 확인하고 테스트를 다시 실행한다.

Unity MCP의 에셋 새로고침이나 도메인 리로드 대기가 60초를 넘기거나
`ping not answered`가 반환되면, 먼저 Computer Use로 Unity 창에 외부 파일 변경
확인 대화상자가 떠 있는지 확인한다. `The open scene(s) have been modified
externally` 대화상자가 있고 디스크의 Scene 변경이 현재 작업에서 만든 것이라면
`Reload`를 선택한 뒤 MCP 작업을 재시도한다. 저장되지 않은 사용자 Scene 변경을
덮을 가능성이 있으면 `Reload`나 `Ignore`를 임의로 선택하지 않고 사용자에게
확인한다.

## 프로토타입 저장 호환성

사용자가 실사용자가 있다고 명시하기 전까지는 기존 저장 데이터 호환성을
전제로 하지 않는다. ID나 데이터 구조를 변경할 때 구 ID 별칭, 저장 마이그레이션
또는 호환 분기를 추가하지 않고 현재 정식 데이터만 간결하게 유지한다.

## 기능 단위 폴더

프로젝트 파일은 파일 종류가 아닌 기능 단위로 구성한다. 스크립트, prefab, material, shader, `ScriptableObject` 및 기능 전용 에셋은 모두 해당 기능의 루트 폴더 안에 둔다. 프로젝트 전체를 대상으로 하는 `Scripts`, `Prefabs`, `Materials` 등의 파일 종류별 폴더를 만들지 않는다.

기능 폴더는 필요에 따라 `Editor`와 `Tests`, 중첩 기능 하위 폴더를 가질 수 있다. 이는 Unity 컴파일 및 테스트 어셈블리 경계를 위한 예외이며, 기능에 속한 파일을 기능 루트 밖으로 분산하지 않는다.

## 기능 README

새로운 기능을 구현하거나 기존 기능의 공개 사용법을 변경한 경우, 해당 기능 루트의 `README.md`를 생성하거나 갱신한다. 문서 작성은 기능 완료 조건에 포함된다.

README는 다음 순서로 작성한다.

### 1. 기능 검증 방법

깨끗한 Scene 또는 명시된 검증 Scene에서 전체 실험 환경을 구성하는 방법을 설명한다. 필요한 `GameObject`, `Component`, prefab, `ScriptableObject`, Inspector 설정값, 실행 순서, 입력 방법과 정상 동작 시 관찰되는 결과를 구체적으로 기록한다. 자동 테스트가 있다면 테스트 커버리지를 상세히 설명하도록 한다.

### 2. 기능 사용법

다른 기능이나 개발자가 사용해야 하는 공개 클래스, `Command`, `Component`, prefab 및 데이터 에셋을 설명한다. 필요한 초기화 순서와 컴파일 가능한 최소 코드 예시를 제공한다. 내부 구현에 직접 의존하는 사용법을 공개 API로 안내하지 않는다.

### 3. 코드 구조와 책임

기능 폴더 안의 직접 작성한 코드 파일을 표로 나열하고 각 파일의 단일 책임을 설명한다. 상태 소유자, 주요 데이터 흐름, Unity 표현 계층과 시뮬레이션 계층의 연결 지점 및 다른 기능에 대한 의존 방향을 함께 설명한다.

필요한 경우 마지막에 현재 제약과 계획된 확장 지점을 기록한다. 현재 구현되지 않은 기능을 이미 지원되는 것처럼 표현하지 않는다.

기능을 변경한 뒤에는 README의 검증 절차, 코드 예제, prefab 구성 및 파일별 책임 설명이 실제 구현과 일치하는지 확인한다. README 갱신과 기록된 검증 절차의 수행이 끝나기 전에는 기능 구현을 완료로 간주하지 않는다.

기능 폴더가 하위 기능 폴더를 다시 가질 수 있다. 이 경우 하위 기능 명세를 하위 폴더의 README로 분리할 수 있으며, 상위 README에서 내용을 중복해 서술하지 않고 범위를 분리하도록 한다.