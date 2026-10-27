# Phase 7 Step 7.0: 배포 전 재검증 게이트 (Pre-Deploy Gate)

> orchestrate Phase 7 에 들어갈 때 Read 하고 그대로 따른다.

게시된 빌드에서만 드러나거나 출시 뒤에는 코드·콘솔로만 고칠 수 있는 것들을 빌드 직전에 다시 확인한다. 시뮬레이터에서 검증할 수 없는 항목은 코드·설정·산출물 상태로 판정한다. **하나라도 미통과면 빌드·제출을 보류한다.** 사용자 승인으로도 Hard Threshold 위반은 넘기지 않는다.

## (a) 코드·검증

| 확인 | 통과 기준 | 미통과 시 |
|------|----------|----------|
| verify | 배포할 커밋에서 `npm run verify` green (check:harness + Domain dotnet test) | 배포 보류, 해당 에이전트로 |
| 생성기 멱등 | `npm run unity -- generate` 를 두 번 돌린 뒤 `git status` 가 깨끗 | 생성기 수정 (presentation-developer) |
| EditMode 테스트 | `npm run unity -- test` 전부 통과 | module-builder |
| HIGH 이슈 | `_workspace/qa/unresolved.md` 에 HIGH 0건 | 보류 |
| 런타임 증거 | app-inspector 가 시뮬레이터에서 첫 실행 → 한 판 → 결과 → 재시작 흐름을 `sim-use` 로 확인하고 기기·커밋·캡처를 `_workspace/qa/runtime/` 에 남김. 못 띄웠으면 `BLOCKED` 이고 PASS 아님 | 실행 가능한 환경에서 재검증 |
| 디자인 리뷰 | 최신 `docs/specs/<feature>/design-review-N.md` 가 Codex **GO**, 미해결 P0 없음 | `/design-review` 루프 |

## (b) 런타임 배선 — 출시 뒤에는 코드로만 고친다

| 확인 | 통과 기준 | 미통과 시 |
|------|----------|----------|
| 평점 트리거 | `ux.store_review=true` 면 승리·기록 갱신 같은 긍정 순간에 정책 엔진을 거친 요청이 최소 1곳. 패배·에러 직후, 첫 판, 자체 사전 팝업 없음 | presentation-developer / platform-integrator |
| 광고 동의 | 광고 앱이면 `Ads.InitializeWithConsent()`(UMP → ATT → RequestConfiguration → Initialize) 뒤에만 로드, 리워드는 보상 콜백에서만 지급, 전면 광고는 빈도 게이트 경유. AdMob 콘솔 GDPR·IDFA 메시지 Published | platform-integrator |
| 운영 광고 ID | 앱이 부르는 광고 단위마다 운영 ID 가 placeholder·테스트 ID 가 아님. 테스트 기기 ID 는 Development 빌드에서만 | 실제 단위 생성(`store-admob`) 후 재빌드 |
| ATT 문구 | `NSUserTrackingUsageDescription` 이 무엇(광고 식별자)·왜(관련성·설치 측정)·구체 예시·거부 시 결과를 담고 언어별로 있음. "deliver personalized ads to you" 류 상투 문구 금지 | 다시 쓴다 (`docs/rules/ads.md`) |
| KPI 이벤트 | 북극성 + 4축 이벤트가 코드 경로에 배선, 수집은 운영 빌드에서만 | platform-integrator |

## (c) 산출물·스토어

| 확인 | 통과 기준 | 미통과 시 |
|------|----------|----------|
| IPA 식별자 | `unzip -p build-output/<slug>.ipa 'Payload/*.app/Info.plist' \| plutil -p -` 에서 `CFBundleIdentifier` = `AppInfo.BundleId`, `CFBundleShortVersionString` = `AppInfo.Version`, `CFBundleVersion` = TestFlight 최신 +1 | 재빌드 |
| AAB 식별자 | `bundletool dump manifest --bundle <aab>` 의 package·versionCode·versionName | 재빌드 |
| 운영 빌드 | Development Build 아님(`BuildOptions.Development` 없음, `Debug.isDebugBuild` false), autoplay 기본값 꺼짐 | 재빌드 |
| 아이콘 | 1024 알파 없는 PNG 가 들어감 (ASC 처리 오류 메일 없음) | `Content/AppIcon.png` 교체 |
| 앱 이름 일치 | `AppInfo.Name`, 언어별 홈 화면 이름, iOS `name.txt`, Android `title.txt` 가 언어별로 같음(30자 이하) | 동기화 후 재빌드 |
| 개인정보·지원 URL | `https://seungmanchoi.github.io/{slug}/privacy.html` 이 열리고 4개 언어 탭 | GitHub Pages 커밋·푸시 |
| 데이터 안전 라벨 | 들어간 SDK(GMA, Firebase Analytics·Crashlytics)와 Apple 개인정보 라벨·Play 데이터 안전이 일치 | `store-forms` 로 보정 |
| 권한 | 쓰는 권한만 선언(진동은 Android VIBRATE 만). 안 쓰는 권한 설명 문구 없음 | 후처리·manifest 정리 |
| app-ads.txt | 광고 앱이면 게시 + 스토어 리스팅 도메인 연결 | `store-admob` |
| IAP 상품 | IAP 앱이면 ASC·Play 에 상품 생성·승인 | 콘솔 등록 |
| iPhone Duo 스크린샷 (제출 직전) | 2027-04 이후 iOS 제출이고 바이너리가 iOS 27.1 SDK(Xcode 27.1) 이상으로 빌드됐으면, 제출할 빌드와 같은 커밋·설정으로 Duo 시뮬레이터에서 캡처한 outer·inner 스크린샷이 언어별로 있고 ASC 에 올라감. `--duo` letterbox 대체본은 준비용이라 통과가 아니다. 빌드 전에는 Duo 시뮬레이터(Xcode 27.1+)를 쓸 수 있는지만 본다 | iOS 제출 보류, `store-screenshots` Step 2d 로 네이티브 캡처 |
| 시크릿 | `GoogleService-Info.plist`·`google-services.json`·키스토어·`.p12`·`.p8` 커밋 0건 (check-harness 가 본다) | 커밋에서 제거 후 키 교체 검토 |

## 진행 순서 (모두 통과한 뒤)

```
0. (광고 앱) store-admob — 앱·광고 단위. --ad-types 는 spec monetization.ad_formats 와 같게
1. iOS:     fastlane ios build_unity → fastlane ios upload → fastlane ios tf_status
   Android: ANDROID_KEYSTORE=… fastlane android build_unity build:N → fastlane android internal
2. store-metadata → store-screenshots → store-forms
3. 심사 제출은 사용자 확인 뒤
```

store-deploy 플러그인: Claude 는 `.claude/settings.json` 이 등록, Codex 는 `references/runtime-adapters.md` 의 설치 명령.
