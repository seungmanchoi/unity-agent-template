---
name: orchestrate
description: "Unity 모바일 게임의 전체 라이프사이클(아이디어→기획→스펙→디자인→구현→QA→디자인 리뷰→배포)을 하네스 에이전트·스킬로 오케스트레이션하는 일회성 빌드 파이프라인. '게임 만들어줘', '앱 만들어줘', '처음부터 끝까지 만들어줘', 'end-to-end 개발', '프로덕션 게임 만들어줘' 요청 시 반드시 이 스킬을 사용할 것. (출시 후 반복 개선은 /iterate-app)"
---

# Orchestrate — Unity Game Lifecycle Pipeline

## Overview

Unity 모바일 게임을 0에서 출시까지 한 번 만드는 파이프라인이다. 에이전트는 Unity Editor GUI 를 쓰지 않는다. C#·JSON·원본 에셋이 정본이고, 씬은 `Editor/Build.cs` 의 `Generate()` 가 batchmode 로 만든다.

> **Codex 에서 실행 중이면** 이 문서의 Claude 도구 이름(`Agent`, `AskUserQuestion`, `SendMessage`, `/skill`, `mcp__*`)을 `references/runtime-adapters.md` 의 Codex 대응으로 읽는다.
>
> **통과 기준은 하나다**: `npm run verify`(check:harness + `dotnet test unity/DomainTests~`)가 green 이고 CLAUDE.md Hard Threshold 위반이 0 이어야 Phase 를 넘긴다. Stop hook 이 같은 명령을 돌리지만, hook 이 없는 환경에서도 직접 실행한다.

**Stack**: Unity 6000.3.26f1 (Personal) · Input System 1.20.0 · Test Framework 1.6.0 · uGUI 2.0.0 · C# 9

**어셈블리**: `Game.Domain`(엔진 비의존, 결정적 고정 스텝, 시드 `Rng`, 시계 없음) ← `Game.Platform`(SDK·`DllImport`·네이티브 브리지) ← `Game.Presentation`(코드로 만드는 uGUI, 입력, FX, 오디오) ← `Game.Editor`(Generate·RenderCheck·BuildIOS·BuildAndroid). `Game.Tests.EditMode` 는 Domain 만 본다. 상세: `docs/rules/unity-workflow.md`.

하네스 설계 원칙(Context Reset, Sprint 분해, Generator-Evaluator 분리, Hard Threshold, Pivot vs Refine, Active Testing): `references/harness-principles.md`.

## Supervisor 메커니즘

이 스킬 자체가 수퍼바이저다.

### 1. 파이프라인 상태 추적

`_workspace/pipeline-status.md` 를 만들고 Phase 전환마다 갱신한다.

```markdown
# Pipeline Status
## Current Phase: {n}   ## Last Updated: {timestamp}

| Phase | Status | Agent | Completed | Notes |
|-------|--------|-------|-----------|-------|
| 0. Survey | COMPLETED | orchestrate | ... | spec.md |
| 1. Ideation | COMPLETED | idea-researcher | ... | |
| 2. Planning | IN_PROGRESS | product-planner | | KPI 포함 |
| 2.5 Spec | PENDING | spec-planner | | |
| 3. Design | PENDING | design-architect + Codex | | |
| 4a. Domain | PENDING | module-builder | | + Quick QA |
| 4b. Presentation | PENDING | presentation-developer | | + Quick QA |
| 4c. Platform | PENDING | platform-integrator | | + Quick QA |
| 4d. Analytics | PENDING | platform-integrator | | + Quick QA |
| 5. QA | PENDING | qa-reviewer + app-inspector | | 병렬 |
| 5.5 Design Review | PENDING | Codex | | GO 까지 |
| 6. Iteration | PENDING | | | 최대 3회 |
| 7. Deploy | PENDING | /store-deploy | | |
```

### 2. Go/No-Go

- **Go**: 그 Phase 산출물이 `_workspace/` 에 있고, Quick QA(아래) PASS, Error Escalation 없음
- **No-Go**: 산출물 누락 → 재실행 · Quick QA FAIL → 수정 후 재검증(최대 2회) · 사용자 블로킹 → `error.md` 후 대기

**Quick QA** (Phase 4 서브스텝마다, `qa-reviewer`): `npm run verify` + `npm run unity -- generate` 성공(컴파일 오류 0) + 두 번째 `generate` 뒤 `git status` 깨끗(생성기 멱등).

### 3. 컨텍스트 리셋 시 재개

`_workspace/pipeline-status.md` 를 읽어 마지막 COMPLETED 다음 Phase 부터 재개한다.

## Workspace

에이전트 간 데이터는 `_workspace/` 파일로 주고받는다. 대부분 gitignore 된 임시 산출물이고 `spec.md`·`plan/`·`iterate/` 만 커밋한다.

```
_workspace/
├── spec.md                 # Phase 0 산출물 (모든 Phase 가 먼저 읽는다)
├── decisions.log           # 무인 모드 자동 결정
├── idea/                   # idea-researcher: market-research.md, game-concepts.md
├── plan/                   # product-planner: prd.md(코어 루프·KPI·Review Triggers), module-map.md, kpis.md
├── design/                 # design-architect: design-system.md, screen-layouts.md, captures/
├── implementation/         # sprint-contract-{4a,4b,4c,4d}.md, 서브스텝 메모
└── qa/                     # code-review.md, inspection-report.md, runtime/, unresolved.md
```

설계 결정과 디자인 리뷰 기록은 `docs/specs/<feature>/` 에 남긴다(spec-planner 가 만든다, 리뷰는 `design-review-N.md`).

## Full Pipeline

### Phase 0: Pre-flight Survey — 수퍼바이저 직접

**목적**: 장르·코어 루프·세션 길이·수익화·아트 방향·언어·광고/분석 여부를 모아 `_workspace/spec.md` 를 만든다. **절차·질문 카탈로그·분기·무인 실행: `references/survey.md` — 시작 시 Read.**

| Step | 내용 |
|------|------|
| 0.0 | CodeGraph 확인(선택, 없으면 grep — 차단하지 않음) |
| 0.0b | `unity/Assets/Game/Editor/AppInfo.cs` 의 `Slug` 가 아직 `"my-game"` 이면 `init-app` 스킬 먼저 |
| 0.1 | 기존 `_workspace/spec.md` 재사용 여부 확인, 없으면 `templates/spec.template.yml` 복사 |
| 0.2 | 자유 입력 1회 → `project.context` |
| 0.3 | 묻기 전에 채운다. 남은 [필수] 항목만 한 번에 최대 4문항, 추천안을 첫 옵션으로 |
| 0.4 | 조건부 분기(광고 → 형식·빈도, 분석 → 북극성 등) |
| 0.5 | spec.md 작성·YAML 검증 |
| 0.6 | 사용자 최종 확인(무인이면 생략, `decisions.log`) |

**통과 조건**: spec.md 가 YAML 로 파싱되고 [필수] 필드가 모두 채워짐.

### Phase 1: Ideation — `idea-researcher` (`/ideate`)

입력: spec.md. 출력: `_workspace/idea/market-research.md`, `game-concepts.md`(후보 3개: 한 줄 훅, 코어 루프, 세션 길이, 레퍼런스 게임, 차별점, 위험). 선택은 `execution.idea_selection`. 사용자가 이미 콘셉트를 줬으면 검증·보강만 한다.

### Phase 2: Planning — `product-planner` (`/plan-app`)

출력: `_workspace/plan/prd.md`(코어 루프, 화면 상태 흐름, 진행·보상, 실패 조건, 난이도 곡선, Review Triggers), `kpis.md`(북극성 1개 + 획득·활성·유지·수익화 4축 + 이벤트 카탈로그 snake_case `동사_명사`), `module-map.md`.

```markdown
# Module Map
## Domain (순수 C#, 테스트 대상)
- Combat/ — BattleSim, Rules (seed + rules in, snapshot out)
## Presentation (MonoBehaviour·uGUI)
- GameController (Title → Play → Result 상태 머신), Hud, Fx, Audio
## Platform (SDK·네이티브)
- Haptics, Storage(PlayerPrefs 비민감 값만), Ads, Analytics, StoreReview
## Content
- 원본 에셋 · JSON 데이터 · THIRD_PARTY_ASSETS.md
```

### Phase 2.5: Spec — `spec-planner`

PRD 를 `docs/specs/<NN-feature>/` 로 나누고 phase/task 체크박스로 분해한다. 각 task 는 담당 에이전트와 완료 기준(테스트 이름·캡처·명령)을 가진다. 출력 경로를 `pipeline-status.md` 에 적는다.

### Phase 3: Design — `design-architect` (`/design-system`) + Codex

디자인 결정은 Codex 가 맡는다(`/design-review`). design-architect 는 방향 시안(팔레트·타이포·모션·타격감 원칙·화면 레이아웃)을 `_workspace/design/` 에 쓰고, 코드 토큰(`Presentation` 의 색·크기 상수)으로 옮긴다. 레퍼런스 해상도 1080×2340, Expand, 모든 HUD 는 SafeArea 안.

- 첫 화면이 그려지는 즉시 `npm run unity -- render` 로 1080×2340 PNG 를 만들어 Codex 에 방향 확인을 받는다(시뮬레이터 캡처가 정본, RenderCheck 는 빠른 초안용).
- 선택은 `execution.design_selection`.

### Phase 4: Implementation — 순차 + Quick QA

각 서브스텝 시작 전 `_workspace/implementation/sprint-contract-4{x}.md`(Done 조건, 검증 명령, 예상 산출물)를 쓰고, 끝나면 spec task 를 체크한다.

#### 4a. Domain — `module-builder` (`/create-module`)

- 게임 규칙·시뮬레이션·진행·보상을 `Game.Domain` 에 순수 C# 로. `UnityEngine` 금지, `DateTime.Now`·`System.Random` 금지(시드 `Rng`, 시간은 인자로), 60Hz 고정 스텝 `Tick()`
- 모듈마다 `Tests/EditMode/*Tests.cs`: 같은 시드 같은 결과, 경계값, 종료 조건
- 통과: `npm test` green

#### 4b. Presentation — `presentation-developer` (`/create-screen`)

- 화면은 단일 씬 상태 머신(Title → Play → Result …) 안의 상태로 추가한다. uGUI 는 코드로 만든다: `CanvasScaler` 1080×2340 `screenMatchMode = Expand`, `Screen.safeArea` 를 따르는 SafeArea RectTransform, Noto Sans KR
- 입력은 Input System(`Pointer.current` 등), Domain 은 고정 스텝 누산기로 돌린다
- 씬·에셋 참조 배선은 `Editor/Build.cs` `Generate()` 에서 `SerializedObject` 로. 씬 YAML·`.meta` 를 손으로 고치지 않는다
- 캡처·성능용 autoplay 경로(`PlayerPrefs autoplay`)를 새 상태에도 이어 준다
- 통과: Quick QA + `npm run unity -- render` 산출물 확인

#### 4c. Platform — `platform-integrator`

spec 이 켠 것만 붙인다: 햅틱(템플릿 `Platform/Haptics.cs`), 로컬 저장(PlayerPrefs 비민감 값 / 비밀은 Keychain·Keystore 브리지), 평점(Domain 정책 + 네이티브 브리지), 광고(GMA Unity 11.5.0, `Ads.InitializeWithConsent()`: UMP → ATT → RequestConfiguration → Initialize), IAP. SDK `using`·`DllImport` 은 `Game.Platform` 밖에 두지 않는다. 규칙: `docs/rules/{ads,secure-storage,store-review}.md`.

#### 4d. Analytics — `platform-integrator`

Firebase Unity 13.17.0 Analytics + Crashlytics 를 Platform 래퍼로 붙이고 `kpis.md` 이벤트를 배선한다. 절차: `references/analytics-integration.md`. 설정 파일은 커밋하지 않는다.

### Phase 5: QA — 병렬

- **5a `qa-reviewer`**: `npm run verify`, `npm run unity -- generate` 2회 후 git 깨끗, `npm run unity -- test`(EditMode), 레이어·결정성·SDK 경계·safe area·시크릿 정적 검토 → `_workspace/qa/code-review.md`
- **5b `app-inspector`** (`/inspect-app`): iOS 시뮬레이터에서 실제 플레이. `fastlane ios build_unity_sim` → `xcrun simctl install booted <app>` → `xcrun simctl launch --console-pty booted <bundle id>` → `sim-use ui` / `sim-use tap` 로 핵심 흐름 → `xcrun simctl io booted screenshot`. 증거는 `_workspace/qa/runtime/`(기기·커밋·캡처). 시뮬레이터를 못 띄웠으면 `BLOCKED` — PASS 아님 → `inspection-report.md`

### Phase 5.5: Design Review — Codex (`/design-review`)

실제 시뮬레이터 캡처(autoplay 로 각 상태를 찍는다: `xcrun simctl spawn booted defaults write <bundle id> autoplay -int 1`)를 Codex 에 보내 4축(Design Quality 30 / Originality 25 / Craft 25 / Functionality 20) 점수와 P0/P1/P2 를 받는다. Claude Code 는 `codex:codex-rescue` 첫 줄에 `--wait`, Codex 런타임은 직접 수행. 결과를 `docs/specs/<feature>/design-review-N.md` 에 저장 → P0·P1 구현 → 재캡처 → 재리뷰. **Codex 가 GO 를 줄 때까지 Phase 7 로 가지 않는다.**

### Phase 6: Iteration — Pivot vs Refine (최대 3회)

```
1. QA·디자인 리뷰 점수 추이 확인
2. 상승 → Refine(이슈만 수정) / 정체·하락 → Pivot / 같은 이슈 2회 연속 → Pivot 강제
3. 수정 → qa-reviewer + app-inspector (+ 시각 변경이면 design-review) 재검사
4. 3회 후 남은 MEDIUM/LOW 는 _workspace/qa/unresolved.md 로 넘기고 Phase 7
```

Hard Threshold 위반, `npm run verify` 실패, HIGH 이슈, 디자인 P0 가 남아 있으면 Phase 7 에 가지 않는다. 반복 횟수는 중단 기준이지 통과 기준이 아니다. 사용자 승인으로도 Hard Threshold 위반은 배포하지 않는다. 출시 후 지속 고도화는 `/iterate-app`(`references/loop-engineering.md`).

### Phase 7: Deployment — `/store-deploy` + Unity 빌드 어댑터

#### Step 7.0: Pre-Deploy Gate

**`references/pre-deploy-gate.md` 를 Read 하고 모든 항목을 통과해야 빌드·제출한다.**

#### Step 7.1: 빌드·업로드

```
iOS:     fastlane ios create_app        # 첫 배포만 (ASC 앱 레코드는 웹에서 먼저)
         fastlane ios build_unity       # TestFlight 최신 빌드 +1 → Unity BuildIOS → 서명 → IPA
         fastlane ios upload            # TestFlight (심사 제출 안 함)
         fastlane ios tf_status / invite email:...
Android: ANDROID_KEYSTORE=... fastlane android build_unity build:N
         fastlane android internal      # 첫 AAB 는 Play Console 웹에서 수동
```

팀 공유 배포 인증서(login.keychain)만 쓴다. 새 인증서·키체인을 만들지 않고, 비밀번호를 명령줄에 넘기지 않는다. 메타데이터·스크린샷·스토어 폼·AdMob 은 store-deploy 플러그인 스킬(`store-metadata`, `store-screenshots`, `store-forms`, `store-admob`)로 한다. 빌드 문제: `references/deploy-build-troubleshooting.md`.

#### Step 7.2: Android 프로덕션 액세스 신청 (개인 계정)

비공개 테스트 14일+ / 12명+ 뒤 "프로덕션 액세스 신청". 답변 정본: `references/play-production-access-application.md`. 사실만 쓰고 최종 제출 전 사용자 확인.

## Conventions (전 에이전트 공통)

- C# 관례: PascalCase 타입·메서드·enum, 인터페이스는 `I` 접두사, `_camelCase` private 필드. 구현이 하나뿐인 인터페이스는 만들지 않는다
- 한 asmdef = 한 레이어. 참조 방향은 Domain ← Platform ← Presentation ← Editor
- 날짜 키는 `DateTime.Now.ToString("yyyy-MM-dd")`(로컬), 저장 타임스탬프는 UTC ISO. Domain 은 시간을 인자로 받는다
- 로그 태그 `[Game]` — `launch --console-pty` 에서 grep 한다
- 외부 에셋은 `THIRD_PARTY_ASSETS.md` 에 출처·라이선스 기록

## Error Escalation

블로킹 이슈는 `_workspace/{phase}/error.md` 에 쓰고 다음 Phase 로 가지 않는다. 사용자 입력 뒤 그 Phase 부터 재개한다. Unity 라이선스·Xcode 약관·디스크 부족은 사용자 작업이다.

## Active Runtime Testing

정적 분석만으로 PASS 하지 않는다. 시뮬레이터 조작은 **sim-use 만** 쓴다(`simctl` 좌표 탭·Playwright 금지). `simctl` 은 설치·실행·로그·스크린샷·defaults 에만 쓴다. 상세: `docs/rules/runtime-testing.md`.

## Inspired By

- [Anthropic Harness Design](https://www.anthropic.com/engineering/harness-design-long-running-apps) — Generator-Evaluator, Sprint Contract, Hard Threshold, Pivot vs Refine, Active Testing
- [revfactory/harness](https://github.com/revfactory/harness) — Pipeline, Fan-out, Producer-Reviewer, Supervisor 패턴
