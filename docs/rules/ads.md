# 광고 동의 시퀀스 / 무효 트래픽 방어 (MANDATORY)

> CLAUDE.md 에서 옮긴 상세 규칙이다. **Hard Threshold 표는 CLAUDE.md 가 정본**이며, 여기 없는 통과 기준은 CLAUDE.md 를 따른다.

AdMob 을 쓰는 게임은 Google Mobile Ads Unity 플러그인을 Platform 어셈블리의 `Ads` 어댑터 하나로만 부른다. 동의 순서가 어긋나면 첫 요청이 동의 정보 없이 나가 EU 트래픽은 비개인화로만 팔리고, iOS 는 ATT 를 못 받아 IDFA 를 쓰지 못한다. 템플릿에는 SDK 가 들어 있지 않다. 광고를 쓰기로 했을 때 아래대로 넣는다.

## 표준 스택

| 영역 | 패키지 | 비고 |
|------|--------|------|
| AdMob + UMP | Google Mobile Ads Unity plugin **11.5.0 (정확히 고정)** | UMP(`GoogleMobileAds.Ump.Api`) 포함. EDM4U 가 iOS Pod·Android Gradle 의존성을 푼다 |
| iOS ATT | `Assets/Plugins/iOS/AppTracking.mm` 네이티브 브리지 | `ATTrackingManager.requestTrackingAuthorization`. 별도 패키지를 쓰지 않는다 |
| 목적 문자열 | `Editor` 의 iOS `PostProcessBuild` | `PlistDocument` 로 `NSUserTrackingUsageDescription` + `<lang>.lproj/InfoPlist.strings` |

설치(둘 중 하나, 버전 고정):
- `.unitypackage`: GitHub `googleads/googleads-mobile-unity` 릴리스 11.5.0 을 batchmode 로 import(`-importPackage <file> -quit`).
- OpenUPM: `manifest.json` 에 `scopedRegistries`(`https://package.openupm.com`, scope `com.google`)를 넣고 `"com.google.ads.mobile": "11.5.0"`.

설치 뒤 `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset` 에 iOS·Android App ID 를 넣는다(비밀값이 아니라 커밋한다. GUI 대신 Editor 스크립트로 설정한다). iOS 빌드는 EDM4U 가 `pod install` 로 `Unity-iPhone.xcworkspace` 를 만들고, fastlane `build_unity` 가 workspace 를 자동으로 고른다.

## 표준 시퀀스 (변경 금지)

```
ConsentInformation.Update(request, …)          UMP: 동의 정보 갱신
  → ConsentForm.LoadAndShowConsentFormIfRequired   EU 사용자에게 폼 (비 EU 는 no-op)
  → ATT 요청 (iOS 네이티브 브리지, Android 건너뜀)
  → MobileAds.SetRequestConfiguration(…)        테스트 기기 ID, 콘텐츠 등급
  → MobileAds.Initialize(_ => IsReady = true)
```

- UMP 가 먼저여야 GDPR 거부 시 ATT 와 무관하게 개인화가 꺼지고 두 다이얼로그가 이어서 뜬다.
- `SetRequestConfiguration` 은 `Initialize` 전이어야 첫 요청부터 반영된다.
- `ConsentInformation.CanRequestAds()` 가 false 면 `Initialize` 하지 않는다.
- `MobileAds.RaiseAdEventsOnUnityMainThread = true` 를 초기화 전에 둔다. 콜백에서 Unity API 를 만지기 때문이다.

## 구현 위치

```
unity/Assets/Game/Platform/Ads/
├── Ads.cs            InitializeWithConsent(), IsReady, ShowPrivacyOptions(), PrivacyOptionsRequired
├── AdGates.cs        CanShowInterstitial() — 공유 쿨다운 · 일일 cap (판정 로직은 Domain 에 두고 테스트)
├── RewardedSlot.cs   로드·표시·백오프 재시도
└── AdUnits.cs        광고 단위 ID (테스트 ID / 운영 ID)
unity/Assets/Plugins/iOS/AppTracking.mm
```

- Presentation 은 앱 시작 때 `Ads.InitializeWithConsent()` 만 부르고 기다리지 않는다(폼·ATT 는 첫 화면 위에 뜬다).
- 모든 광고 로드(`BannerView`, `InterstitialAd.Load`, `RewardedAd.Load`)는 `Ads.IsReady` 뒤에만 시작한다.
- `ConsentInformation.*`, `ConsentForm.*`, `MobileAds.*` 를 Platform 밖에서 부르지 않는다(verify 가 `using GoogleMobileAds` 를 Platform 밖에서 잡는다).

## ATT 목적 문자열 — 상투 문구는 자동 반려된다 (CRITICAL)

실제 반려(poly-dash 1.0.10, 2026-08-06, Guideline 5.1.1): `"This identifier will be used to deliver personalized ads to you."` 를 placeholder 로 판정했다. 게임마다 언어별로 새로 쓰고 아래 4가지를 모두 담는다.

1. 무엇을 — 기기의 광고 식별자
2. 왜 — 광고 관련성 향상과 설치 측정
3. 구체적 예시 — "관련 없는 상품 대신 이 게임과 비슷한 게임 광고를 보여준다"
4. 거부 시 결과 — "광고는 계속 나오며 맞춤 광고가 아닐 뿐"

PostProcessBuild 가 `Info.plist` 기본값과 각 `<lang>.lproj/InfoPlist.strings` 를 쓰고 `PBXProject` 에 등록한다. 검증:

```bash
grep -rn "deliver personalized ads to you" unity/Assets   # 0건
ls unity/Build/ios/*.lproj/InfoPlist.strings              # 지원 언어 수와 같아야 한다
```

## AdMob 콘솔 (배포 전 한 번)

- Privacy & messaging 의 **GDPR 메시지와 IDFA 메시지를 게시(Published)** 한다. 게시 전에는 UMP 가 늘 `NotRequired` 를 돌려 폼이 뜨지 않는다.
- 콘솔 자동화(앱 등록·광고 단위·메시지 게시)는 `store-deploy:store-admob` 스킬을 쓴다.

## Privacy Options

`ConsentInformation.PrivacyOptionsRequirementStatus == Required` 면 설정 화면에 "광고 개인정보 설정" 버튼을 두고 `ConsentForm.ShowPrivacyOptionsForm` 을 부른다. 상태는 동의 갱신 뒤마다 다시 읽는다(오프라인 첫 실행 뒤 재시도에서 바뀐다).

## 무효 트래픽 방어 (계정 정지 방지)

| 규칙 | 구현 |
|------|------|
| 화면당 배너 1개, 숨김·가림·UI 겹침 금지 | 배너 영역을 safe area 안 고정 위치에 예약. 미로드 시 영역을 접는다 |
| 전면 광고는 게이트 경유 | `CanShowInterstitial()` — 전면 광고 공유 쿨다운 + 일일 cap. 핵심 조작 중·직전 금지 |
| 리워드는 보상 콜백에서만 지급 | `rewardedAd.Show(reward => Grant(...))` 안에서만. 닫힘·실패 콜백에서 지급 금지 |
| 로드 실패 재시도 | 지수 백오프(예: 2·4·8·… 최대 60초). 즉시 무한 재시도 금지 |
| 테스트 트래픽 격리 | 개발 빌드는 Google 테스트 광고 단위 ID. 운영 ID 내부 빌드는 `RequestConfiguration.TestDeviceIds` 등록 기기에서만. 본인 광고 클릭 금지 |
| 계정 레벨 | app-ads.txt 게시, 스토어 리스팅 연결(`store-admob` 스킬) |

## Analytics 기록 (권장)

user property `ump_status`, `ump_can_request_ads`, `att_status` 를 남기면 동의 상태별 eCPM 코호트를 나눠 볼 수 있다(`analytics.md` 래퍼 경유).

## 에이전트 분담

- **product-planner**: PRD "광고 정책" — placement, 빈도 상한, 쿨다운·cap 수치
- **platform-integrator**: SDK 설치, `Ads` 어댑터, ATT 브리지, PostProcessBuild, 콘솔 설정
- **presentation-developer**: 배너 영역·전면 광고 시점·리워드 버튼 배치
- **qa-reviewer / app-inspector**: 순서·게이트·배치 검사, 시뮬레이터에서 동의 흐름 확인

## Hard Threshold

| 기준 | 임계값 |
|------|--------|
| GMA 플러그인 버전이 정확히 고정되지 않음 | **0개** |
| Platform `Ads` 밖에서 `MobileAds.*` / `ConsentInformation.*` / `ConsentForm.*` 호출 | **0개** |
| UMP → ATT → `SetRequestConfiguration` → `Initialize` 순서 위반 | **0개** |
| `Ads.IsReady` 전 광고 로드 | **0개** |
| `NSUserTrackingUsageDescription` 누락 / 상투 문구 / 언어별 InfoPlist.strings 누락 | **0개** |
| AdMob GDPR·IDFA 메시지 미게시 상태로 배포 | **0개** |
| Privacy Options 필요한데 설정 화면 진입점 없음 | **0개** |
| 한 화면 배너 2개 이상 / 숨겨진·가려진 광고 | **0개** |
| 전면 광고가 `CanShowInterstitial()` 미경유 | **0개** |
| 리워드 보상 콜백 밖 지급 | **0개** |
| 백오프 없는 로드 재시도 | **0개** |
| 운영 광고 ID 빌드를 테스트 기기 미등록 상태로 내부 배포 | **0개** |
