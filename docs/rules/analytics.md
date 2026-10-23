# Analytics & 핵심 지표 (MANDATORY)

> CLAUDE.md 에서 옮긴 상세 규칙이다. **Hard Threshold 표는 CLAUDE.md 가 정본**이며, 여기 없는 통과 기준은 CLAUDE.md 를 따른다.

모든 게임은 출시 직후부터 KPI 를 수집한다. PRD 에 북극성 지표 1개와 4축 기본 세트를 정의하고, 이벤트는 Firebase Unity SDK 를 Platform 의 `Analytics` 래퍼로만 보낸다. 템플릿에는 SDK 가 없다. 출시 빌드 전에 넣는다.

## KPI (PRD 필수)

| 축 | 예시 지표 |
|----|-----------|
| 북극성 | 주간 완료 판 수, D7 재방문 플레이어 비율 등 게임 핵심 가치 1개 |
| 획득 | 신규 설치, 첫 실행 → 튜토리얼 시작 |
| 활성 | 튜토리얼 완료율, 첫 판 완료율, 첫 승리까지 시간 |
| 유지 | D1/D7/D30, 세션 수/일, 세션 길이 |
| 수익화 | 광고 노출/DAU, 리워드 시청률, IAP 전환 |

## 표준 스택

| 영역 | 패키지 | 비고 |
|------|--------|------|
| Analytics | Firebase Unity SDK **13.17.0** `FirebaseAnalytics` | 정확히 고정. `.unitypackage` 를 batchmode `-importPackage` 로 넣는다 |
| Crash | 같은 버전 `FirebaseCrashlytics` | 릴리스 빌드에서 항상 켠다 |

## 래퍼 (Platform)

```
unity/Assets/Game/Platform/Analytics/
├── Analytics.cs        Init(), LogEvent(name, params), SetUserProperty, RecordError(Exception)
├── AnalyticsEvents.cs  이벤트 이름 상수 (snake_case 동사_명사: start_game, complete_level, watch_rewarded_ad)
└── AppEnv.cs           IsProd => !Application.isEditor && !Debug.isDebugBuild
```

- `Init()` 은 `FirebaseApp.CheckAndFixDependenciesAsync()` 가 `Available` 을 돌려준 뒤에만 Firebase 를 만진다. 그 전 호출은 버리거나 큐에 쌓는다.
- `FirebaseAnalytics.SetAnalyticsCollectionEnabled(AppEnv.IsProd)` — 에디터·Development Build 는 수집하지 않는다.
- 이벤트 이름은 `AnalyticsEvents` 상수만. 매직 스트링 금지. 파라미터에 PII(이메일, 이름, 기기 식별자 원문, 토큰) 금지.
- 화면 상태가 바뀔 때마다 `screen_view`(`screen_name` = 상태 이름)를 래퍼가 기록한다.
- Crashlytics 자동 수집을 끄지 않는다. 첫 실행 크래시가 올라와야 한다. 잡은 예외는 `Analytics.RecordError` 로 남긴다.
- Presentation·Domain 은 `using Firebase` 를 쓰지 않는다(verify 가 잡는다).

## 설정 파일

- `GoogleService-Info.plist` / `google-services.json` 원본은 레포 루트 `firebase/` 에 둔다(gitignore).
- 빌드 전에 `unity/Assets/Firebase/` 로 복사한다(`unity/.gitignore` 의 `**/GoogleService-Info.plist` 로 역시 커밋되지 않는다). 복사는 Editor 빌드 진입점에서 하고, 파일이 없으면 릴리스 빌드를 실패시킨다.
- bundle id 는 `AppInfo.BundleId` 와 같아야 한다.

## 에이전트 분담

- **product-planner**: PRD KPI 표와 이벤트 카탈로그
- **platform-integrator**: SDK 설치, 래퍼, 설정 파일 복사, Crashlytics
- **presentation-developer**: 상태 전환·핵심 행동에서 `Analytics.LogEvent(AnalyticsEvents.X, …)`
- **qa-reviewer**: 아래 표 검사

## Hard Threshold

| 기준 | 임계값 |
|------|--------|
| PRD 에 KPI 정의 누락 (북극성 + 4축) | **0개** |
| Platform 밖 `using Firebase` / Firebase 직접 호출 | **0개** |
| `AnalyticsEvents` 상수를 거치지 않은 이벤트 이름 | **0개** |
| PII 가 담긴 이벤트 파라미터·user property | **0개** |
| 에디터·Development Build 에서 수집 | **0개** |
| Crashlytics 자동 수집 끄기 | **0개** |
| `GoogleService-Info.plist` / `google-services.json` 커밋 | **0개** |
