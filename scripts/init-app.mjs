#!/usr/bin/env node
// Turns the template into a new game: identity strings and the C# namespace, no code generation.
//   npm run app:init -- --slug pocket-pulse --name "Pocket Pulse" [--namespace PocketPulse] [--force]
// Sets slug / display name / bundle id com.seungmanchoi.<slug without '-'> in Editor/AppInfo.cs (fastlane
// reads them from there) and the package.json name. --namespace (default: PascalCase slug) renames the
// assemblies, namespaces, log tag and unity/Assets/<Game> folder — .meta files move with their asset, so
// GUIDs stay the same. Refuses to run twice (slug no longer my-game) unless --force.
import { execFileSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync, renameSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';

const root = fileURLToPath(new URL('..', import.meta.url));
const { values: a } = parseArgs({
  options: {
    slug: { type: 'string' },
    name: { type: 'string' },
    namespace: { type: 'string' },
    force: { type: 'boolean' },
  },
});
const fail = (msg) => {
  console.error(`app:init: ${msg}`);
  process.exit(1);
};
if (!a.slug || !a.name)
  fail('usage: npm run app:init -- --slug <kebab-slug> --name "<Display Name>" [--namespace <PascalCase>]');
if (!/^[a-z][a-z0-9-]*$/.test(a.slug))
  fail(`slug "${a.slug}" must be lowercase letters, digits and '-', starting with a letter`);
if (a.name.length > 30) fail('display name must be 30 characters or fewer (home screen / store limit)');
if (/["\\\n]/.test(a.name)) fail('name cannot contain double quotes, backslashes or newlines');
const ns = a.namespace ?? a.slug.replace(/(^|-)([a-z0-9])/g, (_, __, c) => c.toUpperCase());
if (!/^[A-Z][A-Za-z0-9]*$/.test(ns)) fail(`namespace "${ns}" must be PascalCase letters/digits`);
if (['Unity', 'UnityEngine', 'UnityEditor', 'System', 'NUnit'].includes(ns)) fail(`namespace "${ns}" clashes with a framework namespace`);

const assets = join(root, 'unity/Assets');
const oldDir = readdirSync(assets).find((d) => existsSync(join(assets, d, 'Editor/AppInfo.cs')));
const infoPath = join(assets, oldDir, 'Editor/AppInfo.cs');
const info = readFileSync(infoPath, 'utf8');
const oldNs = info.match(/namespace (\w+)\.Editor/)[1];
if (!info.includes('const string Slug = "my-game";') && !a.force)
  fail('AppInfo.cs slug is no longer my-game — already initialized (use --force to re-run)');
const bundleId = `com.seungmanchoi.${a.slug.replace(/-/g, '')}`;

// Replacement callbacks, so `$&` / `$1` in a user value is inserted literally.
writeFileSync(
  infoPath,
  info
    .replace(/(const string Slug = )"[^"]*"/, (_, k) => `${k}"${a.slug}"`)
    .replace(/(const string Name = )"[^"]*"/, (_, k) => `${k}"${a.name}"`)
    .replace(/(const string BundleId = )"[^"]*"/, (_, k) => `${k}"${bundleId}"`),
);
console.log(`updated   unity/Assets/${oldDir}/Editor/AppInfo.cs`);
const pkg = join(root, 'package.json');
writeFileSync(pkg, readFileSync(pkg, 'utf8').replace(/"name": "[^"]*"/, () => `"name": "${a.slug}"`));
console.log('updated   package.json');

if (ns !== oldNs) {
  // Text: assembly names, namespaces, log tag and Assets/<Game>/ paths in every tracked text file
  // (C#, asmdefs, DomainTests csproj, fastlane, harness docs and their Codex copies).
  const layers = '(Domain|Presentation|Platform|Editor|Tests)';
  const swaps = [
    [new RegExp(`\\b${oldNs}\\.${layers}\\b`, 'g'), (_, l) => `${ns}.${l}`],
    [new RegExp(`\\[${oldNs}\\]`, 'g'), () => `[${ns}]`],
    [new RegExp(`Assets/${oldDir}/`, 'g'), () => `Assets/${ns}/`],
  ];
  const files = execFileSync('git', ['ls-files', '-z'], { cwd: root })
    .toString()
    .split('\0')
    .filter((f) => /\.(cs|asmdef|csproj|md|mjs|toml|json|rb)$|(^|\/)(Fastfile|Appfile)$/.test(f));
  let changed = 0;
  for (const f of files) {
    const p = join(root, f);
    if (!existsSync(p)) continue;
    const before = readFileSync(p, 'utf8');
    const after = swaps.reduce((s, [re, fn]) => s.replace(re, fn), before);
    if (after !== before) {
      writeFileSync(p, after);
      changed++;
    }
  }
  console.log(`updated   ${changed} files: ${oldNs}.* → ${ns}.*`);

  // Files: asmdefs (+ .meta) and the game folder (+ .meta). Moving the .meta keeps the GUID.
  const gameDir = join(assets, oldDir);
  const asmdefs = (dir) =>
    readdirSync(dir, { withFileTypes: true }).flatMap((e) =>
      e.isDirectory() ? asmdefs(join(dir, e.name)) : e.name.endsWith('.asmdef') ? [join(dir, e.name)] : [],
    );
  for (const file of asmdefs(gameDir)) {
    const to = file.replace(new RegExp(`/${oldNs}\\.([\\w.]+)\\.asmdef$`), `/${ns}.$1.asmdef`);
    renameSync(file, to);
    if (existsSync(`${file}.meta`)) renameSync(`${file}.meta`, `${to}.meta`);
  }
  if (oldDir !== ns) {
    renameSync(gameDir, join(assets, ns));
    if (existsSync(`${gameDir}.meta`)) renameSync(`${gameDir}.meta`, join(assets, `${ns}.meta`));
    console.log(`moved     unity/Assets/${oldDir} → unity/Assets/${ns}`);
  }
}

console.log(`
Done: ${a.name} (${a.slug}) · bundle id ${bundleId} · namespace ${ns}
Next (see the init-app skill):
  1. npm run sync:codex && npm run verify
  2. npm run unity -- generate        # regenerates the scene and applies the new identity to ProjectSettings
  3. Replace unity/Assets/${ns}/Content/AppIcon.png (1024×1024 PNG, no alpha)
  4. Store records: fastlane ios create_app (ASC app record first on the web), Play Console app
  5. Firebase / AdMob only when the game uses them (docs/rules/analytics.md, docs/rules/ads.md)`);
