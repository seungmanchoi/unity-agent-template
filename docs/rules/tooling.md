# 개발 도구 — ponytail · CodeGraph · Codex 협업 · sim-use

> CLAUDE.md 에서 옮긴 상세 규칙이다. **Hard Threshold 표는 CLAUDE.md 가 정본**이며, 여기 없는 통과 기준은 CLAUDE.md 를 따른다.

## ponytail (lazy senior dev)

존재하지 않아도 되는 코드가 최고의 코드다. 요청을 이해한 뒤 아래에서 먼저 걸리는 칸에서 멈춘다.

1. 애초에 필요한가? (YAGNI)
2. 이 레포에 이미 있나? (재구현 금지)
3. .NET 표준 라이브러리로 되나?
4. Unity·플랫폼 기능으로 되나? (uGUI, Input System, ParticleSystem, PlayerPrefs …)
5. 이미 들어 있는 패키지로 되나? (몇 줄짜리 일로 패키지를 늘리지 않는다)
6. 한 줄로 되나?
7. 그제서야 동작하는 최소 코드.

- 요청하지 않은 추상화(구현이 하나뿐인 interface, 매니저의 매니저, 서비스 로케이터)를 만들지 않는다.
- 버그는 증상이 아닌 근본 원인을 공유 함수 한 곳에서 고친다.
- 입력 검증·에러 처리·보안·접근성은 줄이지 않는다.
- 플러그인 `ponytail@ponytail`(마켓플레이스 `DietrichGebert/ponytail`)이 `.claude/settings.json` 에 선언돼 있다. 레벨: `/ponytail lite|full|ultra`, 끄기: `stop ponytail`.

## CodeGraph (선택)

C# 도 인덱싱한다. 구조 질문(무엇이 무엇을 호출·정의·파급하는지)은 `codegraph_*`, 리터럴 텍스트는 grep. 없으면 grep 으로 진행한다.

| 질문 | 도구 |
|------|------|
| 심볼 위치 | `codegraph_search` |
| 영역 컨텍스트 | `codegraph_context` |
| X→Y 흐름 | `codegraph_trace` |
| 호출자/피호출자 | `codegraph_callers` / `codegraph_callees` |
| 바꾸면 깨지는 것 | `codegraph_impact` |

인덱스: `codegraph init -i`, 갱신 `codegraph sync`. `.codegraph/` 는 gitignore.

## Codex 협업

- **디자인은 Codex 가 맡는다.** 화면이 바뀌는 작업은 시뮬레이터 캡처를 Codex 에 보내 4축(Design Quality · Originality · Craft · Functionality) 점수와 P0/P1/P2 지적을 받고, 구현 → 다시 캡처 → 재리뷰를 반복한다(`design-review` 스킬).
- 큰 구현·진단을 Codex 에 넘길 때 결과를 받아 이어서 일해야 하면 `codex:codex-rescue` 프롬프트 첫 줄에 `--wait` 를 쓴다. 빼면 background 로 가고 결과는 사용자만 `/codex:result` 로 받을 수 있다.
- 단계가 끝날 때마다 Codex 리뷰를 받아 반영한다.

## sim-use

설치·사용법은 `runtime-testing.md`. 시뮬레이터 UI 조작은 sim-use 만 쓴다.

## Unity · .NET

| 도구 | 설치 |
|------|------|
| Unity 6000.3.26f1 | Unity Hub → Installs (iOS·Android Build Support 포함), Hub 로그인 후 Personal 라이선스 활성화 |
| .NET SDK 10 | `brew install dotnet` (`npm test` 용) |
| fastlane | `brew install fastlane` |
| CocoaPods | `brew install cocoapods` (GMA·Firebase 를 넣은 뒤 EDM4U 가 쓴다) |
