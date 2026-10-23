using System.IO;
using Game.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Renders every screen state of the generated scene to Build/render-check/*.png (1080×2340) for review
    /// without a simulator. Needs a GPU: do not pass -nographics.
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
            var rt = new RenderTexture(1080, 2340, 24);
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
            game.EnsureBuilt();
            Directory.CreateDirectory("Build/render-check");
            foreach (GameController.State state in System.Enum.GetValues(typeof(GameController.State)))
            {
                Canvas.ForceUpdateCanvases(); // no player loop in batchmode: lay out before and after the state change
                game.Enter(state);
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"Build/render-check/{state}.png", tex.EncodeToPNG());
            }
            cam.targetTexture = null;
            RenderTexture.active = null;
            Debug.Log("[Game] RenderCheck wrote Build/render-check/*.png");
        }
    }
}
