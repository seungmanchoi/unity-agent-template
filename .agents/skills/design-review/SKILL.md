---
name: design-review
description: "시뮬레이터 실제 캡처로 Codex 디자인 리뷰를 받는 스킬. 상태별 화면을 캡처해 Codex 에 4축(Design Quality·Originality·Craft·Functionality) 점수와 P0/P1/P2 지적, GO/NO-GO 판정을 받고, 고친 뒤 다시 캡처해 재리뷰한다. 화면·연출·폰트·색 등 보이는 것이 바뀐 모든 작업 뒤, '디자인 리뷰', '디자인 검토', 'Codex 리뷰', '화면 평가' 요청 시 반드시 이 스킬을 사용할 것."
---

# Design Review — Codex 디자인 리뷰 루프

디자인은 Codex 가 소유한다. Claude 는 캡처·구현·재캡처를 맡고, 판정은 Codex 가 한다. **보이는 것이 바뀐 작업은 이 게이트를 통과해야 끝난다.**

## Trigger
- 화면·연출·폰트·색·레이아웃 변경 직후
- "디자인 리뷰", "디자인 검토", "Codex 리뷰", "화면 평가"

## Steps

### Step 1: 캡처 (실제 시뮬레이터가 정본)
```bash
fastlane ios build_unity_sim     # Unity → 시뮬레이터 Xcode 프로젝트 → .app (서명 없음)
APP=$(ls -d build-output/sim-derived/Build/Products/Debug-iphonesimulator/*.app | head -1)
xcrun simctl install booted "$APP"
xcrun simctl launch booted <bundle id>
```
- 화면 조작은 `sim-use` 만 쓴다(`sim-use ui` → `tap @N` → `sim-use ui`). 좌표 탭 금지
- 상태마다 `xcrun simctl io booted screenshot _workspace/design/captures/r{N}-{state}.png`
- 플레이 중 화면은 autoplay 로 찍는다: `xcrun simctl spawn booted defaults write <bundle id> autoplay -int 1` 후 재실행(끝나면 `defaults delete <bundle id> autoplay`)
- 노치·Dynamic Island 기기 1대 + 작은 화면 1대(예: iPhone SE)
- 보조 자료: `npm run unity -- render` 의 `unity/Build/render-check/*.png`(시뮬레이터를 대신하지 않는다)

### Step 2: Codex 리뷰 요청
Claude Code 에서는 `codex:codex-rescue` 에이전트에 넘기고 **프롬프트 첫 줄에 `--wait`** 를 쓴다(빠뜨리면 background 로 가서 결과를 못 받는다). Codex CLI 만 있으면 `codex exec` 로 같은 프롬프트를 보낸다.

프롬프트에 담을 것:
- 캡처 경로 목록, `_workspace/design/design-system.md`, 관련 spec(`docs/specs/<feature>/`), 직전 리뷰 문서(있으면)
- 요청: 4축 각 1~10점(Design Quality, Originality, Craft, Functionality)과 평균, P0(출시 차단)/P1(이번에 고칠 것)/P2(나중) 지적과 각각의 구체적 수정안(위치·수치), GO/NO-GO

### Step 3: 기록
`docs/specs/<feature>/design-review-{N}.md` 에 저장한다: 날짜, 커밋(`git rev-parse --short HEAD`), 캡처 목록, 점수 표, P0/P1/P2, 판정, 이번에 반영할 항목.

### Step 4: 반영 → 재캡처 → 재리뷰
- P0·P1 을 `presentation-developer` 에 맡겨 고친다(토큰은 `Theme.cs`, 연출 수치는 해당 코드). P2 는 `_workspace/iterate/backlog.md` 로
- `npm run verify` 후 Step 1 부터 다시. 재리뷰 프롬프트에는 직전 리뷰 문서와 "무엇을 어떻게 고쳤는지"를 붙인다

### 게이트
- **통과**: P0 없음 + 평균 7.5 이상, 또는 Codex 가 GO
- 3라운드 안에 통과하지 못하면 멈추고 점수 추이·남은 P0 를 사용자에게 보고한다

## Agent Delegation
| Step | Agent |
|------|-------|
| 1, 3 | app-inspector (캡처·기록) |
| 2 | Codex (`codex:codex-rescue --wait`) |
| 4 | presentation-developer |
