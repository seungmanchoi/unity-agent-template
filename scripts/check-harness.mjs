#!/usr/bin/env node
// Hard Threshold checks for the Unity project (CLAUDE.md). There is no Unity compiler here, so these are
// static checks on asmdefs, sources, package pins, ProjectSettings and git state. Part of `npm run verify`
// (the Domain tests run next via dotnet test); exits 1 on any violation.
import { execFileSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('..', import.meta.url));
const read = (p) => (existsSync(join(root, p)) ? readFileSync(join(root, p), 'utf8') : '');
const failures = [];
const expect = (ok, message) => ok || failures.push(message);
const walk = (dir) =>
  existsSync(join(root, dir))
    ? readdirSync(join(root, dir)).flatMap((name) => {
        const p = join(dir, name);
        return statSync(join(root, p)).isDirectory() ? walk(p) : [p];
      })
    : [];
// Comments and string literals are stripped so a "// TODO: UnityEngine" does not count.
const code = (src) =>
  src
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/\/\/.*$/gm, '')
    .replace(/@?"(?:[^"\\\n]|\\.)*"/g, '""');

// Codex copies match the .claude sources.
try {
  execFileSync(process.execPath, [join(root, 'scripts/sync-codex-harness.mjs'), '--check'], {
    stdio: 'pipe',
  });
} catch (e) {
  failures.push(e.stderr.toString().trim());
}

// The game folder is the one holding Editor/AppInfo.cs (Assets/Game until app:init --namespace).
const assets = 'unity/Assets';
const gameName = readdirSync(join(root, assets)).find((d) =>
  existsSync(join(root, assets, d, 'Editor/AppInfo.cs')),
);
if (!gameName) {
  console.error(`check-harness: no ${assets}/<Game>/Editor/AppInfo.cs`);
  process.exit(1);
}
const game = `${assets}/${gameName}`;
const ns = read(`${game}/Editor/AppInfo.cs`).match(/namespace (\w+)\.Editor/)?.[1];
const cs = walk(game).filter((f) => f.endsWith('.cs'));
const inLayer = (layer) => (f) => f.startsWith(`${game}/${layer}/`);

// Pinned engine and packages. Bumping a pin is a deliberate change: update it here and in CLAUDE.md.
const PINS = {
  editor: '6000.3.26f1',
  'com.unity.inputsystem': '1.20.0',
  'com.unity.test-framework': '1.6.0',
  'com.unity.ugui': '2.0.0',
};
expect(
  read('unity/ProjectSettings/ProjectVersion.txt').includes(`m_EditorVersion: ${PINS.editor}\n`),
  `ProjectVersion.txt: Unity editor must be ${PINS.editor}`,
);
const deps = JSON.parse(read('unity/Packages/manifest.json') || '{}').dependencies ?? {};
for (const [pkg, version] of Object.entries(PINS).filter(([k]) => k !== 'editor'))
  expect(deps[pkg] === version, `Packages/manifest.json: ${pkg} must be pinned to ${version}`);
for (const [pkg, version] of Object.entries(deps))
  expect(
    /^\d+\.\d+\.\d+(-[\w.]+)?$/.test(version) ||
      /#[0-9a-f]{7,40}$/.test(version) ||
      /^file:.*-\d+\.\d+\.\d+\.tgz$/.test(version), // Google's UPM tarballs (Firebase, GMA)
    `Packages/manifest.json: ${pkg} "${version}" is not an exact version (or a commit-pinned git URL / versioned .tgz)`,
  );
expect(
  /activeInputHandler: 1\b/.test(read('unity/ProjectSettings/ProjectSettings.asset')),
  'ProjectSettings.asset: Active Input Handling must be the Input System (activeInputHandler: 1)',
);

// Assembly layering: Domain ← Platform ← Presentation ← Editor; Tests see Domain only.
const ALLOWED = {
  Domain: [],
  Platform: ['Domain'],
  Presentation: ['Domain', 'Platform'],
  Editor: ['Domain', 'Platform', 'Presentation'],
  'Tests.EditMode': ['Domain'],
};
for (const file of walk(game).filter((f) => f.endsWith('.asmdef'))) {
  const asm = JSON.parse(read(file));
  const layer = asm.name.slice(ns.length + 1);
  expect(asm.name.startsWith(`${ns}.`) && layer in ALLOWED, `${file}: unknown assembly ${asm.name}`);
  for (const ref of asm.references.filter((r) => r.startsWith(`${ns}.`)))
    expect(
      (ALLOWED[layer] ?? []).includes(ref.slice(ns.length + 1)),
      `${file}: ${asm.name} must not reference ${ref} (layering)`,
    );
  if (layer === 'Domain') {
    expect(asm.noEngineReferences === true, `${file}: Domain needs "noEngineReferences": true`);
    expect(asm.references.length === 0, `${file}: Domain must not reference any assembly`);
  }
}

// Domain is pure and deterministic: no engine, no clock, no unseeded or global randomness.
for (const f of cs.filter(inLayer('Domain'))) {
  const src = code(read(f));
  expect(!/\bUnity(Engine|Editor)\b/.test(src), `${f}: Domain must not use UnityEngine/UnityEditor`);
  expect(!/DateTime\.(Utc)?Now|Stopwatch|Environment\.TickCount/.test(src), `${f}: Domain must not read the clock — pass time in`);
  expect(!/new\s+(System\.)?Random\s*\(/.test(src), `${f}: Domain must use the seeded Rng, not System.Random`);
}

// SDKs and native bridges only in Platform (ads, analytics, crash reporting, store review, IAP).
const SDK = /\busing\s+(GoogleMobileAds|Firebase|Google\.Play|UnityEngine\.Purchasing|Unity\.Services)\b|\[DllImport\(/;
for (const f of cs.filter((f) => !inLayer('Platform')(f) && !inLayer('Editor')(f)))
  expect(!SDK.test(code(read(f))), `${f}: SDK / native calls belong in ${game}/Platform`);

// Every code-built canvas follows the safe area and never shrinks below the reference size.
for (const f of cs.filter(inLayer('Presentation'))) {
  const src = code(read(f));
  if (/AddComponent<CanvasScaler>|typeof\(CanvasScaler\)/.test(src))
  {
    expect(/Screen\.safeArea/.test(src), `${f}: builds a Canvas but never applies Screen.safeArea`);
    // Width-match alone clips the bottom on wide windows (iPhone Duo unfolded).
    expect(/ScreenMatchMode\.Expand/.test(src), `${f}: CanvasScaler must use ScreenMatchMode.Expand`);
  }
}

// Local calendar days come from local time; UTC is for stored timestamps only.
for (const f of cs) {
  const src = read(f);
  expect(
    !/UtcNow(\.Date\b|\.ToString\(\s*"yyyy-MM-dd")/.test(src),
    `${f}: local day key from UTC — use DateTime.Now.ToString("yyyy-MM-dd")`,
  );
  // PlayerPrefs is plain text (NSUserDefaults / SharedPreferences): never tokens or secrets.
  expect(
    !/PlayerPrefs\.Set\w+\(\s*"[^"]*(token|secret|password|receipt)/i.test(src),
    `${f}: secrets in PlayerPrefs — use the Keychain/Keystore bridge (docs/rules/secure-storage.md)`,
  );
  expect(
    !src.includes('deliver personalized ads to you'),
    `${f}: generic ATT purpose string (auto-rejected) — rewrite it per app`,
  );
}

// Never committed: Firebase config, signing material, generated scenes, Unity caches.
const tracked = execFileSync('git', ['ls-files'], { cwd: root }).toString().split('\n').filter(Boolean);
for (const f of tracked) {
  if (/(^|\/)(GoogleService-Info\.plist|google-services\.json)$/.test(f))
    failures.push(`${f}: Firebase config must not be committed`);
  if (/\.(p12|p8|jks|keystore|mobileprovision)$|(^|\/)credentials\.json$/.test(f))
    failures.push(`${f}: signing material must not be committed`);
  if (/^unity\/(Library|Temp|Logs|UserSettings|Build)\/|^unity\/Assets\/[^/]+\/Generated\//.test(f))
    failures.push(`${f}: generated/cache file must not be committed`);
}
// Every asset has its .meta (Unity regenerates a missing one with a new GUID and breaks references).
const trackedSet = new Set(tracked);
for (const f of tracked.filter((f) => f.startsWith(`${assets}/`) && !f.endsWith('.meta')))
  expect(trackedSet.has(`${f}.meta`), `${f}: missing ${relative(root, join(root, f))}.meta in git — let Unity generate it, then commit it`);

if (failures.length) {
  console.error(`check-harness: ${failures.length} violation(s)\n  - ${failures.join('\n  - ')}`);
  process.exit(1);
}
console.log('check-harness: OK');
