# CLAUDE.md

This file provides guidance to Claude Code when working with this repository.

## Project Overview

Unity 모바일 게임 템플릿. **AI Agent Harness**(에이전트 10개 · 스킬 11개 · verify 게이트)로 아이디어부터 스토어 배포, 출시 후 고도화까지 Claude Code 와 Codex 가 같이 진행한다. 에이전트는 Unity Editor GUI 를 쓰지 않는다. 코드와 원본 에셋이 정본이고 씬은 Editor 스크립트가 batchmode 로 만든다.

## Tech Stack

- **Engine**: Unity **6000.3.26f1** (Personal 라이선스 — Unity 스플래시가 나온다), iOS · Android, IL2CPP ARM64, 세로 고정
- **Packages (고정)**: Input System **1.20.0** (Active Input Handling = Input System) · Test Framework **1.6.0** · uGUI **2.0.0**
- **UI**: 코드로 만드는 uGUI. CanvasScaler 기준 1080×2340 가로 맞춤, safe area RectTransform, Noto Sans KR 동적 폰트(OFL)
- **Tests**: Domain 은 `dotnet test`(net10.0 · C# 9 · NUnit, Unity 라이선스 불필요), Unity EditMode 는 `-runTests`
- **Build/Deploy**: Unity batchmode → Xcode/Gradle → fastlane (`build_unity` → `upload`), `/store-deploy` 플러그인
- **SDK (필요할 때 Platform 에 추가)**: Google Mobile Ads Unity 11.5.0 (UMP 포함) · Firebase Unity 13.17.0 (Analytics · Crashlytics)
- **Harness scripts**: Node 20+ (`package.json` 은 하네스 스크립트만, 런타임 의존성 없음)

## Harness Engineering Rules (MANDATORY)

**이 프로젝트의 모든 작업은 아래 하네스 규칙을 따른다.** 에이전트와 스킬을 반드시 활용하여 작업한다.

### 핵심 규칙

0. **새 게임 시작**: `unity/Assets/Game/Editor/AppInfo.cs` 의 `Slug` 가 아직 `my-game` 이면 `init-app` 스킬(`npm run app:init`)부터 실행한다.
1. **Editor GUI 금지**: C#·JSON·원본 에셋이 정본이다. 씬·프리팹·머티리얼은 `Editor/Build.cs` 의 `Generate()` 가 만든다(반복 실행해도 같은 결과). Unity YAML 과 `.meta` GUID 를 손으로 쓰지 않는다. `.meta` 는 Unity 가 만든 것을 커밋한다.
2. **레이어 규칙**: 모듈·화면·플랫폼 어댑터를 만들 때 해당 에이전트 정의(`.claude/agents/`)를 Read 하고 따른다. 어셈블리 의존 방향은 아래 "Architecture" 를 지킨다.
3. **풀 게임 개발 시**: `/orchestrate` 스킬의 파이프라인을 따른다. Phase 를 건너뛰지 않는다.
4. **QA 는 생략할 수 없다**: 구현 뒤 반드시 `npm run verify`(check:harness · Domain `dotnet test`)를 실행하고, C# 을 바꿨으면 `npm run unity -- generate` 로 Unity 컴파일까지 확인한다. Stop hook(Claude `.claude/settings.json`, Codex `.codex/hooks.json`)이 코드가 바뀐 턴마다 verify 를 돌려 실패를 되돌려 주지만, hook 이 없어도 직접 실행한다.
5. **디자인은 Codex 가 맡는다**: 화면이 바뀌는 작업은 시뮬레이터 실제 캡처로 `design-review` 스킬(Codex 4축 평가: Design Quality · Originality · Craft · Functionality)을 돌리고, P0/P1 을 고친 뒤 재캡처·재리뷰해 GO 를 받는다.
6. **`_workspace/`**: 에이전트 간 데이터는 `_workspace/` 파일로 넘긴다. `spec.md`·`plan/`·`iterate/` 는 커밋 대상이고 나머지는 임시 산출물이다.
7. **배포 시**: `/store-deploy` 스킬과 이 레포의 fastlane lane 만 쓴다(아래 "Build & Deploy").
8. **상세 규칙 문서**: Unity 작업 방식·광고·Analytics·평점·보안 저장·빌드/배포·런타임 테스트·도구를 건드리는 작업은 시작 전에 해당 `docs/rules/*.md` 를 Read 하고 따른다. 이 파일에는 요약과 Hard Threshold 만 있다.

### Hard Thresholds (위반 시 작업 FAIL)

`scripts/check-harness.mjs` 가 기계적으로 잡는 항목은 ✓ 로 표시했다.

| 기준 | 임계값 |
|------|--------|
| `npm run verify` 실패 (check:harness · Domain 테스트) | **0개** ✓ |
| Unity 컴파일 에러 (`npm run unity -- generate` 로그의 `error CS`) | **0개** |
| asmdef 의존 방향 위반 (Domain ← Platform ← Presentation ← Editor, Tests → Domain) | **0개** ✓ |
| Domain 의 `UnityEngine`/`UnityEditor` 사용, `noEngineReferences` 누락 | **0개** ✓ |
| Domain 의 시계 읽기(`DateTime.Now`·`Stopwatch`·`TickCount`)·`System.Random` | **0개** ✓ |
| Platform 밖의 SDK `using`(GoogleMobileAds·Firebase·Google.Play 등)·`[DllImport]` | **0개** ✓ |
| `CanvasScaler` 를 만드는 화면 코드에 `Screen.safeArea` 적용 누락 | **0개** ✓ |
| 에디터·패키지 버전 고정 위반 (6000.3.26f1 · Input System 1.20.0 · Test Framework 1.6.0 · uGUI 2.0.0, 범위 버전) | **0개** ✓ |
| Active Input Handling ≠ Input System | **0개** ✓ |
| 손으로 편집한 Unity YAML/`.meta`, 커밋된 `Generated/`·`Library/`·`Build/` | **0개** (생성물·캐시 커밋은 ✓) |
| 에셋의 `.meta` 누락 커밋 | **0개** ✓ |
| `Generate` 를 두 번 돌렸을 때 git 변경 발생 (생성기 비결정) | **0개** |
| UTC 로 구한 로컬 날짜 키 (`UtcNow.Date`, `UtcNow.ToString("yyyy-MM-dd")`) | **0개** ✓ |
| 토큰·시크릿·영수증을 `PlayerPrefs`·평문 파일에 저장 | **0개** ✓ (PlayerPrefs 키 이름 기준) |
| Firebase 설정 파일·서명 파일(.p12/.p8/.jks/.keystore/credentials.json) 커밋 | **0개** ✓ |
| 디자인 변경 후 Codex design-review GO 없이 완료 처리 | **0개** |

### Unity 작업 방식 요약

- 진입점: `npm run unity -- generate|render|test|ios|ios-sim|android|apk` (`scripts/unity.mjs` 가 AppInfo 의 네임스페이스와 `ProjectVersion.txt` 의 Unity 경로로 batchmode 를 부른다. 직접 부를 때는 `Unity -batchmode -quit -projectPath unity -executeMethod Game.Editor.Build.Generate -logFile -`)
- `render` 는 GPU 가 필요하다(`-nographics` 금지). `test` 는 `-quit` 없이 `-runTests` 로 돈다
- Domain 은 결정적이다: 고정 스텝(`GameSim.StepsPerSecond`), 시드 `Rng`, 시간은 인자로 받는다. 그래서 Unity 없이 `npm test` 로 검증된다
- 화면은 코드로 만든 uGUI 하나의 씬 상태 머신이면 충분하다(Title → Play → Result). 터치 영역은 기준 132px(≈44pt) 이상
- 공유 게임 에셋은 `~/works/game-assets/aether/LLM_INDEX.md` 를 먼저 읽고 `use_asset.py` 로 가져온다. 출처는 `THIRD_PARTY_ASSETS.md` 에 남는다

상세(생성기 규칙, 레이어 표, 테스트 두 갈래, UI 기준, 고정 버전 올리는 법): `docs/rules/unity-workflow.md`

### 날짜/시간 처리 (CRITICAL)

| 상황 | 올바른 방법 | 금지하는 방법 |
|------|-----------|-------------|
| 오늘 날짜 키 (YYYY-MM-DD) | `DateTime.Now.ToString("yyyy-MM-dd")` (Presentation/Platform 에서 구해 Domain 에 넘긴다) | `DateTime.UtcNow.ToString("yyyy-MM-dd")`, `UtcNow.Date` |
| 저장용 타임스탬프 | `DateTime.UtcNow.ToString("o")` | 로컬 시간 문자열 |
| 게임 시간 | 고정 스텝 수(`Step`) 또는 `Time.unscaledDeltaTime` 누산 | Domain 안에서 `DateTime.Now`·`Time.time` 읽기 |

UTC 날짜는 한국·일본(+9)에서 자정~09시에 "어제"가 된다.

### 에이전트 활용 매핑

| 작업 유형 | 사용할 에이전트 | 참조 파일 |
|----------|--------------|----------|
| 아이디어/리서치 | `idea-researcher` | `.claude/agents/idea-researcher.md` |
| 기획/PRD/KPI | `product-planner` | `.claude/agents/product-planner.md` |
| 스펙/Task 분해 | `spec-planner` | `.claude/agents/spec-planner.md` |
| 비주얼 방향/테마/디자인 리뷰 | `design-architect` | `.claude/agents/design-architect.md` |
| Domain 모듈 (순수 C# 규칙·시뮬레이션·테스트) | `module-builder` | `.claude/agents/module-builder.md` |
| 화면·입력·FX·오디오·씬 생성기 | `presentation-developer` | `.claude/agents/presentation-developer.md` |
| 광고·Analytics·저장·평점·햅틱·네이티브 브리지 | `platform-integrator` | `.claude/agents/platform-integrator.md` |
| 코드 품질 | `qa-reviewer` | `.claude/agents/qa-reviewer.md` |
| 기능/UX 검수 (시뮬레이터) | `app-inspector` | `.claude/agents/app-inspector.md` |
| 출시 후 고도화/다음 작업 추천 | `loop-engineer` | `.claude/agents/loop-engineer.md` |

## Agent Team & Skills

### Skills

| Skill | Command | Description |
|-------|---------|-------------|
| `init-app` | "새 게임 시작" / "앱 이름 바꿔줘" | slug·이름·bundle id·네임스페이스 교체 + 남은 수동 단계 안내 |
| `ideate` | "게임 아이디어 찾아줘" | 시장 조사 및 게임 아이디어 도출 |
| `plan-app` | "게임 기획해줘" | PRD(코어 루프·KPI) 및 모듈 맵 설계 |
| `design-system` | "디자인 시스템 만들어줘" | 팔레트·타이포·간격 토큰(`Theme.cs`)과 화면 레이아웃 |
| `design-review` | "디자인 리뷰해줘" | 시뮬레이터 캡처 → Codex 4축 평가 → 수정 → 재리뷰 |
| `create-module` | "모듈 만들어줘" | Domain 모듈 + NUnit 테스트 스캐폴딩 |
| `create-screen` | "화면 추가해줘" | 코드로 만드는 uGUI 화면/상태 추가 |
| `inspect-app` | "앱 검사해줘" | 시뮬레이터 기능/UX 전체 검사 |
| `orchestrate` | "게임 만들어줘" | 전체 파이프라인 오케스트레이션 (일회성 빌드) |
| `iterate-app` | "다음 뭐 만들지" / "고도화해줘" | 출시 후 개발→검증→다음 추천 지속 루프 |
| `sim-use` | 시뮬레이터 런타임 검증 시 | 시뮬레이터·에뮬레이터 조작 |

> `spec-planner` 는 전용 스킬 없이 에이전트로 직접 호출된다(Phase 2.5).

### Claude · Codex 공용 하네스

| | Claude Code | Codex CLI |
|---|---|---|
| 진입 지침 | `CLAUDE.md` | `AGENTS.md` → `CLAUDE.md` |
| 에이전트 | `.claude/agents/*.md` (원본) | `.codex/agents/*.toml` (생성 사본) |
| 스킬 | `.claude/skills/` (원본), `/스킬명` | `.agents/skills/` (생성 사본), `$스킬명` 또는 `/skills` |
| 검증 hook | `.claude/settings.json` `hooks.Stop` | `.codex/hooks.json` (처음 한 번 `/hooks` 에서 신뢰) |
| 배포 플러그인 | `.claude/settings.json` 이 `seungmanchoi/store-deploy-plugin` 을 등록 | `codex plugin marketplace add seungmanchoi/store-deploy-plugin` → `codex plugin add store-deploy@store-deploy-marketplace` |

- 에이전트·스킬을 고친 뒤에는 반드시 `npm run sync:codex` 를 실행하고 사본을 함께 커밋한다(사본을 직접 수정하지 않는다). `npm run verify` 가 사본이 낡았으면 실패한다.
- 스킬·에이전트 본문의 Claude 도구 이름(`Agent`, `AskUserQuestion`, `SendMessage`, `/loop`, `mcp__*`)을 Codex 에서 어떻게 읽는지는 `.claude/skills/orchestrate/references/runtime-adapters.md` 가 정본이다.

### Full Pipeline

```
Phase 0: Survey        — orchestrate (init-app 먼저)
Phase 1: Ideation      — idea-researcher        (/ideate)
Phase 2: Planning      — product-planner        (/plan-app)
Phase 2.5: Spec        — spec-planner           (docs/specs/)
Phase 3: Design        — design-architect       (/design-system → /design-review)
Phase 4: Implementation (순차, 단계마다 Quick QA)
  4a module-builder         (/create-module)    Domain + 테스트
  4b presentation-developer (/create-screen)    화면·입력·FX·오디오·생성기
  4c platform-integrator                        햅틱·저장·평점·광고 (PRD 가 요구하는 것만)
  4d platform-integrator                        Firebase Analytics KPI 이벤트
Phase 5: QA (병렬)     — qa-reviewer + app-inspector (/inspect-app)
Phase 5.5: Design QA   — Codex design-review (GO 까지)
Phase 6: Iteration     — Fix Loop (최대 3회)
Phase 7: Deployment    — /store-deploy (fastlane build_unity → upload)
───────────────────────────────────────────────────
출시 후: Assess → Recommend → Develop → Verify → Reflect — loop-engineer (/iterate-app)
```

## Analytics & 핵심 지표 (MANDATORY)

PRD 에 북극성 지표 1개와 4축 기본 세트(획득·활성·유지·수익화)를 정의하고, 이벤트는 Firebase Unity(Analytics · Crashlytics)로 보낸다. 호출은 Platform 의 `Analytics` 래퍼로만, 이벤트 이름은 상수 클래스의 snake_case `동사_명사`, PII 금지. 수집은 운영 빌드(`!Application.isEditor && !Debug.isDebugBuild`)에서만 켠다. `GoogleService-Info.plist`·`google-services.json` 은 루트 `firebase/` 에 두고 커밋하지 않는다.

상세: `docs/rules/analytics.md`

## 광고 (MANDATORY)

Google Mobile Ads Unity 를 쓰는 게임은 Platform 의 `Ads.InitializeWithConsent()` 하나로 **UMP → ATT → `SetRequestConfiguration` → `MobileAds.Initialize`** 순서를 지키고, 모든 광고 로드는 `Ads.IsReady` 뒤에 시작한다. ATT 목적 문자열은 무엇(광고 식별자)·왜(관련성, 설치 측정)·구체적 예시·거부 시 결과를 담아 게임마다 새로 쓴다(상투 문구는 Guideline 5.1.1 자동 반려, poly-dash 1.0.10, 2026-08-06). 보상은 획득 콜백에서만, 전면 광고는 공유 쿨다운·일일 상한 게이트, 화면당 배너 1개, 로드 재시도는 백오프.

상세(설치, 콘솔 설정, Info.plist 후처리, Privacy Options, 무효 트래픽): `docs/rules/ads.md`

## 스토어 평점 / 보안 저장

- 평점 요청은 Domain 의 `ReviewPolicy`(설치 3일·실행 5회·핵심 액션 3회·90일 간격·연 3회·실행 후 120초·UI idle·최근 에러 없음)를 거쳐 Platform 브리지로만 한다. 자체 사전 프롬프트·점수 유도 문구·결과 분기 금지. 상세: `docs/rules/store-review.md`
- `PlayerPrefs` 는 평문이다. 설정·플래그·최고 기록만 둔다. 토큰·시크릿·영수증은 Platform 의 Keychain/Keystore 브리지에만. 상세: `docs/rules/secure-storage.md`

## Build & Deploy (MANDATORY)

- 앱 식별자(Slug · Name · BundleId `com.seungmanchoi.{slug}` · Version)는 `unity/Assets/Game/Editor/AppInfo.cs` 한 곳에 둔다. fastlane 이 거기서 읽는다
- iOS: `fastlane ios build_unity`(마지막 TestFlight 빌드 +1 → Unity BuildIOS → 수출 규정 plist → sigh → `Unity-iPhone` 타깃에만 수동 서명 → gym) → `fastlane ios upload` → `tf_status` / `invite`. 시뮬레이터용은 `fastlane ios build_unity_sim`
- 팀 공유 배포 인증서(login.keychain)를 쓴다. 새 인증서·키체인을 만들지 않고 비밀번호를 명령줄에 넘기지 않는다(`~/works/CLAUDE.md`)
- Android: 키스토어는 `ANDROID_KEYSTORE`·`ANDROID_KEY_ALIAS`·`ANDROID_KEYSTORE_PASS`·`ANDROID_KEY_PASS` 환경변수로만. `fastlane android build_unity build:N` → `fastlane android internal`
- 제출 전 IPA/AAB 의 version·build number·bundle id 를 확인하고, Development Build 가 아닌지 본다. 아이콘은 1024 PNG 알파 없음(`Content/AppIcon.png`)
- 스토어 이름과 홈 화면 이름을 언어별로 맞춘다(30자 이내)

상세(lane 단계, 다국어 홈 화면 이름 후처리, 트러블슈팅): `docs/rules/build-deploy.md`, `docs/troubleshooting.md`

## Branch Strategy

이 템플릿으로 만든 게임 레포:

```
main     <- Production
  ^
devel    <- Development (default)
  ^
feature/* <- Feature branches
```

템플릿 레포 자체에는 `devel` 이 없다. `main` 에 PR 로 머지한다.

## Commands

```bash
npm run verify                    # check:harness + Domain dotnet test (Stop hook 도 이것을 돌린다)
npm test                          # dotnet test unity/DomainTests~ (Unity 라이선스 불필요)
npm run unity -- generate         # 씬 생성 + PlayerSettings 적용 (Unity 컴파일 확인 겸용)
npm run unity -- render           # 화면 상태별 PNG → unity/Build/render-check/
npm run unity -- test             # Unity EditMode 테스트 → build-output/editmode-results.xml
npm run unity -- ios-sim          # 시뮬레이터용 Xcode 프로젝트 (fastlane ios build_unity_sim 이 빌드까지)
npm run unity -- apk              # 에뮬레이터용 APK
npm run app:init -- --slug <slug> --name "<Name>" [--namespace <Ns>]   # 템플릿 → 새 게임
npm run sync:codex                # .claude → Codex 사본 재생성
fastlane ios build_unity && fastlane ios upload                         # TestFlight
```

디스크가 부족하면 `unity/Library` 와 `unity/Build` 를 지워도 된다(다음 실행 때 다시 만든다).

## 시뮬레이터 런타임 테스트 (sim-use) (MANDATORY)

화면과 흐름 검증은 `sim-use` 만 쓴다(observe `sim-use ui` → act `tap`·`swipe` → verify). `xcrun simctl` 은 설치·실행·스크린샷·로그(`launch --console-pty`)에만 쓰고 좌표 탭에 쓰지 않는다. 캡처용 자동 플레이: `xcrun simctl spawn booted defaults write <bundle id> autoplay -int 1`.

상세: `docs/rules/runtime-testing.md`

## 코딩 원칙 — ponytail · CodeGraph

- **ponytail**: 존재하지 않아도 되는 코드가 최고의 코드. 필요한가? → 이미 있나? → .NET/Unity 기본 기능 → 설치된 패키지 → 한 줄 → 동작하는 최소 코드 순으로 먼저 걸리는 칸에서 멈춘다. 버그는 근본 원인을 공유 함수 한 곳에서 고친다. 입력 검증·에러 처리·보안·접근성은 줄이지 않는다
- **CodeGraph (선택)**: 구조 질문은 `codegraph_*`, 리터럴 텍스트는 grep

상세: `docs/rules/tooling.md`

## Architecture

```
unity/
├── Assets/Game/
│   ├── Domain/        Game.Domain        순수 C# — 규칙·시뮬레이션·진행 (noEngineReferences)
│   ├── Platform/      Game.Platform      네이티브 브리지·광고·Analytics·저장·평점·햅틱 (SDK 는 여기만)
│   ├── Presentation/  Game.Presentation  MonoBehaviour·uGUI·입력·FX·오디오
│   ├── Editor/        Game.Editor        AppInfo · Build(Generate/BuildIOS/BuildAndroid) · RenderCheck
│   ├── Tests/EditMode Game.Tests.EditMode NUnit — Domain 만 참조
│   ├── Content/       원본 에셋 (폰트·아이콘·이미지·오디오·JSON)
│   └── Generated/     Generate 산출물 (gitignore)
├── Assets/Plugins/iOS/ .mm 네이티브 코드 (AppHaptics.mm)
├── DomainTests~/      dotnet test 프로젝트 (Unity 가 무시하는 ~ 폴더)
├── Packages/manifest.json   고정 버전
└── ProjectSettings/   batchmode 로 만든 설정 (커밋)
fastlane/              build_unity · build_unity_sim · upload · tf_status · invite · android
scripts/               check-harness · unity · init-app · sync-codex-harness · verify-hook
```

**의존 방향**: `Domain ← Platform ← Presentation ← Editor`, `Tests.EditMode → Domain`. 위 레이어만 아래 레이어를 참조한다.

## Code Conventions

- C# 9 (Unity 6 와 `DomainTests~` 가 같은 언어 버전). 네임스페이스는 어셈블리 이름과 같다(`Game.Domain` …)
- 인터페이스 접두사 `I`, 구현이 하나뿐인 인터페이스·팩토리는 만들지 않는다
- 직렬화 필드는 `[SerializeField]` private, 연결은 `Generate()` 의 `SerializedObject` 로 한다
- 로그는 `[Game]` 태그로 시작한다(시뮬레이터 콘솔에서 grep)
- 변경 후 `npm run verify`, C# 을 바꿨으면 `npm run unity -- generate`
