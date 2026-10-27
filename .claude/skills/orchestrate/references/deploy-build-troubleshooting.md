# Deploy & Build Troubleshooting (on-demand reference)

Phase 7(`/store-deploy`)과 Unity batchmode 빌드에서만 필요한 절차·증상 모음이다. 항상 적용되는 규칙은 CLAUDE.md 와 `docs/rules/build-deploy.md`, 일반 증상은 `docs/troubleshooting.md` 에 있다.

> 파이프라인: Unity batchmode(`npm run unity -- ios` 또는 `fastlane ios build_unity` 안에서) → `unity/Build/ios/Unity-iPhone.xcodeproj` → fastlane 서명·IPA → `fastlane ios upload`. Android 는 Unity 가 AAB 를 직접 만든다. 메타데이터·스크린샷·폼은 store-deploy 플러그인 스킬이 정본이다.

## 빌드 흐름 한눈에

```
fastlane ios build_unity
  1. latest_testflight_build_number + 1 → BUILD_NUMBER, AppInfo.Version → APP_VERSION
  2. Unity -batchmode -quit -executeMethod Game.Editor.Build.BuildIOS   (Generate 를 먼저 부른다)
  3. update_info_plist: ITSAppUsesNonExemptEncryption = false
  4. sigh (App Store 프로파일) → update_code_signing_settings (Unity-iPhone 타깃만)
  5. gym: workspace 가 있으면 Unity-iPhone.xcworkspace, 없으면 .xcodeproj → build-output/<slug>.ipa
fastlane ios upload → TestFlight (심사 제출 안 함)
```

## Unity batchmode

| 증상 | 원인 | 해결 |
|------|------|------|
| 빌드가 "Active Input Handling was not the Input System" 으로 exit 1 | `activeInputHandler` 가 1 이 아니었다. 백엔드는 에디터 재시작 뒤에만 바뀐다 | `Build` 가 이미 값을 바꿨다. 같은 명령을 한 번 더 실행한다. 바뀐 `ProjectSettings.asset` 은 커밋한다 |
| 처음 연 프로젝트에서 `Packages/manifest.json` 이 기본 패키지 목록으로 덮어써짐 | `ProjectSettings/` 가 없으면 Unity 가 새 프로젝트로 보고 기본 manifest 를 쓴다 | `ProjectSettings/*.asset` 은 항상 커밋한다. 덮어써졌으면 `git checkout unity/Packages` 후 `unity/Library` 를 지우고 다시 연다 |
| `-runTests` 가 테스트를 안 돌리고 끝남 | `-quit` 을 같이 줬다 | `-runTests` 에는 `-quit` 을 주지 않는다(`npm run unity -- test` 가 맞게 부른다) |
| RenderCheck PNG 가 검거나 비어 있음 | `-nographics` 로 실행했다 | GPU 가 있는 환경에서 `-nographics` 없이 실행 |
| "another Unity instance is running with this project" | 같은 `-projectPath` 로 에디터나 다른 batchmode 가 열려 있다 | 그 프로세스를 끝낸다. `unity/Temp/UnityLockfile` 은 프로세스가 없을 때만 지운다 |
| 라이선스 오류로 시작 실패 | Unity Hub 로그인·Personal 라이선스 미활성 | 사용자 작업: Hub 에서 로그인·활성화. Domain 테스트는 `npm test` 로 라이선스 없이 돈다 |
| `generate` 를 두 번 돌렸더니 git 변경이 생김 | 생성기가 멱등이 아니다(타임스탬프·랜덤·순서 비결정) | 생성기를 고친다. 생성물은 `Generated/` 로만, 그 폴더는 커밋하지 않는다 |
| 디스크 부족으로 IL2CPP·Xcode 단계 실패 | `unity/Library`(수백 MB) + IL2CPP 출력·Xcode 프로젝트(수 GB) + DerivedData | 빌드 전 여유 5GB+ 확인. 끝나면 `unity/Build/`, `build-output/sim-derived/` 를 지운다. 다른 프로젝트 캐시는 건드리지 않는다 |

## iOS

| 증상 | 원인 | 해결 |
|------|------|------|
| "UnityFramework does not support provisioning profiles" | 수동 서명을 모든 타깃에 걸었다 | `update_code_signing_settings(targets: ["Unity-iPhone"])` — 앱 타깃만 |
| 인증서·프로파일 오류 | 팀 공유 배포 인증서가 login.keychain 에 없다 | 새로 만들지 않는다. `~/works/CLAUDE.md` "iOS 배포 인증서" 절차로 한 번 import. 비밀번호를 명령줄에 넘기지 않는다 |
| GMA·Firebase 추가 뒤 링크 실패(`Undefined symbols`, 모듈 없음) | `.xcodeproj` 만 빌드해 Pods 가 빠졌다 | EDM4U iOS Resolver 를 "Podfile + Xcode workspace" 로 두고 `Unity-iPhone.xcworkspace` 를 빌드한다(Fastfile 이 자동 감지). `pod install` 실패는 Podfile 최소 iOS 버전과 SDK 요구 버전을 맞춘다 |
| 업로드마다 수출 규정 질문 | `ITSAppUsesNonExemptEncryption` 없음 | build_unity 가 `false` 를 넣는다. 표준 암호화 외를 쓰면 사용자와 확인 |
| ASC 거부: 아이콘 누락 / 알파 채널 | 1024 마케팅 아이콘이 없거나 투명도가 있다 | `Content/AppIcon.png` 1024×1024, 알파 없는 PNG. `Build.ApplyPlayerSettings` 가 `PlayerSettings.SetIcons` 로 넣는다 |
| "The bundle version must be higher" / 같은 build number 거부 | 같은 버전 안에서 build number 재사용 | build_unity 는 TestFlight 최신 +1 을 쓴다. 로컬에서 `BUILD_NUMBER` 를 직접 넣었으면 그 값을 확인 |
| 버전 업로드 불가 | ASC 에 더 높은 버전이 있다 | `AppInfo.Version` 을 올린다 |
| 시뮬레이터에 설치 안 됨 / 아키텍처 오류 | 기기 SDK 로 만든 Xcode 프로젝트 | `IOS_SIMULATOR=1`(`npm run unity -- ios-sim` / `fastlane ios build_unity_sim`) — Simulator SDK + ARM64 |
| `xcodebuild -showBuildSettings` 타임아웃 | 부하가 높을 때 기본 3초 제한 | Fastfile 이 `FASTLANE_XCODEBUILD_SETTINGS_TIMEOUT=120` 을 둔다. 더 필요하면 올린다 |
| Personal 라이선스 빌드에 Unity 스플래시 | Personal 은 스플래시를 끌 수 없다 | 정상. 심사 문제 아님 |

## Android

| 증상 | 원인 | 해결 |
|------|------|------|
| AAB 가 디버그 키로 서명됨 / Play 업로드 거부 | `ANDROID_KEYSTORE` 등 환경변수 미설정 | `ANDROID_KEYSTORE`, `ANDROID_KEY_ALIAS`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEY_PASS` 를 환경으로만 넘긴다. 키스토어·비밀번호를 레포·명령줄 인자에 두지 않는다 |
| Play "64-bit 필요" / ABI 경고 | 아키텍처 설정 | Build 는 IL2CPP + ARM64 만 만든다. 에뮬레이터 확인은 `npm run unity -- apk` (Apple Silicon 에뮬레이터는 arm64) |
| version code 중복 | 같은 versionCode 재업로드 | `fastlane android build_unity build:N` 에 Play 최신 +1 |
| 첫 AAB 업로드 실패 | draft 앱은 Play Developer API 업로드를 거부 | 첫 AAB 는 Play Console 웹에서 수동, 이후 `fastlane android internal` |
| Gradle·SDK 경로 오류 | Unity Hub 의 Android Build Support 모듈 미설치 | 사용자 작업: Hub 에서 Android Build Support(OpenJDK·SDK·NDK) 설치 |

## 배포 전 준비 (store-deploy 와 함께)

| 항목 | 설명 |
|------|------|
| 개인정보처리방침·지원 URL | `https://seungmanchoi.github.io/{slug}/privacy.html` (en/ko/ja/zh) |
| 아이콘 | iOS 1024 알파 없음, Android 512 + adaptive |
| 스크린샷 | 언어별 한 세트. 시뮬레이터 autoplay 캡처 → `store-screenshots`. iOS: iPhone 6.9" 1320×2868, iPhone Duo outer 1398×2034·inner 2007×2853(2027-04 부터 iOS 27.1 SDK 이상 바이너리에 필수). 규격·캡처·업로드 정본: store-deploy 플러그인 `store-screenshots` 스킬 |
| 릴리스 노트 | Android changelog 언어별 500자 이하. iOS 도 같은 글을 쓰니 500자 기준 |
| 버전 | `AppInfo.Version` 이 스토어 최신보다 높아야 한다 |
| 앱 크기 | 미사용 에셋·폰트 제거, 텍스처 압축(ASTC), 오디오 Vorbis/ADPCM, Managed Stripping Level 확인 |
