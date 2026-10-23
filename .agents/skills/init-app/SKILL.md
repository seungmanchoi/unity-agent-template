---
name: init-app
description: "이 템플릿을 새 Unity 게임으로 바꾸는 첫 단계 스킬. slug·앱 이름·bundle id(AppInfo.cs)·package.json 이름·C# 네임스페이스(어셈블리·Assets 폴더)를 한 번에 바꾸고, 아이콘·스토어 레코드·Firebase/AdMob 등 남은 수동 단계를 안내한다. '새 게임 시작', '새 앱 시작', '템플릿으로 게임 만들기', '앱 이름 바꿔줘', 'init app', 'bootstrap' 요청 시, 그리고 orchestrate 가 AppInfo.cs 의 Slug 가 아직 my-game 인 걸 발견했을 때 사용할 것."
---

# Init App — 템플릿 → 새 게임

템플릿 식별자(`my-game` / `My Game` / 네임스페이스 `Game`)를 새 게임 값으로 바꾼다. 코드는 만들지 않는다.

## 언제
- `unity/Assets/Game/Editor/AppInfo.cs` 의 `Slug = "my-game"` 이 그대로일 때
- 이미 바꿨으면 스크립트가 거부한다. 이름만 고칠 때는 `--force`

## Step 1: 값 정하기
사용자 요청에서 뽑고, 없을 때만 묻는다.

| 값 | 규칙 |
|----|------|
| `slug` | kebab-case 영문 소문자. bundle id 는 `com.seungmanchoi.{slug에서 - 제거}` (iOS·Android 같은 값). **첫 업로드 뒤에는 바꿀 수 없다**. Play Console 에 같은 패키지명이 없는지 확인한다 |
| `name` | 영어 표시 이름, 30자 이하. 스토어 `name.txt`/`title.txt` 와 같아야 한다 |
| `namespace` | PascalCase. 기본값은 slug 의 PascalCase(`pocket-pulse` → `PocketPulse`) |

## Step 2: 실행
```bash
npm run app:init -- --slug pocket-pulse --name "Pocket Pulse" [--namespace PocketPulse]
```
- `AppInfo.cs` 의 `Slug`·`Name`·`BundleId`, `package.json` 의 `name` 을 바꾼다. fastlane 은 `AppInfo.cs` 를 읽는다
- 네임스페이스가 바뀌면 어셈블리 이름(`Game.Domain` → `PocketPulse.Domain` …), `namespace`/`using`, 로그 태그 `[Game]`, `unity/Assets/Game/` 폴더와 asmdef 파일 이름, 문서·Codex 사본 속 경로를 바꾼다. `.meta` 를 같이 옮겨 GUID 는 그대로다

## Step 3: 남은 수동 단계
1. **검증**: `npm run sync:codex && npm run verify`
2. **재생성**: `npm run unity -- generate` — 씬을 다시 만들고 새 이름·bundle id 를 ProjectSettings 에 반영한다. 바뀐 `ProjectSettings/ProjectSettings.asset` 을 커밋한다
3. **아이콘**: `unity/Assets/<Ns>/Content/AppIcon.png` 를 1024×1024, **알파 없는** PNG 로 교체(아이콘이 없으면 App Store 가 거부한다)
4. **스토어 레코드**: ASC 웹에서 앱 레코드를 먼저 만들고(API 로는 못 만든다) `fastlane ios create_app` 으로 bundle id 등록·확인. Play Console 에 앱을 만든다
5. **Firebase·AdMob**: 쓸 때만. `docs/rules/analytics.md`, `docs/rules/ads.md`. 광고를 쓰면 ATT 목적 문자열을 이 게임에 맞게 새로 쓴다
6. **홈 화면 다국어 이름**: 스토어에 다국어 이름을 넣으면 iOS PostProcessBuild 로 `<lang>.lproj/InfoPlist.strings` 의 `CFBundleDisplayName` 을 맞춰야 한다. 템플릿에는 아직 없다(`docs/rules/build-deploy.md` TODO)
7. **시뮬레이터 확인**: `fastlane ios build_unity_sim` → 설치·실행 → `sim-use ui` 로 이름과 첫 화면

## 다음
- 처음부터 만들기: `orchestrate`
- 하나씩: `create-module` / `create-screen`
