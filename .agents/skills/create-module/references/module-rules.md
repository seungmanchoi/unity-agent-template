# Module Rules — asmdef 레이어

## 어셈블리 방향

```
Game.Domain  ←  Game.Platform  ←  Game.Presentation  ←  Game.Editor
     ↑
Game.Tests.EditMode
```

| 어셈블리 | 폴더 | 내용 | 참조 가능 |
|----------|------|------|-----------|
| `Game.Domain` | `Domain/` | 규칙·시뮬레이션·결과. `noEngineReferences: true` | 없음 |
| `Game.Platform` | `Platform/` | SDK(GMA·Firebase·평점)·네이티브 브리지(`DllImport`, `AndroidJavaObject`)·저장 | Domain |
| `Game.Presentation` | `Presentation/` | MonoBehaviour, uGUI(코드로 생성), 입력(Input System), FX, 오디오 | Domain, Platform, `Unity.InputSystem`, `UnityEngine.UI` |
| `Game.Editor` | `Editor/` | `Build.Generate`·`RenderCheck`·빌드 진입점, `AppInfo` | Domain, Platform, Presentation |
| `Game.Tests.EditMode` | `Tests/EditMode/` | NUnit. `dotnet test unity/DomainTests~` 와 Unity `-runTests` 가 같은 파일을 돈다 | Domain |

`scripts/check-harness.mjs` 가 위 방향, Domain 의 `UnityEngine`·시계·비시드 난수 사용, Platform 밖 SDK·`DllImport` 를 잡는다.

## Domain 규칙
- 결정적: 같은 시드·같은 입력 열 → 같은 상태. 고정 스텝(60Hz) 정수 카운트로 시간을 센다
- 시계·엔진·전역 난수·파일·네트워크 없음. 필요한 값은 인자로 받는다
- C# 9(Unity 6) 문법만: `record`·`init` 은 쓰지 않는다(Unity 에 `IsExternalInit` 이 없다)

## 이름
| 대상 | 규칙 | 예 |
|------|------|----|
| 폴더·클래스 | PascalCase | `Domain/Combo/ComboSim.cs` |
| 튜닝 값 | `{Name}Rules` | `ComboRules` |
| 시뮬레이션 | `{Name}Sim` | `ComboSim` |
| 테스트 | `{Name}Tests` | `ComboTests` |
| private 필드 | `_camelCase` | `_lastHitStep` |
| 상수 | PascalCase | `StepsPerSecond` |

## 파일·에셋
- 파일 하나에 공개 타입 하나가 기본(작은 enum·struct 는 `{Name}Types.cs` 에 모은다)
- `.meta` 는 Unity 가 만든다. 손으로 쓰거나 GUID 를 고치지 않는다. 폴더를 옮길 때는 `.meta` 도 같이 옮긴다
