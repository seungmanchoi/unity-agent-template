# Unity 작업 방식 (MANDATORY)

> CLAUDE.md 에서 옮긴 상세 규칙이다. **Hard Threshold 표는 CLAUDE.md 가 정본**이며, 여기 없는 통과 기준은 CLAUDE.md 를 따른다.

에이전트는 Unity Editor GUI 를 쓰지 않는다. C#·JSON·원본 에셋(PNG, WAV, 폰트)이 정본이고, 씬·프리팹은 Editor 스크립트가 batchmode 로 만든다. 검증은 `npm run verify`(정적 검사 + Domain 테스트), Unity 컴파일·EditMode 테스트, 시뮬레이터 순서로 한다.

## 고정 버전

| 항목 | 버전 | 위치 |
|------|------|------|
| Unity Editor | `6000.3.26f1` (Personal) | `unity/ProjectSettings/ProjectVersion.txt` |
| Input System | `1.20.0` | `unity/Packages/manifest.json` |
| Test Framework | `1.6.0` | 〃 |
| uGUI | `2.0.0` | 〃 |
| Active Input Handling | Input System (`activeInputHandler: 1`) | `ProjectSettings.asset` |

- 모든 패키지는 정확한 버전(또는 커밋 해시로 고정한 git URL)만 쓴다. 범위·`latest` 금지.
- 버전을 올리는 건 의도한 변경이다. `manifest.json`, `scripts/check-harness.mjs` 의 `PINS`, CLAUDE.md 를 같이 고친다.
- `ProjectSettings/` 는 커밋한다. 없으면 Unity 가 새 프로젝트로 여겨 `manifest.json` 을 기본 패키지 목록으로 덮어쓴다(`docs/troubleshooting.md`).

## 생성기 — 씬은 코드가 만든다

- `unity/Assets/Game/Editor/Build.cs` 의 `Generate()` 가 `Assets/Game/Generated/Main.unity` 를 새로 만들고 PlayerSettings·아이콘을 적용한다. `Generated/` 는 gitignore 다.
- 생성기는 몇 번을 돌려도 결과가 같아야 한다(idempotent). 두 번째 실행 뒤 `git status` 가 깨끗해야 한다.
- Unity YAML(`.unity`, `.prefab`, `.asset`)과 `.meta` GUID 를 손으로 쓰지 않는다. 새 파일을 만들면 Unity 를 한 번 돌려 `.meta` 를 만들게 하고, 그 `.meta` 를 같이 커밋한다. 빠진 `.meta` 는 Unity 가 새 GUID 로 다시 만들어 참조가 끊긴다.
- 직렬화 필드 연결은 `SerializedObject.FindProperty(...).objectReferenceValue` 로 생성기 안에서 한다.
- 앱 식별자(slug·이름·bundle id·버전)는 `Editor/AppInfo.cs` 한 곳에만 둔다. fastlane 이 같은 파일을 읽는다.

## 명령

| 목적 | 명령 | 비고 |
|------|------|------|
| 정적 검사 + Domain 테스트 | `npm run verify` | Stop hook 이 같은 명령을 돈다 |
| Domain 테스트만 | `npm test` | `dotnet test unity/DomainTests~` (net10.0, C# 9, Unity 라이선스 불필요) |
| 씬 생성 | `npm run unity -- generate` | |
| 화면 렌더 | `npm run unity -- render` | `Build/render-check/*.png`, GPU 필요 — `-nographics` 금지 |
| Unity EditMode 테스트 | `npm run unity -- test` | `-runTests` 는 `-quit` 없이. 결과 `build-output/editmode-results.xml` |
| iOS Xcode 프로젝트 | `npm run unity -- ios` / `ios-sim` | `APP_VERSION`, `BUILD_NUMBER` env. 배포는 fastlane(`build-deploy.md`) |
| Android | `npm run unity -- android` / `apk` | 키스토어는 env 로만 |

`scripts/unity.mjs` 는 네임스페이스를 `AppInfo.cs` 에서, Unity 경로를 `ProjectVersion.txt`(또는 `UNITY_PATH`)에서 읽는다. 같은 프로젝트를 Unity 두 개가 동시에 열 수 없으니 batchmode 실행은 한 번에 하나씩 한다.

## 어셈블리 레이어 (asmdef)

```
Game.Domain  ←  Game.Platform  ←  Game.Presentation  ←  Game.Editor
     ↑
Game.Tests.EditMode
```

| 어셈블리 | 담는 것 | 참조 가능 |
|----------|---------|-----------|
| `Game.Domain` | 순수 C# 규칙·시뮬레이션·진행·보상·평점 정책. `noEngineReferences: true` | 없음 |
| `Game.Platform` | 광고·Analytics·Crashlytics·평점·햅틱·Keychain 등 SDK `using`, `[DllImport]`, `AndroidJavaObject` | Domain |
| `Game.Presentation` | MonoBehaviour, 코드로 만드는 uGUI, 입력(`Pointer.current`), FX, 오디오 | Domain, Platform |
| `Game.Editor` | 생성기, RenderCheck, 빌드 진입점, PostProcessBuild | 전부 |
| `Game.Tests.EditMode` | NUnit 테스트 (`dotnet test` 와 Unity 양쪽에서 돈다) | Domain |

네이티브 코드는 `unity/Assets/Plugins/iOS/*.mm`(심볼 이름은 앱과 무관하게, 예: `AppHaptic`)와 Android `AndroidJavaClass` 로 두고, C# 진입점은 Platform 에만 둔다.

## Domain — 결정적이어야 한다

- 고정 스텝(`GameSim.StepsPerSecond = 60`)으로만 진행한다. Presentation 이 누산기로 `Tick()` 을 부른다:
  ```csharp
  _acc += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
  while (_acc >= GameSim.StepSeconds && !_sim.Done) { _sim.Tick(); _acc -= GameSim.StepSeconds; }
  ```
- 난수는 시드를 받는 `Rng`(xorshift32)만 쓴다. `System.Random`·`UnityEngine.Random` 은 런타임마다 수열이 달라 금지.
- 시계를 읽지 않는다(`DateTime.Now`, `Stopwatch`, `Environment.TickCount` 금지). 시각·시드는 호출자가 넘긴다.
- 상태는 평범한 필드·프로퍼티로 내보내고, Presentation 은 읽어서 그리기만 한다. 규칙 수치는 `GameRules` 같은 객체 하나에 모은다.
- 새 규칙에는 테스트를 같이 쓴다(같은 시드 → 같은 결과, 경계값).

## 모바일 UI (uGUI, 코드로 생성)

- `CanvasScaler`: `ScaleWithScreenSize`, 기준 해상도 **1080×2340**, `screenMatchMode = Expand`. 캔버스가 기준보다 작아지지 않아 세로 폰은 가로 맞춤, 펼친 iPhone Duo 같은 넓은 창은 세로 맞춤이 된다. `matchWidthOrHeight = 0` 만 쓰면 넓은 창에서 기준 높이가 1535 까지 줄어 아래가 잘린다.
- 모든 UI 는 `SafeArea` RectTransform 아래에 두고, `Screen.safeArea` 나 화면 크기가 바뀔 때 앵커를 다시 계산한다. Canvas 를 만드는 파일에 `Screen.safeArea` 가 없으면 verify 가 실패한다.
- 터치 영역은 기준 해상도에서 **132px 이상**(약 44pt).
- 폰트: Noto Sans KR 동적 폰트(`Content/Fonts`, OFL). 새 폰트·아트는 `THIRD_PARTY_ASSETS.md` 에 출처를 적는다.
- 한 씬 상태 기계(Title → Play → Result 등)로 충분하다. 화면이 늘어도 씬을 나누기 전에 상태를 늘린다.
- `Application.targetFrameRate = 60`. 캡처·성능 측정용 자동 플레이는 PlayerPrefs `autoplay`(iOS 에선 `defaults write`)로 켠다.
- uGUI 텍스트는 접근성 트리에 잘 드러나지 않는다. 런타임 확인은 `runtime-testing.md` 를 따른다.

## 날짜·시간

| 상황 | 쓰는 것 | 금지 |
|------|---------|------|
| 오늘(로컬 날짜 키) | `DateTime.Now.ToString("yyyy-MM-dd")` | `DateTime.UtcNow.Date`, `UtcNow.ToString("yyyy-MM-dd")` |
| 저장용 타임스탬프 | `DateTime.UtcNow.ToString("o")` | 로컬 시각 문자열 |
| 게임 규칙 안 시간 | 스텝 수 | 벽시계 |

## 공유 아트

새 아트를 만들거나 받기 전에 `~/works/game-assets/aether/LLM_INDEX.md` 를 읽고 `python3 ~/works/game-assets/aether/scripts/search_assets.py <query>` 로 찾는다. 가져올 때는 `use_asset.py <ID> --project . --dest unity/Assets/Game/Content/<dir> --name <file>` 로 복사해 `THIRD_PARTY_ASSETS.md` 에 출처를 남긴다. 원본은 고치지 않고, 없는 아트를 대량으로 긁어 오지 않는다.

## Hard Threshold (`scripts/check-harness.mjs`)

| 기준 | 임계값 |
|------|--------|
| Codex 사본(`.codex/agents`, `.agents/skills`)이 `.claude` 원본과 다름 | **0개** |
| Unity 에디터·고정 패키지 버전 불일치, 정확한 버전이 아닌 패키지 | **0개** |
| Active Input Handling 이 Input System 이 아님 | **0개** |
| asmdef 레이어 역참조 (위 표 밖의 참조) | **0개** |
| Domain asmdef `noEngineReferences` 누락 / 다른 어셈블리 참조 | **0개** |
| Domain 의 `UnityEngine`/`UnityEditor` 사용, 시계 읽기, `System.Random` | **0개** |
| Platform·Editor 밖의 SDK `using`(GoogleMobileAds, Firebase, Google.Play, Purchasing, Unity.Services) / `[DllImport]` | **0개** |
| Canvas 를 만들면서 `Screen.safeArea` 미적용 | **0개** |
| UTC 로 만든 로컬 날짜 키 | **0개** |
| PlayerPrefs 에 token/secret/password/receipt 저장 | **0개** |
| 상투적 ATT 문구(`deliver personalized ads to you`) | **0개** |
| Firebase 설정 파일·서명 파일·`Library/`·`Build/`·`Generated/` 커밋 | **0개** |
| `Assets/` 파일의 `.meta` 누락 | **0개** |
| `npm test`(Domain) 실패 | **0개** |
