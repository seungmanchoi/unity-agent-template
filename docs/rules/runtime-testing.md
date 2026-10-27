# 시뮬레이터 런타임 테스트 (sim-use) (MANDATORY)

> CLAUDE.md 에서 옮긴 상세 규칙이다. **Hard Threshold 표는 CLAUDE.md 가 정본**이며, 여기 없는 통과 기준은 CLAUDE.md 를 따른다.

게임을 시뮬레이터에서 실제로 돌려 화면·흐름을 확인할 때 UI 관찰·조작·검증은 [`sim-use`](https://github.com/lycorp-jp/sim-use) 만 쓴다. `simctl`·`adb` 좌표 탭, Playwright 로 화면을 조작하지 않는다. `simctl` 은 기기 부팅·설치·실행·스크린샷·로그에만 쓴다.

## 부트스트랩 (처음 한 번)

```bash
command -v sim-use >/dev/null || { brew tap lycorp-jp/tap && brew install lycorp-jp/tap/sim-use; }
```

- 요구: macOS 14+, 최신 Xcode Command Line Tools.
- 스킬은 레포에 있다(`.claude/skills/sim-use`, Codex 사본 `.agents/skills/sim-use`). CLI 를 올린 뒤 스킬도 맞추려면 `sim-use init --client claude --dest .claude/skills` 후 `npm run sync:codex`.

## 빌드·설치·실행 (iOS)

```bash
fastlane ios build_unity_sim     # Unity IOS_SIMULATOR=1 (ARM64) → xcodebuild, 서명 없음
APP=$(ls -d build-output/sim-derived/Build/Products/Debug-iphonesimulator/*.app)
xcrun simctl boot "iPhone 16" 2>/dev/null; open -a Simulator
xcrun simctl install booted "$APP"
xcrun simctl launch --console-pty booted <bundle id>   # Unity Debug.Log 가 터미널로 나온다 ([Game] state -> …)
```

- 캡처·성능 측정용 자동 플레이: `xcrun simctl spawn booted defaults write <bundle id> autoplay -int 1` 후 재실행. 끝나면 `xcrun simctl spawn booted defaults delete <bundle id> autoplay`. iOS `defaults` 가 곧 PlayerPrefs 다.
- 스크린샷: `xcrun simctl io booted screenshot <file>.png` (디자인 리뷰 캡처의 정본).
- 시뮬레이터 없이 화면만 볼 때는 `npm run unity -- render`(`Build/render-check/*.png`, 펼친 iPhone Duo 비율은 `duo-inner/*.png`). 시뮬레이터 캡처를 대신하지는 않는다.

### 창 크기 변화 — iPhone Duo (2027-04 부터)

iPhone Duo 는 접고 펴면 게임이 켜진 채로 창 크기가 바뀐다(outer 1398×2034 ↔ inner 2007×2853, inner 는 거의 4:3 에 가깝게 넓다). iOS 27.1 SDK 이상으로 빌드하면 inner 화면 전체를 쓰고, 2027-04 부터 Apple 이 Duo 대응과 Duo 스크린샷을 요구한다. portrait 고정은 창 비율을 고정하지 않는다.

- CanvasScaler 는 `ScreenMatchMode.Expand`(verify 가 본다). 넓은 창에서 UI 가 잘리지 않고 좌우 여백이 늘어난다
- safe area 와 화면 크기는 `Update` 에서 바뀔 때마다 다시 적용한다. 크기를 `Start`/`Awake` 에서 한 번만 읽어 캐시하지 않는다
- 화면을 바꾸면 `npm run unity -- render` 의 `duo-inner/*.png` 에서 잘림·빈 공간을 먼저 본다
- 검증: Xcode 27.1+ 의 iPhone Duo 시뮬레이터에서 접힘·펼침 각각 Title·Play·Result 를 보고, 플레이 중에 접기→펴기→접기 해 점수·타이머·입력이 이어지는지 본다. Duo 시뮬레이터가 없으면 이 항목만 `BLOCKED` 로 기록한다

## Android

```bash
npm run unity -- apk                       # unity/Build/android/<slug>.apk
adb install -r unity/Build/android/*.apk   # 설치만 adb. 조작은 sim-use
sim-use android init --device <serial>     # 기기당 한 번 브리지 설치
```

## observe → act → verify

```bash
sim-use ui            # 관찰
sim-use tap @9        # 조작 (별칭)
sim-use ui            # 검증
```

| 목적 | 명령 |
|------|------|
| 화면 관찰 | `sim-use ui` · `sim-use screenshot` |
| 탭·스와이프·입력 | `sim-use tap` · `swipe` · `type` |
| 스크린샷·영상 | `sim-use screenshot` · `record-video` |

- **uGUI 는 접근성 트리에 요소를 거의 내보내지 않는다.** `sim-use ui` 가 빈약하면 `sim-use screenshot` 으로 위치를 확인하고 sim-use 의 좌표 지정(`sim-use tap -x <x> -y <y>`)으로 조작한다. 그래도 조작 도구는 sim-use 다.
- 조작 뒤에는 반드시 관찰로 결과를 확인한 다음 다음 행동을 정한다. 게임 상태는 `--console-pty` 로그(`[Game] state -> Result`)로도 확인한다.

## 언제 쓰나

- **app-inspector** 의 런타임 UX·기능 검수, 디자인 리뷰 캡처(`design-review` 스킬)
- `/iterate-app` Verify 단계, 버그 재현·회귀 확인

## Hard Threshold

| 기준 | 임계값 |
|------|--------|
| sim-use 외 도구로 시뮬레이터 UI 조작 (simctl/adb 좌표 탭, Playwright) | **0개** |
| 시뮬레이터 확인 없이 화면 변경 완료 보고 | **0개** |
| 관찰 없이 연속 조작(fire-and-forget) | **0개** |
| autoplay 플래그를 남긴 채 사람 플레이 검증 | **0개** |
