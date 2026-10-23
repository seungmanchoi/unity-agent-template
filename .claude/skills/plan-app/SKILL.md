---
name: plan-app
description: "모바일 게임 기획서(PRD)를 작성하는 스킬. 코어 루프, 유저 스토리, 화면 상태 흐름, KPI, Domain/Presentation/Platform 모듈 맵을 정의한다. '기획해줘', 'PRD 작성', '기능 정의해줘', '화면 흐름 설계해줘', '게임 설계' 요청 시 반드시 이 스킬을 사용할 것."
---

# Plan App — 게임 기획/PRD 스킬

## Trigger
- "기획해줘", "PRD 작성", "기능 정의"
- "화면 흐름 설계", "게임 설계", "구조 잡아줘"

## Input
- `_workspace/idea/app-concepts.md` 또는 사용자 설명

## Steps

### Step 1: 핵심 가치와 코어 루프
- 플레이어가 느낄 핵심 재미 한 문장, 페르소나
- 코어 루프(입력 → 판정 → 피드백 → 보상)와 한 판 목표 길이
- 메타 루프(등급·기록·해금·재화) — MVP 에 필요한 것만

### Step 2: 유저 스토리
- "플레이어로서, ~하기 위해 ~할 수 있다" + P0/P1/P2 + 인수 조건
- 손맛 수치(판정 창, 히트스톱, 난이도 곡선)는 초기값과 조정 범위로 적는다

### Step 3: 화면 상태 흐름
- 단일 씬 상태 머신으로 매핑한다(예: `Title → Play → Result`, 필요 시 `Practice`, `Settings`). 상태마다 주요 UI 요소와 전환 조건
- 설정 화면이 필요한 경우: 광고 개인정보 설정(UMP privacy options), 사운드·햅틱 토글

### Step 4: KPI
- 북극성 1개 + 4축(획득·활성·유지·수익화). 이벤트 카탈로그는 snake_case `동사_명사`, PII 금지(`docs/rules/analytics.md`)

### Step 5: 모듈 맵
| 레이어 | 내용 |
|--------|------|
| `Domain/<Module>/` | 규칙·시뮬레이션·결과 계산(순수 C#, 결정적, 테스트 대상) |
| `Presentation/` | 상태별 화면, 입력, FX, 오디오 |
| `Platform/` | 광고·Analytics·Crashlytics·평점·저장·햅틱 어댑터(SDK 는 여기만) |
| `Editor/` | 생성기(`Generate()`)가 만들 씬 오브젝트·에셋 참조 |

백엔드가 있으면 엔드포인트와 Request/Response 초안을 추가한다(대부분 오프라인 게임은 없음).

### Step 6: MVP 범위
- 1차(P0)·2차(P1) 목록 → `_workspace/plan/prd.md`, 모듈 맵은 `_workspace/plan/module-map.md`, KPI 는 `_workspace/plan/kpis.md`

## Agent Delegation
| Step | Agent |
|------|-------|
| 1-6 | product-planner |
