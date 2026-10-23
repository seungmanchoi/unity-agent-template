# Phase 0: Pre-flight Survey — 상세 절차

> orchestrate SKILL.md 에서 옮긴 상세 절차다. Phase 0 에 들어갈 때 Read 하고 그대로 따른다.

**목적**: 게임마다 달라지는 핵심 결정(장르·코어 루프·세션 길이·수익화·아트 방향·언어·광고/분석)을 먼저 모아 `_workspace/spec.md` 를 만든다. 이후 모든 Phase 는 spec.md 를 먼저 읽는다.

**입력**: 사용자 자연어 요청 · **출력**: `_workspace/spec.md`

#### Step 0.0: CodeGraph (선택, 차단하지 않음)

```
which codegraph
├─ 없음 → "설치할까요?" 물음 (무인: 건너뛰고 grep)
└─ 있음 → .codegraph/ 없으면 "인덱스를 빌드할까요?" (무인: codegraph init -i), 있으면 codegraph sync
```

결정은 `_workspace/decisions.log` 에 남긴다.

#### Step 0.0b: 템플릿 초기화 확인

`unity/Assets/Game/Editor/AppInfo.cs` 에 `Slug = "my-game"` 이 남아 있으면 `init-app` 스킬을 먼저 실행한다(이름·slug 는 요청에서 뽑고, 없을 때만 묻는다). 결과를 `project.name` 에 쓴다.

#### Step 0.1: spec.md 확인

```
_workspace/spec.md 있음 → "기존 spec.md 를 그대로 쓸까요? (Y/n)"
   Y → Step 0.5 검증 → Phase 1
   n → spec.md.{timestamp}.bak 백업 후 Step 0.2
없음 → templates/spec.template.yml 을 _workspace/spec.md 로 복사 후 Step 0.2
```

무인 실행을 원하면 템플릿을 직접 채워 `_workspace/spec.md` 로 두고 `/orchestrate` 만 실행한다.

#### Step 0.2: 자유 입력 1회

```
"이 게임에 대해 더 알려 주고 싶은 게 있나요? (레퍼런스 게임, 손맛, 톤, 제약 — 비워도 됨)"
→ project.context 에 그대로 저장
```

#### Step 0.3: 질문 카탈로그

**묻기 전에 채운다.** 요청·0.2 답·현재 코드로 답이 나오는 항목은 채우고 근거를 `<field>_notes` 에 한 줄 남긴다. 남은 **[필수]** 만 `AskUserQuestion` 한 번에 최대 4문항, 첫 옵션을 추천안으로 둔다. "Other" 에 쓴 글은 `_notes` 에 저장한다. 바이브 코딩 요청("간단한 X 게임 만들어줘")은 질문 1~2회 안에 Phase 1 로 넘어간다.

##### A. 게임 콘셉트

| ID | 질문 | 옵션 | spec 필드 |
|----|------|------|----------|
| A1 [필수] | 장르 | 액션 / 퍼즐 / 하이퍼캐주얼 / 리듬 / 아이들 / 로그라이크 / 기타 | `game.genre` |
| A2 [필수] | 코어 루프 한 줄 | LLM 추천 수락 / 직접 입력 | `game.core_loop` |
| A3 [필수] | 한 판 길이 | 30초 이하 / 1~3분 / 5분+ | `game.session_length` |
| A4 [필수] | 조작 | 탭 / 스와이프 / 드래그 / 길게 누르기 / 복합 | `game.input` |
| A5 [필수] | 화면 방향 | 세로 / 가로 | `game.orientation` |
| A6 [조건부] | 메타 진행 | 없음 / 최고 기록 / 레벨·스테이지 / 업그레이드 | `game.meta` (세션이 짧으면 묻기) |

##### B. 아트·사운드

| ID | 질문 | 옵션 | spec 필드 |
|----|------|------|----------|
| B1 [필수] | 아트 방향 | 미니멀 도형 / 2D 일러스트 파츠 / 픽셀 / 2.5D | `art.direction` |
| B2 [필수] | 에셋 출처 | 공용 에셋 라이브러리(`game-assets/aether`) / 이미지 생성 / 직접 제공 | `art.source` |
| B3 [필수] | 사운드 | 효과음 + 배경음 / 효과음만 / 없음 | `art.audio` |
| B4 [필수] | 햅틱 | Yes / No | `art.haptics` |

##### C. 측정

| ID | 질문 | 옵션 | spec 필드 |
|----|------|------|----------|
| C1 [필수] | Firebase Analytics | Yes / No | `measurement.firebase_analytics` |
| C2 [필수] | Crashlytics | Yes / No | `measurement.crashlytics` |
| C3 [필수] | 북극성 지표 | LLM 추천 수락 / 직접 입력 | `measurement.north_star` |

##### D. 수익화

| ID | 질문 | 옵션 | spec 필드 |
|----|------|------|----------|
| D1 [필수] | 모델 | 없음 / 광고 / IAP / 광고+IAP | `monetization.model` |
| D2 [조건부] | 광고 형식 | banner / interstitial / rewarded / app_open (다중) | `monetization.ad_formats` |
| D3 [조건부] | 전면 광고 빈도 | N판마다 (기본 3) + 첫 판 제외 | `monetization.interstitial_every_n` |
| D4 [조건부] | 리워드 보상 | 이어하기 / 재화 2배 / 아이템 | `monetization.rewarded_use` |
| D5 [조건부] | IAP 상품 | 광고 제거 / 재화 / 스킨 | `monetization.iap_products` |

##### E. UX·정책·배포

| ID | 질문 | 옵션 | spec 필드 |
|----|------|------|----------|
| E1 [필수] | 지원 언어 | en / ko / ja / zh … (다중) | `ux.languages` |
| E2 [필수] | 튜토리얼 | 없음 / 연습 판 / 단계별 안내 | `ux.tutorial` |
| E3 [필수] | 평점 유도 | Yes / No | `ux.store_review` |
| E4 [조건부] | 평점 트리거 | LLM 추천 / 직접 | `ux.store_review_triggers` |
| E5 [필수] | 연령 등급 | 4+ / 9+ / 12+ / 17+ | `policy.age_rating` |
| E6 [필수] | 개인정보처리방침 URL | GitHub Pages 자동 / 직접 | `policy.privacy_url` |
| E7 [필수] | 타깃 스토어 | iOS / Android / 둘 다 | `deployment.platforms` |
| E8 [필수] | 베타 채널 | TestFlight / Internal testing / 둘 다 | `deployment.beta_channel` |

#### Step 0.4: 조건부 분기

앞선 답이 다음 결정을 가를 때만 묻는다.

- `D1` 에 광고 → D2·D3 활성, 광고가 있으면 ATT 문구·UMP 메시지가 Phase 7 게이트 항목이 된다
- `D2` 에 rewarded → D4 활성
- `D1` 에 IAP → D5 활성 (Unity IAP 는 Platform 어셈블리에만)
- `C1 = No` 이면 C3 는 로컬 지표(테스트 플레이 기록)로 대체하고 `_notes` 에 남긴다
- `E3 = Yes` → E4 활성. 트리거는 패배·실패 직후가 아니라 승리·기록 갱신 같은 긍정 순간
- `policy.age_rating` 이 어린이 대상이면 맞춤 광고 금지·Family 정책 질문 추가

#### Step 0.5: spec.md 작성

필드와 기본값의 정본은 `templates/spec.template.yml` 이다. 여기에 사본을 두지 않는다. 모든 객관식 필드는 `<field>_notes` 를 가질 수 있고, 에이전트는 `_notes` 를 객관식 값보다 우선한다.

#### Step 0.6: 최종 확인

```
"spec.md 를 만들었습니다. 이대로 진행할까요? (Y/편집)"
```

#### 무인 실행

`execution.unattended: true` 면 체크포인트에서 멈추지 않고 `on_ambiguity`(`pick_first` / `pick_recommended` / `ask`)로 고른다. 자동 결정은 `_workspace/decisions.log` 에 남긴다.

```
[2026-10-23 14:32:01] phase=1 decision=idea_selection value=2 reason=pick_recommended
```

#### 통과 조건

- `_workspace/spec.md` 가 YAML 로 파싱된다
- [필수] 필드가 모두 채워졌다 (`project.name`, `game.genre`, `game.core_loop`, `deployment.platforms` 포함)

못 채운 필드는 다시 묻는다.
