using System;
using System.IO;
using Game.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Batchmode entry points. Agents never open the Editor GUI: C#, JSON and source assets are the truth and
    /// scenes are generated here (Assets/Game/Generated is gitignored — never hand-edit Unity YAML or .meta).
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Game.Editor.Build.Generate -logFile -
    /// Env: APP_VERSION (default AppInfo.Version), BUILD_NUMBER (default 1), IOS_SIMULATOR=1 for a simulator
    /// Xcode project, ANDROID_APK=1 for an emulator APK.
    /// Android signing: ANDROID_KEYSTORE, ANDROID_KEY_ALIAS, ANDROID_KEYSTORE_PASS, ANDROID_KEY_PASS (env only).
    /// </summary>
    public static class Build
    {
        public const string MainScene = "Assets/Game/Generated/Main.unity";
        const string FontPath = "Assets/Game/Content/Fonts/NotoSansKR-Bold.otf";
        const string IconPath = "Assets/Game/Content/AppIcon.png";

        /// <summary>Idempotent: rebuilds the scene from code and applies identity/player settings.</summary>
        public static void Generate()
        {
            if (UseInputSystemBackend()) Debug.LogWarning("[Game] Active Input Handling switched to the Input System; restart Unity before building");
            ApplyPlayerSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(MainScene));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Background;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            var game = new GameObject("Game").AddComponent<GameController>();
            var so = new SerializedObject(game);
            so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(FontPath)
                ?? throw new FileNotFoundException("missing font", FontPath);
            so.FindProperty("cam").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MainScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Game] Generate done: " + MainScene);
        }

        public static void BuildIOS()
        {
            Generate();
            PlayerSettings.iOS.buildNumber = Env("BUILD_NUMBER", "1");
            PlayerSettings.iOS.appleDeveloperTeamID = AppInfo.AppleTeamId;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            // IOS_SIMULATOR=1 builds for the simulator (sim-use checks); devices need the default SDK.
            bool sim = Env("IOS_SIMULATOR", "") == "1";
            PlayerSettings.iOS.sdkVersion = sim ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64; // Apple Silicon Macs
            Run(BuildTarget.iOS, sim ? "Build/ios-sim" : "Build/ios");
        }

        public static void BuildAndroid()
        {
            Generate();
            PlayerSettings.Android.bundleVersionCode = int.Parse(Env("BUILD_NUMBER", "1"));
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            // ANDROID_APK=1 builds an installable APK for emulator checks; the store gets the AAB.
            bool apk = Env("ANDROID_APK", "") == "1";
            EditorUserBuildSettings.buildAppBundle = !apk;
            string ks = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE");
            if (!string.IsNullOrEmpty(ks))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ks;
                PlayerSettings.Android.keystorePass = Env("ANDROID_KEYSTORE_PASS", "");
                PlayerSettings.Android.keyaliasName = Env("ANDROID_KEY_ALIAS", "");
                PlayerSettings.Android.keyaliasPass = Env("ANDROID_KEY_PASS", "");
            }
            Run(BuildTarget.Android, $"Build/android/{AppInfo.Slug}.{(apk ? "apk" : "aab")}");
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = AppInfo.Company;
            PlayerSettings.productName = AppInfo.Name;
            PlayerSettings.bundleVersion = Env("APP_VERSION", AppInfo.Version);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, AppInfo.BundleId);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, AppInfo.BundleId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            // One 1024 icon without alpha; Unity scales it to every size, App Store marketing icon included
            // (a missing icon is an App Store rejection).
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath)
                ?? throw new FileNotFoundException("missing app icon", IconPath);
            if (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath) }, IconKind.Any);
        }

        static void Run(BuildTarget target, string output)
        {
            if (UseInputSystemBackend())
            {
                Debug.LogError("[Game] Active Input Handling was not the Input System; switched it, run the build again");
                EditorApplication.Exit(1);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? output);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            });
            Debug.Log($"[Game] {target} build: {report.summary.result}, {report.summary.totalErrors} errors");
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>
        /// Sets Player > Active Input Handling to the Input System package (1). There is no public API and the
        /// backend only changes on restart, so this returns true when it had to switch.
        /// </summary>
        static bool UseInputSystemBackend()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var handler = settings.FindProperty("activeInputHandler");
            if (handler.intValue == 1) return false;
            handler.intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return true;
        }

        static string Env(string key, string fallback)
        {
            string v = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(v) ? fallback : v;
        }
    }
}
