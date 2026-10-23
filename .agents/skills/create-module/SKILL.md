---
name: create-module
description: "Unity Domain 레이어에 새 게임 규칙 모듈(규칙·시뮬레이션·결과 타입 + NUnit 테스트)을 스캐폴딩하는 스킬. 순수 C#·결정적·시드 난수·asmdef 레이어 규칙을 강제한다. '모듈 만들어줘', '피처 만들어줘', '엔티티 만들어줘', '게임 규칙 추가', '새 시스템 만들어줘', '도메인 모델 추가' 요청 시 반드시 이 스킬을 사용할 것."
---

# Create Module — Domain 모듈 스캐폴딩

> 시작 전 `.claude/agents/module-builder.md` 를 Read 하고 따른다. 레이어 규칙: `references/module-rules.md`.

## Trigger
- "모듈 만들어줘", "피처/엔티티 만들어줘", "게임 규칙 추가", "새 시스템"
- `/create-module {Name}`

## Input
- `Name`: PascalCase 모듈 이름(예: `Combo`, `Economy`, `Wave`)
- 규칙 요약: 입력, 상태, 판정, 결과

## Steps

### Step 1: 확인
- `unity/Assets/Game/Domain/` 에 같은 이름이 없는지, 기존 모듈(`GameSim`, `Rng`)로 해결되지 않는지 먼저 본다

### Step 2: 파일 생성
```
unity/Assets/Game/Domain/{Name}/
├── {Name}Rules.cs     # 튜닝 값(sealed class, public 필드, 기본값)
├── {Name}Sim.cs       # 상태 + Tick()/입력 메서드 (또는 순수 계산이면 static 함수)
└── {Name}Types.cs     # enum·readonly struct 결과 (필요할 때만)
unity/Assets/Game/Tests/EditMode/{Name}Tests.cs
```
- namespace `Game.Domain`(하위 폴더가 많아지면 `Game.Domain.{Name}`)
- `using UnityEngine` 금지. 시간은 고정 스텝 수(`GameSim.StepsPerSecond`)나 인자로 받는다. `DateTime.Now`·`System.Random`·`UnityEngine.Random` 금지 → `Rng` 를 시드로 만든다
- 결과는 값(record 대신 C# 9 호환 class/struct). Presentation 이 읽기만 하게 상태는 `{ get; private set; }`

```csharp
namespace Game.Domain
{
    public sealed class ComboRules { public int WindowSteps = 30; public int MaxMultiplier = 5; }

    public sealed class ComboSim
    {
        public readonly ComboRules Rules;
        int _lastHitStep = int.MinValue / 2;
        public int Multiplier { get; private set; } = 1;
        public ComboSim(ComboRules rules = null) => Rules = rules ?? new ComboRules();
        public void Hit(int step)
        {
            Multiplier = step - _lastHitStep <= Rules.WindowSteps ? System.Math.Min(Multiplier + 1, Rules.MaxMultiplier) : 1;
            _lastHitStep = step;
        }
    }
}
```

### Step 3: 테스트
- `Tests/EditMode/{Name}Tests.cs`(NUnit, namespace `Game.Tests`): 경계값, 같은 시드 → 같은 결과, 종료 후 입력 무시 등 규칙마다 하나
- `npm test`(= `dotnet test unity/DomainTests~`) green. Unity 라이선스가 필요 없다

### Step 4: 검증
- `npm run verify`(asmdef 레이어·Domain 순수성 포함)
- 새 `.cs` 의 `.meta` 는 Unity 가 만든다: `npm run unity -- generate` 를 한 번 돌리고 생긴 `.meta` 를 함께 커밋한다(손으로 만들지 않는다)

### 다음
- 화면·입력·연출이 필요하면 `presentation-developer`(`/create-screen`)
- 광고·Analytics·저장·네이티브가 필요하면 `platform-integrator`

## Agent Delegation
| Step | Agent |
|------|-------|
| 1-3 | module-builder |
| 4 | qa-reviewer |
