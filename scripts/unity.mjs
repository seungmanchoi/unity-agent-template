#!/usr/bin/env node
// Runs a Unity batchmode entry point without anyone touching the Editor GUI.
//   npm run unity -- <generate|render|test|ios|ios-sim|android|apk>
// The namespace and game folder are read from AppInfo.cs, so this keeps working after app:init --namespace.
// Unity binary: $UNITY_PATH, else the Hub install matching unity/ProjectSettings/ProjectVersion.txt.
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('..', import.meta.url));
const project = join(root, 'unity');
const game = readdirSync(join(project, 'Assets')).find((d) =>
  existsSync(join(project, 'Assets', d, 'Editor/AppInfo.cs')),
);
const ns = readFileSync(join(project, 'Assets', game, 'Editor/AppInfo.cs'), 'utf8').match(
  /namespace (\w+)\.Editor/,
)[1];
const version = readFileSync(join(project, 'ProjectSettings/ProjectVersion.txt'), 'utf8').match(
  /m_EditorVersion: (\S+)/,
)[1];
const unity =
  process.env.UNITY_PATH ??
  `/Applications/Unity/Hub/Editor/${version}/Unity.app/Contents/MacOS/Unity`;

const commands = {
  generate: { method: 'Build.Generate' },
  render: { method: 'RenderCheck.Run' }, // needs a GPU: never -nographics
  ios: { method: 'Build.BuildIOS' },
  'ios-sim': { method: 'Build.BuildIOS', env: { IOS_SIMULATOR: '1' } },
  android: { method: 'Build.BuildAndroid' },
  apk: { method: 'Build.BuildAndroid', env: { ANDROID_APK: '1' } },
  test: {}, // -runTests quits by itself; -quit would skip the run
};
const name = process.argv[2];
const cmd = commands[name];
if (!cmd) {
  console.error(`usage: npm run unity -- <${Object.keys(commands).join('|')}>`);
  process.exit(2);
}
if (!existsSync(unity)) {
  console.error(`Unity ${version} not found at ${unity} — install it with Unity Hub or set UNITY_PATH`);
  process.exit(1);
}

mkdirSync(join(root, 'build-output'), { recursive: true });
const results = join(root, 'build-output/editmode-results.xml');
const args = ['-batchmode', '-projectPath', project, '-logFile', '-'];
if (cmd.method) args.push('-quit', '-executeMethod', `${ns}.Editor.${cmd.method}`);
else args.push('-runTests', '-testPlatform', 'EditMode', '-testResults', results);

console.log(`> ${unity} ${args.join(' ')}`);
const run = spawnSync(unity, args, { stdio: 'inherit', env: { ...process.env, ...cmd.env } });
if (name === 'test' && existsSync(results)) {
  const m = readFileSync(results, 'utf8').match(/total="(\d+)" passed="(\d+)" failed="(\d+)"/);
  if (m) console.log(`EditMode tests: ${m[2]}/${m[1]} passed, ${m[3]} failed (${results})`);
}
process.exit(run.status ?? 1);
