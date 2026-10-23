# 트러블슈팅 (Unity)

증상 → 원인 → 해결 순서로 적는다. 새로 겪은 문제는 여기에 한 항목씩 더한다.

## 프로젝트·패키지

**`manifest.json` 이 Unity 기본 패키지 목록(ai.navigation, timeline, collab-proxy …)으로 바뀌었다**
- 원인: `unity/ProjectSettings/` 가 없는 상태로 Unity 를 열면 새 프로젝트로 여겨 manifest 를 덮어쓴다.
- 해결: `ProjectSettings/` 는 항상 커밋한다. 이미 덮였으면 `git checkout unity/Packages/manifest.json`, `unity/Library` 와 `unity/Packages/packages-lock.json` 을 지우고 다시 `npm run unity -- generate`.

**Generate 로그에 "Active Input Handling switched to the Input System; restart Unity"**
- 원인: 입력 백엔드는 에디터 재시작 때만 바뀐다. Generate 가 설정을 고쳤다.
- 해결: 같은 명령을 한 번 더 돌린다. 빌드 진입점은 이 상태면 일부러 exit 1 로 멈춘다. 고친 `ProjectSettings.asset` 은 커밋한다.

**"It looks like another Unity instance is running with this project open"**
- 원인: 같은 프로젝트를 Unity 두 개가 열 수 없다(`unity/Temp/UnityLockfile`).
- 해결: 다른 batchmode·에디터를 끝낸다. 남은 프로세스가 없는데도 나오면 `unity/Temp/` 를 지운다.

**`.meta` 가 계속 새로 생기거나 참조가 `Missing` 으로 바뀐다**
- 원인: `.meta` 를 커밋하지 않았거나 손으로 만들었다.
- 해결: Unity 가 만든 `.meta` 를 그대로 커밋한다. 파일을 옮길 때는 `.meta` 도 같이 옮긴다(`git mv`).

## 테스트·렌더

**`-runTests` 를 돌렸는데 결과 파일이 없다**
- 원인: `-quit` 을 같이 넘기면 테스트가 돌기 전에 끝난다.
- 해결: `npm run unity -- test` 를 쓴다(`-quit` 없이 돈다).

**RenderCheck PNG 가 검거나 실패한다**
- 원인: `-nographics` 로 돌렸다. 렌더에는 GPU 가 필요하다.
- 해결: `npm run unity -- render`(플래그 없이). 원격·CI 처럼 GPU 가 없는 곳에서는 돌리지 않는다.

**`npm test` 가 "The current .NET SDK does not support targeting .NET 10.0"**
- 해결: `brew install dotnet`(SDK 10). dotnet 이 없으면 `npm run verify` 는 Domain 테스트를 건너뛰지만, 완료 보고 전에는 꼭 돌린다.

## 라이선스

**batchmode 가 "No valid Unity Editor license found" / Licensing 오류로 끝난다**
- 해결: Unity Hub 에 로그인하고 Personal 라이선스를 활성화한 뒤 다시 돌린다. Hub 를 한 번 띄워 두면 LicensingClient 가 살아난다.

## 디스크

**디스크가 모자라다**
- `unity/Library`(수백 MB, 지우면 다음 실행 때 다시 만든다), `unity/Build`, `build-output/` 을 지운다. 다른 프로젝트의 캐시는 건드리지 않는다.
- IL2CPP iOS/Android 빌드는 산출물만 수 GB 가 될 수 있다. 빌드 전 여유를 확인한다.

## 빌드·서명

**gym 이 "UnityFramework does not support provisioning profiles"**
- 원인: 수동 서명을 모든 타깃에 걸었다.
- 해결: `update_code_signing_settings(targets: ["Unity-iPhone"])` 만(템플릿 lane 이 이미 그렇게 한다).

**codesign 이 키체인 비밀번호를 묻거나 "errSecInternalComponent"**
- 해결: `~/works/CLAUDE.md` "iOS 배포 인증서" 절차(`security set-key-partition-list …`). 새 인증서를 만들지 않는다.

**`xcodebuild -showBuildSettings` 시간 초과**
- 해결: `FASTLANE_XCODEBUILD_SETTINGS_TIMEOUT=120`(Fastfile 기본값). 다른 빌드가 동시에 돌고 있으면 끝난 뒤 다시.

**GMA·Firebase 를 넣은 뒤 iOS 링크 오류 / Pod 를 못 찾는다**
- 원인: EDM4U 가 `pod install` 을 못 돌렸다(CocoaPods 없음, 저장소 갱신 실패).
- 해결: `brew install cocoapods`, `cd unity/Build/ios && pod install --repo-update`. 이후 `Unity-iPhone.xcworkspace` 가 생기고 fastlane 이 자동으로 쓴다.

**App Store 업로드가 아이콘 누락으로 거부된다**
- 해결: `AppIcon.png` 1024×1024 알파 없음 확인 후 다시 Generate·빌드.
