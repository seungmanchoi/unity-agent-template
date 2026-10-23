# 빌드 & 배포 (MANDATORY)

> CLAUDE.md 에서 옮긴 상세 규칙이다. **Hard Threshold 표는 CLAUDE.md 가 정본**이며, 여기 없는 통과 기준은 CLAUDE.md 를 따른다.

배포는 `store-deploy` 플러그인(`/store-deploy`)으로만 한다. 빌드 단계만 Unity 어댑터로 바꾼다: Unity batchmode 가 Xcode/Gradle 산출물을 만들고, 이 레포의 fastlane lane 이 서명·업로드한다. 클라우드 빌드나 Editor GUI 의 Build 버튼은 쓰지 않는다.

## 앱 식별자

`unity/Assets/Game/Editor/AppInfo.cs` 한 곳이 정본이다(slug, 표시 이름, bundle id `com.seungmanchoi.<slug에서 - 제거>`, 버전). `npm run app:init` 이 바꾸고, `Build.Generate()` 가 PlayerSettings 에, fastlane `Fastfile`/`Appfile` 이 정규식으로 읽어 쓴다. 버전을 올릴 때는 `AppInfo.Version` 만 고친다.

## iOS

```bash
fastlane ios create_app      # bundle id 등록 + ASC 레코드 확인 (레코드는 ASC 웹에서 먼저 만든다)
fastlane ios build_unity     # → build-output/<slug>.ipa
fastlane ios upload          # TestFlight 업로드 (심사 제출 없음)
fastlane ios tf_status       # 처리 상태·베타 그룹
fastlane ios invite email:someone@example.com
```

`build_unity` 순서:
1. 마지막 TestFlight 빌드 번호 + 1 (이력이 없으면 1)
2. Unity `BuildIOS` — env `APP_VERSION`(=`AppInfo.Version`), `BUILD_NUMBER` → `unity/Build/ios/`
3. `update_info_plist` 로 `ITSAppUsesNonExemptEncryption=false`
4. `sigh` 로 App Store 프로파일
5. `update_code_signing_settings` 를 **`Unity-iPhone` 타깃에만**(UnityFramework 에 걸면 "does not support provisioning profiles")
6. `gym` — EDM4U 가 만든 `Unity-iPhone.xcworkspace` 가 있으면 그것, 없으면 `.xcodeproj`

서명은 `login.keychain` 의 팀 공유 배포 인증서(`Apple Distribution: SeungMan Choi (6Y6T9LPHH3)`)로 한다. 새 인증서·전용 키체인을 만들지 않고, 비밀번호를 명령줄 인자로 넘기지 않는다(`~/works/CLAUDE.md` "iOS 배포 인증서").

## Android

```bash
export ANDROID_KEYSTORE=… ANDROID_KEY_ALIAS=… ANDROID_KEYSTORE_PASS=… ANDROID_KEY_PASS=…   # 셸 환경으로만
fastlane android build_unity build:12   # → unity/Build/android/<slug>.aab (IL2CPP, ARM64)
fastlane android internal               # internal 트랙 draft 업로드
```

- 새 앱의 첫 AAB 는 Play Developer API 가 거부한다. Play Console 웹에서 직접 올린 뒤부터 lane 을 쓴다.
- 업로드 키스토어와 비밀번호는 레포·명령줄 인자·로그에 두지 않는다.
- 개인 계정의 프로덕션 액세스 신청: `.claude/skills/orchestrate/references/play-production-access-application.md`.

## 제출 전 확인

```bash
unzip -p build-output/<slug>.ipa 'Payload/*.app/Info.plist' | plutil -p - | grep -E 'CFBundleShortVersionString|CFBundleVersion|CFBundleIdentifier|ITSAppUsesNonExemptEncryption'
```

- version = `AppInfo.Version`, build number 증가, bundle id = `AppInfo.BundleId`.
- 스토어 빌드는 **Development Build 가 아니다**(`Debug.isDebugBuild == false`). 그래야 Analytics 수집·운영 광고 ID 가 켜진다.
- 광고를 쓰면 운영 광고 단위 ID 가 placeholder 가 아닌지, ATT 문구가 앱 전용인지 본다(`ads.md`).

## 아이콘·스플래시

- 아이콘: `unity/Assets/Game/Content/AppIcon.png` 1024×1024, **알파 없음**. `PlayerSettings.SetIcons` 가 모든 크기로 늘린다. 아이콘이 없으면 App Store 가 반려한다.
- Personal 라이선스는 Unity 스플래시를 끌 수 없다. 출시 전에 알고 간다.

## 앱 이름 일관성

스토어 이름(`fastlane/metadata/ios/<lang>/name.txt`, `fastlane/metadata/android/<lang>/title.txt`)과 홈 화면 이름을 언어별로 같게, 30자 이내로 맞춘다. 기본 이름은 `AppInfo.Name`.

> **TODO (템플릿에 아직 없음)**: 언어별 홈 화면 이름. iOS 는 `Editor` 의 `[PostProcessBuild]` 가 `<lang>.lproj/InfoPlist.strings` 에 `CFBundleDisplayName` 을 쓰고 `PBXProject` 에 등록한다(ATT 문구와 같은 파일이니 한 후처리기로 같이 쓴다). Android 는 `Assets/Plugins/Android/res/values-<lang>/strings.xml` 에 `app_name` 을 둔다(Unity 6 는 `.androidlib` 로 감싸야 한다). 다국어 스토어 이름을 넣는 게임은 첫 출시 전에 만든다.

## 개인정보처리방침·메타데이터

`~/works/seungmanchoi.github.io/<slug>/privacy.html`(en/ko/ja/zh 탭), `privacy_url.txt`·`support_url.txt` 는 그 URL, `copyright.txt` 는 `© {현재 연도} Seungman Choi`(하드코딩 금지). 스크린샷·메타데이터·스토어 폼은 `store-deploy` 스킬이 한다.

## Hard Threshold

| 기준 | 임계값 |
|------|--------|
| `/store-deploy` + 이 레포 fastlane lane 외의 배포 경로 | **0개** |
| 스토어 제출 바이너리가 Development Build | **0개** |
| 새 배포 인증서·전용 키체인 생성 / 비밀번호를 명령줄 인자로 전달 | **0개** |
| 키스토어·`.p12`·`.p8` 커밋 | **0개** |
| 앱 아이콘 누락 또는 알파 채널 포함 | **0개** |
| 제출 전 version·build·bundle id 미확인 | **0개** |
| 스토어 이름 ≠ 홈 화면 이름 | **0개** |
