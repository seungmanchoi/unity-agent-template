# AGENTS.md

Codex entry point for this repository. **The single source of truth is `CLAUDE.md` — read it in full before starting any task.** It holds every Hard Threshold table plus a short summary per domain; the detailed rules live in `docs/rules/*.md` (unity-workflow, ads, analytics, store review, secure storage, build/deploy, runtime testing, tooling) — read the one for the area you touch. This file repeats the pass/fail rules and the flows that must never be skipped.

If `~/works/AGENTS.md` exists (the maintainer's personal-app rules: store deploy, ads, bundle ID, signing), read it before any task. It sits above this repo's git root, so Codex does not load it automatically.

## Project

Unity mobile game template with an AI agent harness for the full lifecycle (idea → plan → spec → design → build → QA → deploy → iterate).

- Unity **6000.3.26f1** (Personal) · Input System **1.20.0** (Active Input Handling = Input System) · Test Framework **1.6.0** · uGUI **2.0.0** — all pinned
- Code-built uGUI (CanvasScaler 1080×2340 Expand, safe area, Noto Sans KR) · IL2CPP ARM64 · portrait
- Domain tests through `dotnet test` (net10.0, C# 9, NUnit) without a Unity license; Unity EditMode tests through `-runTests`
- Optional SDKs, added only in `Platform/`: Google Mobile Ads Unity 11.5.0 (UMP) · Firebase Unity 13.17.0 (Analytics + Crashlytics)
- Deploy: Unity batchmode → Xcode/Gradle → fastlane (`build_unity` → `upload`) through the store-deploy plugin

## Harness in Codex

| Claude harness | Codex copy (generated — never edit) |
| --- | --- |
| `.claude/agents/<name>.md` | `.codex/agents/<name>.toml` |
| `.claude/skills/<name>/` | `.agents/skills/<name>/` |

Edit only the `.claude/` sources, then run `npm run sync:codex` (`npm run verify` fails while the copies are stale). Invoke a skill with `$name`, pick it in `/skills`, or describe the task. When a task maps to a role, read that agent's instructions first:

| Task | Agent |
| --- | --- |
| New game from this template (slug, name, bundle id, namespace) | `init-app` skill — run first while `AppInfo.cs` still has `Slug = "my-game"` |
| Idea / market research | `idea-researcher` |
| PRD / KPIs | `product-planner` |
| Specs / task breakdown | `spec-planner` |
| Visual direction / theme / design review | `design-architect` (+ `design-review` skill) |
| Domain modules (pure C# rules, sims, tests) | `module-builder` |
| Screens, input, FX, audio, scene generator | `presentation-developer` |
| Ads, analytics, storage, store review, haptics, native bridges | `platform-integrator` |
| Code quality gate | `qa-reviewer` |
| Simulator functional / UX inspection | `app-inspector` |
| Post-launch iteration loop | `loop-engineer` |

Full builds follow the `orchestrate` skill's phases; post-launch work uses `iterate-app` (Codex has no `/loop` — ask for N cycles). Read Claude tool names in the docs through `.agents/skills/orchestrate/references/runtime-adapters.md`. Inter-agent artifacts live in `_workspace/`; `spec.md`, `plan/` and `iterate/` are committed, the rest is scratch.

**Setup once per machine**
- Trust this repo's hooks in `/hooks`: `.codex/hooks.json` runs `scripts/verify-hook.mjs` at Stop. Trusted or not, run `npm run verify` yourself before saying a task is done.
- Store deploy skills: `codex plugin marketplace add seungmanchoi/store-deploy-plugin`, then `codex plugin add store-deploy@store-deploy-marketplace`.
- .NET SDK 10 for the Domain tests (`brew install dotnet`), Unity 6000.3.26f1 through Unity Hub with a Personal license for batchmode.

## Hard Thresholds (any violation = FAIL)

**Quality** — `npm run verify` green (check:harness + Domain `dotnet test`) and no `error CS` in `npm run unity -- generate`
- Assembly direction `Domain ← Platform ← Presentation ← Editor`, `Tests.EditMode → Domain` only
- Domain: `noEngineReferences: true`, no `UnityEngine`/`UnityEditor`, no clock reads, no `System.Random` (seeded `Rng`), fixed-step and deterministic
- SDK `using`s and `[DllImport]` only in `Platform/`
- Every code-built canvas applies `Screen.safeArea`; touch targets ≥ 132 reference px
- Pins: editor 6000.3.26f1, Input System 1.20.0, Test Framework 1.6.0, uGUI 2.0.0, exact versions only in `Packages/manifest.json`
- No hand-edited Unity YAML or `.meta` GUIDs; `.meta` committed for every asset; `Generated/`, `Library/`, `Build/` never committed; running `generate` twice leaves git clean
- Local day keys from local time (`DateTime.Now.ToString("yyyy-MM-dd")`), never from UTC

**Never use the Editor GUI** — scenes/prefabs come from `Editor/Build.cs` `Generate()` in batchmode (`npm run unity -- generate`). `render` needs a GPU (no `-nographics`); `test` runs `-runTests` without `-quit`.

**Design** — visual changes need a Codex design review on real simulator captures (Design Quality · Originality · Craft · Functionality, P0/P1/P2) and a GO before done.

**Secure storage** — `PlayerPrefs` is plaintext: settings, flags and best scores only. Tokens, secrets and receipts go through the Platform Keychain/Keystore bridge; never in logs or analytics.

**Store review** — only through the Domain `ReviewPolicy` + Platform bridge; no custom pre-prompt, no score-begging, no branching on the result, never from error handlers.

**Ads** — only `Ads.InitializeWithConsent()` (UMP → ATT → `SetRequestConfiguration` → `MobileAds.Initialize`); every load waits for `Ads.IsReady`. Rewrite the ATT purpose string per game (what, why, example, outcome of declining) — "This identifier will be used to deliver personalized ads to you." is an automatic rejection. One banner per screen, interstitials only through the shared cooldown / daily cap gate, rewards only in the earned callback, load retries with backoff, real-ID internal builds only on registered test devices.

**Analytics** — KPIs in the PRD; only the Platform `Analytics` wrapper; event-name constants, snake_case, no PII; collection only in production builds; `GoogleService-Info.plist` / `google-services.json` never committed.

## Flows That Must Not Be Skipped

- **Identity**: `unity/Assets/Game/Editor/AppInfo.cs` is the single source (fastlane reads it). Bundle id `com.seungmanchoi.{slug without -}` cannot change after the first upload.
- **iOS deploy**: `fastlane ios build_unity` (TestFlight build +1, Unity BuildIOS, `ITSAppUsesNonExemptEncryption=false`, sigh, manual signing on the `Unity-iPhone` target only, gym) → `fastlane ios upload`. Use the team distribution certificate already in `login.keychain`; never create certificates or keychains, never pass passwords on the command line.
- **Android deploy**: keystore only through `ANDROID_KEYSTORE*` env vars; `fastlane android build_unity build:N` → `fastlane android internal` (the first AAB goes up by hand in Play Console).
- **Simulator checks**: `fastlane ios build_unity_sim` → `xcrun simctl install booted <app>` → `xcrun simctl launch --console-pty booted <bundle>`; drive and read the UI with `sim-use` only.
