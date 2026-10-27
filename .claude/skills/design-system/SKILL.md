---
name: design-system
description: "게임 비주얼 시스템을 설계하고 C# 토큰(Presentation/Theme.cs)으로 구현하는 스킬. 컬러 팔레트, 타이포그래피, 간격, 모션·피드백(히트스톱·햅틱·사운드)을 정의하고 Codex 디자인 리뷰로 검증한다. '디자인 해줘', '디자인 시스템 만들어줘', '색상 팔레트', 'UI 설계', '화면 디자인' 요청 시 반드시 이 스킬을 사용할 것."
---

# Design System — 게임 비주얼 시스템 스킬

디자인 결정은 Codex 가 소유한다. 이 스킬은 토큰을 코드로 옮기고 `design-review` 로 판정받는다.

## Trigger
- "디자인 해줘", "디자인 시스템 만들어줘", "색상 팔레트", "테마"
- "UI 설계", "화면 디자인"

## Input
- `_workspace/plan/prd.md`, 레퍼런스 게임(있으면)

## Steps

### Step 1: 아트 방향
- 시각 방향 3안(예: "미니멀 네온" / "따뜻한 수채" / "묵직한 다크 판타지")을 레퍼런스 게임과 함께 제시하고 고른다.
- 아트 소스: 도형·코드 생성 / imagegen / 공용 에셋(`~/works/game-assets/aether/LLM_INDEX.md`). 가져온 에셋과 폰트는 `THIRD_PARTY_ASSETS.md` 에 출처를 남긴다.

### Step 2: 토큰
- 컬러: 배경·표면·잉크(본문)·강조·위험·성공, 상태별 색(피격·완벽 판정 등). 대비 4.5:1 이상(본문)
- 타이포: 기준 해상도 **1080×2340, Expand**(CanvasScaler `screenMatchMode = Expand`, 세로 폰은 가로 맞춤) 단위로 Display/Title/Body/Caption 크기. 폰트는 Noto Sans KR(OFL, `Content/Fonts/`)
- 간격·터치 타깃: 4의 배수 간격, 터치 타깃 최소 132 기준 px(약 44pt), safe area 안쪽 여백
- `unity/Assets/Game/Presentation/Theme.cs` 에 `static readonly Color` / `const int` 로 둔다. 화면 코드에 색·크기 리터럴을 흩뿌리지 않는다.

```csharp
namespace Game.Presentation
{
    public static class Theme
    {
        public static readonly Color Bg = Hex("#12171A"), Ink = Hex("#F0EBDC"), Accent = Hex("#59E3D1");
        public const int Display = 96, Title = 64, Body = 44, Caption = 34;
        public const float TouchMin = 132f;
        static Color Hex(string s) => ColorUtility.TryParseHtmlString(s, out var c) ? c : Color.magenta;
    }
}
```

### Step 3: 모션·피드백
- 판정마다 피드백 묶음: 히트스톱(ms), 화면 흔들림, 파티클, 사운드 큐, 햅틱(`Platform.Haptics` 의 Light/Medium/Heavy/Rigid)
- 페이드·이징 시간, 슬로모 배율. 모든 연출은 Domain 판정과 같은 시간 정의를 쓴다.

### Step 4: 화면 레이아웃
- 상태별(Title/Play/Result…) 레이아웃: SafeArea 안 앵커 위치, 크기, 계층. 핵심 2~3개 화면은 상세히

### Step 5: 반영과 리뷰
- `Theme.cs` 와 화면 코드 반영(`presentation-developer`), `npm run unity -- generate` → `render`
- `_workspace/design/design-system.md` 에 전체 문서 저장
- **`design-review` 스킬로 Codex 판정**(게이트 통과 전에는 완료가 아니다)

## Agent Delegation
| Step | Agent |
|------|-------|
| 1-4 | design-architect |
| 5 | presentation-developer → design-review |
