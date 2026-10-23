# 스토어 평점 유도 (In-App Review) (MANDATORY)

> CLAUDE.md 에서 옮긴 상세 규칙이다. **Hard Threshold 표는 CLAUDE.md 가 정본**이며, 여기 없는 통과 기준은 CLAUDE.md 를 따른다.

평점은 플레이어가 가치를 느낀 직후의 짧은 순간에만 요청한다. 잘못된 때 띄우면 ★1 이 쌓인다. 판정은 Domain 의 순수 정책 `ReviewPolicy` 가 하고(테스트 가능), 실제 요청은 Platform 의 `StoreReview.Request()` 브리지 하나로만 한다.

## 플랫폼 한계

- **iOS** `SKStoreReviewController`: 365일에 최대 3번만 실제로 뜨고, 넘친 호출은 조용히 무시된다. 표시 여부를 알려 주지 않는다.
- **Android** Play In-App Review: Google 이 쿼터를 관리하고 앱은 모른다. 리뷰 의향을 먼저 묻는 사전 프롬프트는 정책 위반이다.
- 둘 다 표시 여부를 돌려주지 않는다. 호출은 fire-and-forget 이다.

## 게이트 (모두 통과해야 요청)

| 게이트 | 기본값 |
|--------|--------|
| 설치 후 경과 | 3일 이상 |
| 누적 실행 | 5회 이상 |
| 핵심 행동(판 완료·승리 등) | 3회 이상 |
| 마지막 요청 이후 | 90일 이상 |
| 최근 365일 요청 수 | 3회 이하 |
| 이번 세션 요청 | 0회 |
| 실행 후 경과 | 120초 이상 |
| UI idle (전환·연출·광고·입력 중 아님) | true |
| 최근 5분 내 에러·크래시 | 없음 |

## 구조

```
unity/Assets/Game/Domain/Review/
├── ReviewPolicy.cs     CanRequest(ReviewState state, long nowUnixSec, bool uiIsIdle, ReviewRules rules)
├── ReviewState.cs      installedAt, sessionStartedAt, launchCount, keyActionCount, lastErrorAt, requestHistory[]
└── ReviewTriggers.cs   트리거 상수 (예: FirstWin, ThirdRunCompleted)
unity/Assets/Game/Platform/StoreReview.cs     Request(): iOS DllImport → AppStoreReview.mm, Android com.google.play.review
unity/Assets/Plugins/iOS/AppStoreReview.mm    [SKStoreReviewController requestReviewInScene:activeWindowScene]
```

- 시각은 호출자가 넘긴다(Domain 은 시계를 읽지 않는다). 상태는 비민감이라 PlayerPrefs 또는 진행 저장 JSON 에 둔다.
- Presentation 은 `MaybeRequestReview(ReviewTriggers.X)` 하나만 부른다: 핵심 행동 카운트 → `ReviewPolicy.CanRequest` → `StoreReview.Request()` → `request_store_review` 이벤트 기록.
- 실행 시 `RecordLaunch()`, 예외 핸들러(`Application.logMessageReceived` 의 Exception)에서 `RecordError()` 를 부른다.
- Android 플러그인은 Google Play In-App Review Unity 패키지(`com.google.play.review`)를 쓰고, 넣을 때 정확한 버전으로 고정한다.

## 안티패턴 (즉시 FAIL)

- 첫 실행·온보딩·튜토리얼 중 요청
- 패배·실패·결제 실패·광고 실패·권한 거부 직후 요청, 에러 핸들러 안 요청
- 자체 사전 프롬프트("평점 남겨 주실래요?")
- "별 5개 부탁" 같은 점수 유도 문구, 평점을 주지 않으면 기능을 막는 패턴
- 요청 결과에 따라 다음 UI 를 바꾸는 분기
- 정책을 거치지 않은 `StoreReview.Request()` 직접 호출, 매직 스트링 트리거

## 에이전트 분담

- **product-planner**: PRD "Review Triggers" — 어떤 가치 순간이 트리거인지
- **module-builder**: Domain `ReviewPolicy` + 테스트
- **platform-integrator**: `StoreReview` 브리지, 네이티브 플러그인
- **presentation-developer**: 성공 순간에 `MaybeRequestReview` 배선
- **배포 전 게이트**: 평점을 쓰기로 했는데 어디에도 배선되지 않았으면 빌드를 보류한다

## Hard Threshold

| 기준 | 임계값 |
|------|--------|
| `ReviewPolicy.CanRequest` 를 거치지 않은 요청 | **0개** |
| 네이티브 리뷰 API·플러그인을 Platform `StoreReview` 밖에서 호출 | **0개** |
| `ReviewTriggers` 상수가 아닌 트리거 | **0개** |
| UI idle·연 3회·실행 후 120초 게이트 누락 | **0개** |
| 에러 핸들러·온보딩·실패 직후 요청 | **0개** |
| 자체 사전 프롬프트 / 점수 유도 문구 | **0개** |
| 요청 결과에 의존한 후속 UI 분기 | **0개** |
