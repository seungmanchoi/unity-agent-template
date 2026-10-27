using System.IO;
using Game.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Renders every screen state of the generated scene to Build/render-check/*.png (1080×2340) and
    /// Build/render-check/duo-inner/*.png (iPhone Duo unfolded, 2007×2853) for review without a simulator.
    /// Needs a GPU: do not pass -nographics.
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Game.Editor.RenderCheck.Run -logFile -
    /// Simulator captures (sim-use + simctl screenshot) stay the source of truth for design reviews.
    /// </summary>
    public static class RenderCheck
    {
        public static void Run()
        {
            Build.Generate();
            EditorSceneManager.OpenScene(Build.MainScene);
            var game = Object.FindFirstObjectByType<GameController>();
            var cam = Camera.main;
            game.EnsureBuilt();
            Render(game, cam, 1080, 2340, "Build/render-check");
            Render(game, cam, 2007, 2853, "Build/render-check/duo-inner");
            Debug.Log("[Game] RenderCheck wrote Build/render-check/*.png and duo-inner/*.png");
        }

        static void Render(GameController game, Camera cam, int width, int height, string dir)
        {
            var rt = new RenderTexture(width, height, 24);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
            // No player loop in batchmode: re-enable the scaler so it rescales for this target size.
            var scaler = game.GetComponentInChildren<UnityEngine.UI.CanvasScaler>();
            scaler.enabled = false;
            scaler.enabled = true;
            Directory.CreateDirectory(dir);
            foreach (GameController.State state in System.Enum.GetValues(typeof(GameController.State)))
            {
                Canvas.ForceUpdateCanvases(); // no player loop in batchmode: lay out before and after the state change
                game.Enter(state);
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"{dir}/{state}.png", tex.EncodeToPNG());
            }
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(tex);
            rt.Release();
        }
    }
}
