---
name: inspect-app
description: "구현된 게임의 기능, 플레이 흐름, 디자인, 접근성을 시뮬레이터에서 종합 검수하는 스킬. PRD 대비 완성도, 상태 전환, safe area, 터치 타깃, 프레임, 엣지 케이스를 sim-use 와 캡처로 점검한다. '앱 검수', '게임 검수', '기능 점검', 'UX 검토', '디자인 QA', '출시 전 점검' 요청 시 반드시 이 스킬을 사용할 것."
---

# Inspect App — 게임 종합 검수

## Trigger
- "앱 검수", "게임 검수", "기능 점검", "QA 해줘"
- "UX 검토", "디자인 QA", "출시 전 점검"

## Input
- `_workspace/plan/prd.md`, `_workspace/design/design-system.md`
- 코드: `unity/Assets/Game/`

## Steps

### Step 1: 기능 완성도
- PRD 유저 스토리를 체크리스트로 바꾸고 각 항목을 코드(Domain 규칙·Presentation 상태)에 매핑한다: PASS / PARTIAL / FAIL
- 규칙이 Domain 테스트로 덮였는지 본다(없는 규칙은 FAIL 사유)

### Step 2: 런타임 검증 (sim-use)
- `command -v sim-use >/dev/null || brew install lycorp-jp/tap/sim-use` (스킬은 레포에 있으니 `sim-use init` 은 하지 않는다)
- 빌드·설치: `fastlane ios build_unity_sim` → `xcrun simctl install booted <app>` → `xcrun simctl launch --console-pty booted <bundle id>` 로 `[Game] state -> …` 로그와 예외를 본다
- 조작은 `sim-use ui` → `tap @N`/`swipe` → `sim-use ui` 만. simctl·adb 좌표 탭 금지
- 필수 흐름: 첫 실행 → 한 판 → 결과 → 재도전, 일시정지·백그라운드 복귀, 설정(있으면), 광고 실패 시에도 진행되는지
- 플레이 화면은 autoplay 로 끝까지 돌린다: `xcrun simctl spawn booted defaults write <bundle id> autoplay -int 1`
- 기록: 기기·커밋(`git rev-parse --short HEAD`)·행동/기대/결과·캡처(`xcrun simctl io booted screenshot _workspace/qa/runtime/…png`)
- 빌드·실행이 안 되면 `BLOCKED`(사유)로 적고 정적 검수만 한다. `BLOCKED` 는 PASS 가 아니며 배포 게이트에서 다시 걸린다

### Step 3: 레이아웃·디자인
- 노치/Dynamic Island 기기와 작은 화면(iPhone SE)에서 safe area 침범, 잘린 글자, 겹침
- `Theme` 토큰 밖 색·크기 리터럴, 폰트 누락(□ 표시)
- 보이는 변경이 있었으면 `design-review` 결과가 최신인지 확인

### Step 4: 접근성·손맛
- 터치 타깃 132 기준 px 이상, 본문 대비 4.5:1 이상
- 프레임: `Application.targetFrameRate = 60`, 플레이 중 끊김(Xcode Instruments 나 로그 기반 측정이 있으면 p95)
- 입력 → 피드백(시각·사운드·햅틱) 지연이 눈에 띄지 않는지

### Step 5: 엣지 케이스
- 연타·멀티터치, 판 종료 순간 입력, 앱 전환 직후 시간 누적(고정 스텝 누산기 상한), 첫 실행(PlayerPrefs 비어 있음), 긴 번역 문자열

### Step 6: 보고서
- 점수(0~100), HIGH / MEDIUM / LOW(HIGH = Hard Threshold 위반·진행 불가·크래시)
- 각 이슈: 파일:라인, 설명, 수정 방법 → `_workspace/qa/inspection-report.md`

## Agent Delegation
| Step | Agent |
|------|-------|
| 1-6 | app-inspector |

## qa-reviewer 와의 분담
- **qa-reviewer**: `npm run verify`, Domain/EditMode 테스트, asmdef 레이어, 코드 품질·보안
- **app-inspector**: 실제 플레이 흐름, 레이아웃, 접근성, 손맛
