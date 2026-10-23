---
name: iterate-app
description: "출시/1차 구현 완료 후 게임을 지속적으로 고도화하는 루프 스킬. 현황 진단 → 다음 고도화 추천(데이터 기반 랭킹) → 한 슬라이스 개발 → 검증(verify·Generate·시뮬레이터·Codex 디자인 리뷰) → 다음 추천을 반복한다. 전체 파이프라인을 재실행하지 않는다. '다음 뭐 만들지', '고도화해줘', '개선 루프', '기능 추가 우선순위', 'iterate', '다음 스프린트 돌려줘' 요청 시 반드시 이 스킬을 사용할 것."
---

# Iterate App — 지속 고도화 루프

이미 만든 게임을 **한 사이클에 한 가지씩** 개선한다. 처음부터 만들 때는 `/orchestrate`.

> 루프·랭킹 루브릭 상세: `.claude/skills/orchestrate/references/loop-engineering.md`
> 의사결정 주체: `loop-engineer`

## Trigger
- "다음 뭐 만들지", "다음 작업 추천", "고도화해줘", "개선해줘"
- "기능 추가 우선순위", "개선 루프", "다음 스프린트", "iterate"

## When NOT to use
- 처음부터 만들 때 → `/orchestrate`
- 지정된 단일 변경(예: "결과 화면 글자 키워줘") → 해당 에이전트 직접

## Steps (1 사이클)

### Step 1: Assess
`loop-engineer` 가 `_workspace/spec.md`, `docs/specs/README.md`, `_workspace/plan/{prd,kpis}.md`, `_workspace/qa/*`, `_workspace/iterate/backlog.md`, 최근 `design-review-*.md`, TestFlight 피드백, git log 를 읽고 현재 상태를 요약한다. KPI 실측이 있으면 취약 축을 찾는다.
> CodeGraph 를 쓰면 진단 전에 `codegraph sync` 로 인덱스를 갱신한다(없으면 grep).

**관측 점검 (매 사이클 첫 단계)**: Firebase Crashlytics 가 없고 `spec.md` 의 `measurement.crashlytics` 가 `false` 로 답해진 적이 없으면 `AskUserQuestion` 으로 붙일지 묻는다(권장: 붙이기). 붙이면 이번 슬라이스를 Crashlytics 연동으로 하고 `docs/rules/analytics.md` 순서를 따른다. 건너뛰면 `measurement.crashlytics: false` 와 사유를 기록한다.

### Step 2: Recommend
후보(미완료 spec task / PRD 2차 / 미해결 이슈 / KPI 갭 / 디자인 리뷰 P2 / 기술 부채 / 플레이테스트 피드백)를 가중 루브릭(KPI 30 · 가치 25 · 노력↓ 20 · 부채 15 · 커버 10)으로 정렬한다.
- 상위 3개를 근거와 함께 제시하고 사용자가 1개 고른다(`execution.unattended: true` 면 1위 자동 + `_workspace/iterate/decisions.log`)
- `_workspace/iterate/backlog.md` 저장

### Step 3: Develop — 한 슬라이스만
| 변경 유형 | 위임 대상 |
|-----------|----------|
| 게임 규칙·시뮬레이션·결과 계산 | `module-builder` |
| 광고·Analytics·Crashlytics·평점·저장·네이티브 | `platform-integrator` |
| 화면·입력·연출·사운드·생성기 배선 | `presentation-developer` |

완료 시 `docs/specs/` 의 `- [ ]` → `- [x]`.

### Step 4: Verify — 게이트
1. `npm run verify` green (check:harness + Domain `dotnet test`). CLAUDE.md Hard Thresholds 가 정본
2. `npm run unity -- generate` 성공, 라이선스가 있으면 `npm run unity -- test`
3. `qa-reviewer`: 변경 슬라이스 + 파급 범위(CodeGraph `impact`) 코드 검토
4. `app-inspector`: 시뮬레이터에서 변경 흐름 + 회귀를 `sim-use` 로 확인(inspect-app Step 2)
5. 보이는 것이 바뀌었으면 **`design-review`** 게이트(P0 없음 + 평균 7.5 이상 또는 Codex GO)
- FAIL → 구현 에이전트 수정(최대 3회). 그래도 Hard Threshold·verify 실패가 남으면 **커밋하지 않고** 보고한다

### Step 5: Reflect
- `_workspace/iterate/cycle-{N}.md`(목표·변경·검증·KPI 가설), `docs/specs/README.md`, `_workspace/plan/kpis.md` 갱신
- 사용자가 원하면 TestFlight: `fastlane ios build_unity` → `fastlane ios upload` (`/store-deploy`, `docs/rules/build-deploy.md`)
- 백로그 재채점 → 다음 추천 1개를 제시하고 종료

## 무인/반복 실행
`_workspace/spec.md` 에 `execution.unattended: true` + `on_ambiguity: pick_recommended` 를 두고 `/loop /iterate-app`(Claude Code 전용). Codex 에는 `/loop` 이 없으니 "iterate-app 을 3 사이클, verify FAIL 이나 백로그가 비면 멈춤"처럼 지정한다(`orchestrate/references/runtime-adapters.md`).

## Agent Delegation
| Step | Agent |
|------|-------|
| 1 · 2 · 5 | loop-engineer |
| 3 | module-builder / platform-integrator / presentation-developer |
| 4 | qa-reviewer + app-inspector (+ Codex design-review) |

## 산출물
```
_workspace/iterate/
├── backlog.md
├── cycle-{N}.md
└── decisions.log
```
