<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.3.26f1-000000?style=for-the-badge&logo=unity&logoColor=white" />
  <img src="https://img.shields.io/badge/C%23-9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/Input_System-1.20.0-222222?style=for-the-badge&logo=unity&logoColor=white" />
  <img src="https://img.shields.io/badge/uGUI-2.0.0-222222?style=for-the-badge&logo=unity&logoColor=white" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Claude_Code-Harness-purple?style=for-the-badge&logo=anthropic&logoColor=white" />
  <img src="https://img.shields.io/badge/Codex-Design_Review-111111?style=for-the-badge&logo=openai&logoColor=white" />
  <img src="https://img.shields.io/badge/Agents-10_Specialists-blueviolet?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Skills-11_Workflows-green?style=for-the-badge" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/platform-iOS%20%7C%20Android-lightgrey?style=flat-square" />
  <img src="https://img.shields.io/badge/tests-dotnet%20test%20%7C%20Unity%20EditMode-6E9F18?style=flat-square" />
  <img src="https://img.shields.io/badge/deploy-fastlane-00F200?style=flat-square&logo=fastlane&logoColor=black" />
  <img src="https://img.shields.io/badge/license-MIT-green?style=flat-square" />
</p>

[English](./README.md) | **한국어**

# Unity Agent Template

Claude Code 와 Codex 가 아이디어부터 스토어 배포, 출시 후 고도화까지 진행하는 Unity 모바일 게임 템플릿.

> **무엇이 다른가?** 에이전트가 Unity Editor 를 열지 않는다. C# 과 원본 에셋이 정본이고, 씬은 Editor 스크립트가 batchmode 로 만든다. 게임 규칙은 엔진과 분리된 순수 C# 이라 Unity 라이선스 없이 `dotnet test` 로 검증된다. 화면은 시뮬레이터에서 `sim-use` 로 조작하고, 디자인은 실제 캡처를 Codex 가 4축으로 평가한다. 배포는 Unity → Xcode → fastlane 으로 TestFlight 까지 한 번에 간다. [react-native-fsd-agent-template](https://github.com/seungmanchoi/react-native-fsd-agent-template) 의 하네스(에이전트·스킬·verify 게이트·Claude/Codex 공용)를 Unity 에 맞게 옮겼다.

---

## Full Pipeline

```
"게임 만들어줘"
  Phase 0   Survey          init-app → _workspace/spec.md
  Phase 1   Ideation        idea-researcher
  Phase 2   Planning        product-planner     PRD · 코어 루프 · KPI · 모듈 맵
  Phase 2.5 Spec            spec-planner        docs/specs/ phase·task
  Phase 3   Design          design-architect    Theme 토큰 → Codex design-review
  Phase 4   Implementation  module-builder → presentation-developer → platform-integrator (단계마다 Quick QA)
  Phase 5   QA              qa-reviewer + app-inspector (시뮬레이터)
  Phase 5.5 Design QA       Codex 4축 리뷰, GO 까지
  Phase 6   Iteration       Fix Loop 최대 3회
  Phase 7   Deploy          /store-deploy → fastlane ios build_unity → upload
"다음 뭐 만들지"
  Loop      Assess → Recommend → Develop → Verify → Reflect   loop-engineer (/iterate-app)
```

## Agent Team

| Agent | 역할 |
|-------|------|
| `idea-researcher` | 모바일 게임 시장·경쟁작 조사, 아이디어 도출 |
| `product-planner` | PRD, 코어 루프, 재미 게이트, KPI, 모듈 맵 |
| `spec-planner` | 피처별 스펙, phase/task 분해, 진행 추적 |
| `design-architect` | 비주얼 방향, `Theme.cs` 토큰, Codex 디자인 리뷰 운영 |
| `module-builder` | Domain 모듈(순수 C# 규칙·시뮬레이션)과 NUnit 테스트 |
| `presentation-developer` | 코드로 만드는 uGUI 화면, 입력, FX, 오디오, 씬 생성기 |
| `platform-integrator` | 햅틱·저장·평점·광고·Analytics·네이티브 브리지 |
| `qa-reviewer` | verify · Unity 컴파일 · Hard Threshold 판정 |
| `app-inspector` | 시뮬레이터 기능/UX 검수 (`sim-use`, 캡처) |
| `loop-engineer` | 출시 후 다음 작업 추천과 고도화 루프 |

## Skills

| Skill | 하는 일 |
|-------|--------|
| `init-app` | slug · 이름 · bundle id · 네임스페이스 교체 |
| `ideate` / `plan-app` | 아이디어, PRD |
| `design-system` | 팔레트·타이포·간격 토큰과 레이아웃 |
| `design-review` | 시뮬레이터 캡처 → Codex 4축 평가 → 수정 → 재리뷰 |
| `create-module` | Domain 모듈 + 테스트 |
| `create-screen` | uGUI 화면/상태 추가 |
| `inspect-app` | 시뮬레이터 전체 검수 |
| `orchestrate` | 전체 파이프라인 |
| `iterate-app` | 출시 후 지속 고도화 루프 |
| `sim-use` | 시뮬레이터·에뮬레이터 조작 |

배포는 [`store-deploy`](https://github.com/seungmanchoi/store-deploy-plugin) 플러그인(`/store-deploy`)이 맡는다. `.claude/settings.json` 이 등록한다.

## Architecture

```
unity/Assets/Game/
  Domain/        Game.Domain        순수 C# (noEngineReferences) — 결정적 고정 스텝, 시드 Rng, 시계 없음
  Platform/      Game.Platform      SDK·네이티브 브리지는 여기만 (Haptics 샘플)
  Presentation/  Game.Presentation  MonoBehaviour, 코드로 만드는 uGUI, Input System
  Editor/        Game.Editor        AppInfo · Build(Generate/BuildIOS/BuildAndroid) · RenderCheck
  Tests/EditMode Game.Tests.EditMode
  Content/       폰트(Noto Sans KR, OFL)·아이콘·원본 에셋
  Generated/     Generate 산출물 (gitignore)
unity/DomainTests~/   dotnet test 프로젝트 (Domain + EditMode 테스트 소스를 컴파일)
```

의존 방향은 `Domain ← Platform ← Presentation ← Editor`, 테스트는 Domain 만 본다. `npm run verify` 가 asmdef 참조, Domain 순수성, SDK 위치, safe area, 버전 고정, 커밋하면 안 되는 파일을 검사한다.

샘플 게임은 "표적 탭"이다. `GameSim` 이 60Hz 고정 스텝으로 표적을 옮기고, `GameController` 가 Title → Play → Result 상태 머신과 uGUI 를 코드로 만든다. 실제 게임으로 바꿀 때도 이 모양(시드와 규칙을 받는 Domain, 그것을 그리는 Presentation)을 유지한다.

## Getting Started

### 1. 준비물

- Unity Hub + Unity **6000.3.26f1** (iOS · Android Build Support), Personal 라이선스로 로그인
- .NET SDK 10 (`brew install dotnet`) — Domain 테스트
- Node 20+ — 하네스 스크립트
- Xcode, fastlane, [`sim-use`](https://github.com/lycorp-jp/sim-use) (`brew tap lycorp-jp/tap && brew install lycorp-jp/tap/sim-use`)

### 2. 템플릿에서 새 게임 만들기

```bash
gh repo create my-new-game --template seungmanchoi/unity-agent-template --private --clone
cd my-new-game
npm run app:init -- --slug pocket-pulse --name "Pocket Pulse"   # 네임스페이스 기본값: PocketPulse
npm run sync:codex && npm run verify
npm run unity -- generate      # 처음 열 때 Library 를 만든다 (몇 분)
```

### 3. 확인

```bash
npm test                       # Domain 테스트 (라이선스 불필요)
npm run unity -- test          # Unity EditMode 테스트
npm run unity -- render        # unity/Build/render-check/*.png
fastlane ios build_unity_sim   # 시뮬레이터 앱 → xcrun simctl install booted <app>
```

### 4. AI Agent Harness 사용

```
"게임 만들어줘"               → /orchestrate
"표적이 점점 빨라지게 해줘"     → module-builder → presentation-developer → qa-reviewer → design-review
"다음 뭐 만들지"               → /iterate-app
```

## Commands

| 명령 | 설명 |
|------|------|
| `npm run verify` | check:harness + Domain `dotnet test` (Stop hook · CI 와 같다) |
| `npm test` | `dotnet test unity/DomainTests~` |
| `npm run unity -- generate` | 씬 생성 + PlayerSettings 적용 |
| `npm run unity -- render` | 화면 상태별 1080×2340 PNG (GPU 필요) |
| `npm run unity -- test` | Unity EditMode 테스트 |
| `npm run unity -- ios` / `ios-sim` | Xcode 프로젝트 (기기 / 시뮬레이터) |
| `npm run unity -- android` / `apk` | AAB / 에뮬레이터용 APK |
| `npm run app:init -- …` | 템플릿 → 새 게임 |
| `npm run sync:codex` | `.claude` → Codex 사본 |

## Build & Deploy

앱 식별자는 `unity/Assets/Game/Editor/AppInfo.cs` 한 곳에 있다. fastlane 이 거기서 읽는다.

| Lane | 하는 일 |
|------|--------|
| `fastlane ios create_app` | bundle id 등록, ASC 앱 레코드 확인 (레코드는 웹에서 먼저 만든다) |
| `fastlane ios build_unity` | TestFlight 마지막 빌드 +1 → Unity BuildIOS → 수출 규정 plist → sigh → `Unity-iPhone` 수동 서명 → IPA |
| `fastlane ios build_unity_sim` | 시뮬레이터 앱 (서명 없음) |
| `fastlane ios upload` | TestFlight 업로드 |
| `fastlane ios tf_status` / `invite` | 처리 상태, 내부 테스터 초대 |
| `fastlane android build_unity build:N` | 환경변수 키스토어로 AAB |
| `fastlane android internal` | 내부 테스트 트랙 업로드 |

iOS 서명은 login.keychain 의 팀 공유 배포 인증서를 쓴다. 새 인증서를 만들지 않고, 비밀번호를 명령줄에 넘기지 않는다. 광고·Firebase SDK 를 넣으면 EDM4U 가 workspace 를 만들고, `build_unity` 가 그것을 알아서 빌드한다.

## Production Rules

| 영역 | 규칙 문서 |
|------|----------|
| Unity 작업 방식 (생성기, 레이어, 테스트, UI 기준, 고정 버전) | [`docs/rules/unity-workflow.md`](./docs/rules/unity-workflow.md) |
| 광고 — Google Mobile Ads Unity 11.5.0 (UMP → ATT → 초기화) | [`docs/rules/ads.md`](./docs/rules/ads.md) |
| Analytics · Crashlytics — Firebase Unity 13.17.0 | [`docs/rules/analytics.md`](./docs/rules/analytics.md) |
| 인앱 평점 정책 엔진 | [`docs/rules/store-review.md`](./docs/rules/store-review.md) |
| 보안 저장 (PlayerPrefs vs Keychain/Keystore) | [`docs/rules/secure-storage.md`](./docs/rules/secure-storage.md) |
| 빌드·배포 | [`docs/rules/build-deploy.md`](./docs/rules/build-deploy.md) |
| 시뮬레이터 런타임 테스트 | [`docs/rules/runtime-testing.md`](./docs/rules/runtime-testing.md) |
| 도구 (ponytail, CodeGraph, Codex) | [`docs/rules/tooling.md`](./docs/rules/tooling.md) |
| 문제 해결 | [`docs/troubleshooting.md`](./docs/troubleshooting.md) |

SDK 는 템플릿에 들어 있지 않다. 게임이 필요로 할 때 `platform-integrator` 가 규칙 문서대로 `Platform/` 에 붙인다.

## Branch Strategy

게임 레포는 `main`(운영) ← `devel`(기본) ← `feature/*`. 템플릿 레포는 `main` 에 PR 로 머지한다.

## Inspired By

- **[react-native-fsd-agent-template](https://github.com/seungmanchoi/react-native-fsd-agent-template)** — 이 하네스의 원본
- **[revfactory/harness](https://github.com/revfactory/harness)** — 에이전트 팀·파이프라인·`_workspace/` 데이터 흐름
- **[Anthropic Harness Design](https://www.anthropic.com/engineering/harness-design-long-running-apps)** — Context Reset, Sprint 분해, Hard Threshold, 독립 Evaluator

## License

MIT. 폰트 등 서드파티 에셋은 [`THIRD_PARTY_ASSETS.md`](./THIRD_PARTY_ASSETS.md) 를 따른다.
