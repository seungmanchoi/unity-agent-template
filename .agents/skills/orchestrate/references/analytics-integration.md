# Phase 4d: Analytics · Crashlytics — 상세 절차 (Firebase Unity 13.17.0)

> orchestrate Phase 4d 와 iterate-app(이미 있는 게임에 Firebase·Crashlytics 를 나중에 붙일 때)이 Read 하고 그대로 따른다. 규칙 요약: `docs/rules/analytics.md`.

**담당**: `platform-integrator` · **입력**: `_workspace/plan/kpis.md`, `prd.md` · **출력**: `Game.Platform` 의 Analytics 래퍼, `AnalyticsEvent` 상수, Presentation 의 이벤트 호출

KPI 가 없으면(`kpis.md` 없음, 북극성·4축 누락) Phase 2 로 되돌린다.

## 0. 이미 있는 게임에 붙일 때 — 먼저 묻는다

`unity/Packages/manifest.json` 에 `com.google.firebase.*` 가 없거나 `spec.md` 의 `measurement.*` 가 비어 있으면 `AskUserQuestion` 1문항: "Firebase Analytics/Crashlytics 가 없습니다. 붙일까요?" (추천: 붙이기 — 시뮬레이터에서 안 나는 기기·GPU·광고 응답 크래시는 출시 뒤에야 보인다). 건너뛰면 `measurement.*: false` 와 사유를 `_notes` 에 남기고 다시 묻지 않는다. 무인 실행은 `on_ambiguity` 를 따르고 `decisions.log` 에 남긴다.

## 1. Firebase 콘솔 — 앱 등록과 설정 파일

Firebase 앱 생성은 API 로 못 하니 Playwright MCP 로 `console.firebase.google.com` 을 조작한다(headless 금지, 로그인은 사용자가 창에서 직접 — 자격 증명을 입력하지 않는다).

1. 프로젝트 선택 또는 생성 `{slug}-prod` (Google Analytics ON)
2. iOS 앱: bundle ID = `AppInfo.BundleId` → `GoogleService-Info.plist` 다운로드
3. Android 앱: 패키지 = `AppInfo.BundleId` (SHA-1 비움) → `google-services.json` 다운로드
4. 원본은 레포 루트 `firebase/` 에 둔다(gitignore). Firebase Unity 는 `Assets/` 아래 파일을 읽으므로 빌드 전에 `unity/Assets/Firebase/` 로 복사한다 — `unity/.gitignore` 의 `**/GoogleService-Info.plist`, `**/google-services.json` 이 막는다. `git status` 에 보이면 즉시 중단
5. selector 가 깨지면 진행 상황을 `_workspace/implementation/firebase-manual.md` 에 적고 사용자에게 수동 등록·다운로드를 부탁한 뒤 4 부터 재개

## 2. 패키지 — 정확한 버전으로

Google 공식 배포인 UPM tarball(`com.google.firebase.app`, `.analytics`, `.crashlytics` 13.17.0 + `com.google.external-dependency-manager`)을 `unity/Packages/` 아래에 두고 `manifest.json` 에서 참조한다. 같은 Firebase 버전끼리만 쓴다. 버전을 올릴 때는 세 패키지를 같이 올리고 CLAUDE.md 의 버전 표도 고친다.

- EDM4U(External Dependency Manager)가 iOS 에 CocoaPods 를 붙인다. iOS Resolver 는 "Podfile 생성 + Xcode workspace" 로 둔다. 이후 Unity iOS 빌드는 `Unity-iPhone.xcworkspace` 를 만들고 `fastlane ios build_unity` 가 자동으로 workspace 를 빌드한다
- GMA Unity 와 함께 쓸 때 두 SDK 가 요구하는 iOS·Android 최소 버전과 EDM4U 버전이 맞는지 첫 빌드에서 확인한다(링크 실패는 `deploy-build-troubleshooting.md`)

## 3. 래퍼 — `Game.Platform` 에만

SDK `using Firebase*` 는 Platform 어셈블리 밖에 두지 않는다(check-harness 가 잡는다).

```
unity/Assets/Game/Platform/
├── Analytics.cs          # Init(), Log(name, params), SetUserProperty(), RecordError(Exception)
├── AnalyticsEvent.cs     # KPI 카탈로그 상수: public const string LevelComplete = "complete_level";
└── AppEnv.cs             # IsProd => !Application.isEditor && !Debug.isDebugBuild
```

- `Init()`: `FirebaseApp.CheckAndFixDependenciesAsync()` 가 `Available` 일 때만 SDK 를 쓴다. 실패하면 no-op 로 남고 게임은 계속된다
- 수집은 `AppEnv.IsProd` 에서만: `FirebaseAnalytics.SetAnalyticsCollectionEnabled(AppEnv.IsProd)`. 네이티브 자동 수집이 Init 전에 켜지지 않게 iOS Info.plist `FIREBASE_ANALYTICS_COLLECTION_ENABLED=NO`, Android manifest meta-data `firebase_analytics_collection_enabled=false` 를 빌드 후처리로 넣는다
- Crashlytics: `Crashlytics.ReportUncaughtExceptionsAsFatal = true`. 자동 수집은 끄지 않는다 — 첫 실행 크래시도 올라와야 한다. 처리된 오류는 `Analytics.RecordError(e)` → `Crashlytics.LogException(e)`
- 이벤트 이름은 `AnalyticsEvent` 상수만, snake_case `동사_명사`, 40자 이하. 파라미터에 PII·토큰 금지
- 에디터·Development Build 는 로그만 남긴다(`[Game] analytics <name>`)

## 4. 배선

1. 첫 씬의 첫 컴포넌트(`GameController.Awake`)에서 `Analytics.Init()` 를 한 번 부른다. 광고 초기화(`Ads.InitializeWithConsent()`)와 병렬이어도 되고 첫 화면을 막지 않는다(await 하지 않는다)
2. 화면 상태 전환(`Enter(state)`)에서 `screen_view` 대신 `AnalyticsEvent.ScreenView` + 상태 이름을 기록한다 — 단일 씬이라 자동 화면 추적이 없다
3. PRD 의 북극성·획득·활성·유지·수익화 이벤트를 각 1개 이상 실제 코드 경로에 넣는다 (예: `start_game`, `complete_game`, `achieve_best`, `earn_reward`, `purchase_item`)
4. 크래시가 아니어도 플레이가 끊기는 `catch`(저장 실패, 광고 로드 예외, 네이티브 브리지 예외)에서 `RecordError`

## 5. 검증

- `npm run verify` green, `npm run unity -- generate` 성공
- `fastlane ios build_unity_sim` → 시뮬레이터 실행 → `xcrun simctl launch --console-pty booted <bundle id>` 로 `[Game] analytics` 로그와 Firebase 초기화 로그 확인
- iOS 기기 빌드 로그에서 Crashlytics dSYM 업로드 run script 확인. Android IL2CPP 는 Player 설정의 symbols.zip 을 만들고 `firebase crashlytics:symbols:upload --app=<APP_ID> <symbols.zip>` 로 올린다
- (선택) TestFlight 운영 빌드에서 진단용 강제 예외로 Crashlytics 콘솔 수신 확인. 진단 코드는 커밋하지 않는다

## Sprint Contract (`_workspace/implementation/sprint-contract-4d.md`)

```markdown
## Done 조건
- [ ] Firebase 콘솔에 iOS/Android 앱 등록, bundle id = AppInfo.BundleId
- [ ] 설정 파일이 firebase/ 와 unity/Assets/Firebase/ 에 있고 git ls-files 에 0건
- [ ] Firebase 13.17.0 패키지 3개 + EDM4U 가 manifest 에 정확한 버전으로
- [ ] Platform/Analytics.cs 래퍼, AnalyticsEvent 상수 = kpis.md 카탈로그 1:1
- [ ] 수집은 AppEnv.IsProd 에서만, Crashlytics 자동 수집은 켜 둠
- [ ] 북극성 + 4축 이벤트 각 1개 이상 호출
- [ ] npm run verify green, generate 성공, 시뮬레이터 로그 확인
```

## Quick QA (4d-QA, `qa-reviewer`)

- `npm run verify` (Platform 밖 `using Firebase` 0건은 check-harness 가 본다)
- 문자열 리터럴로 `Analytics.Log("...")` 를 부르는 곳 0건 — `AnalyticsEvent.*` 만
- 이벤트 파라미터에 이메일·이름·정확 위치·토큰 0건
- `GoogleService-Info.plist`·`google-services.json` 커밋 0건

PASS → Phase 5. FAIL → platform-integrator 수정(최대 2회).

## 오류

| 증상 | 조치 |
|------|------|
| `CheckAndFixDependenciesAsync` 가 `Available` 이 아님 | Android Google Play services 문제. 래퍼가 no-op 로 남는지 확인하고 로그만 남긴다 |
| iOS 링크 실패(중복 심볼, 최소 버전) | workspace 를 빌드했는지, Podfile 의 platform 버전, EDM4U 재해결 → `deploy-build-troubleshooting.md` |
| 콘솔 권한 없음 | `_workspace/implementation/error-4d.md` 에 기록, 사용자에게 권한 요청 |
| 콘솔 미로그인 | 사용자에게 창에서 로그인 요청 후 대기. 자격 증명 자동 입력 금지 |
