---
name: create-screen
description: "코드로 만드는 uGUI 에 새 화면(상태)을 추가하는 스킬. SafeArea·기준 해상도 1080×2340·Theme 토큰·한글 폰트·터치 타깃 규칙을 따르고 Generate/RenderCheck 와 Codex 디자인 리뷰로 마무리한다. '스크린 추가해줘', '화면 만들어줘', '페이지 추가', '상태 추가', 'UI 추가' 요청 시 반드시 이 스킬을 사용할 것."
---

# Create Screen — uGUI 화면 추가

> 시작 전 `.claude/agents/presentation-developer.md` 를 Read 하고 따른다. 씬·프리팹·Unity YAML 을 손으로 만들지 않는다. UI 는 코드로 만든다.

## Trigger
- "스크린 추가해줘", "화면 만들어줘", "상태 추가", "설정 화면", "결과 화면"
- `/create-screen {Name}`

## Input
- `Name`: 상태 이름(PascalCase, 예: `Settings`)
- 들어오고 나가는 전환, 보여줄 Domain 상태

## Steps

### Step 1: 위치 정하기
- 작은 화면: `GameController` 의 `State` enum 에 값을 추가하고 `Enter()`·`Render()`·`Update()` 분기에 넣는다
- 요소가 많은 화면: `Presentation/{Name}Screen.cs` 에 빌더 클래스를 두고(`Build(RectTransform safe)`, `Show(bool)`, `Render(...)`) 컨트롤러가 소유한다

### Step 2: 만들기 규칙
- 모든 요소는 `SafeArea` RectTransform 아래에 둔다(배경·비네트만 예외). 새 Canvas 를 만들면 `CanvasScaler`(ScaleWithScreenSize, 1080×2340, `matchWidthOrHeight = 0`)와 `Screen.safeArea` 적용을 함께 넣는다 — 없으면 `npm run verify` 가 실패한다
- 색·글자 크기·간격은 `Theme` 토큰만 쓴다. 폰트는 컨트롤러가 받은 Noto Sans KR
- 터치 타깃 최소 132 기준 px(약 44pt). 탭 판정은 `RectTransformUtility.ScreenPointToLocalPointInRectangle` 로 한다
- 입력은 Input System(`Pointer.current`, `Touchscreen.current`). 옛 `UnityEngine.Input` 금지
- 게임 판정은 Domain 에 두고 화면은 읽기·전달만 한다
- 상태 전환마다 `Debug.Log($"[Game] state -> {state}")`(시뮬레이터 로그로 흐름을 확인한다)

```csharp
void BuildSettings(RectTransform safe)
{
    _settings = Rect("Settings", safe);
    Stretch(_settings);
    var title = Label("SettingsTitle", _settings, Theme.Title, TextAnchor.MiddleCenter);
    Place(title.rectTransform, new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(1080, 120));
    title.text = "설정";
}
```

### Step 3: 씬·에셋 연결 (필요할 때만)
- 새 에셋(스프라이트·오디오·폰트) 참조나 씬 오브젝트가 필요하면 `Editor/Build.cs` 의 `Generate()` 에서 `SerializedObject` 로 연결한다. `Generate()` 는 여러 번 돌려도 같은 결과여야 한다
- `RenderCheck` 는 `State` enum 값을 모두 찍으니 새 상태가 자동으로 포함된다. 외부 데이터가 필요한 상태면 캡처용 기본값을 준다

### Step 4: 검증
- `npm run verify`
- `npm run unity -- generate` → `npm run unity -- render` 로 `unity/Build/render-check/{Name}.png` 확인
- 시뮬레이터에서 `sim-use` 로 진입·이탈 흐름 확인
- **`design-review` 스킬로 Codex 판정**

## Agent Delegation
| Step | Agent |
|------|-------|
| 1-3 | presentation-developer |
| 4 | qa-reviewer → design-review |
