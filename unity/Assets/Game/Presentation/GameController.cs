using Game.Domain;
using Game.Platform;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// Single-scene state machine (Title → Play → Result) with a uGUI built in code. The scene only holds this
    /// component, the camera and asset references; Editor/Build.cs generates it, so never edit the scene by hand.
    /// Autoplay for captures and perf runs: `xcrun simctl spawn booted defaults write {bundle id} autoplay -int 1`
    /// (iOS defaults are PlayerPrefs), or `-autoplay` on the desktop/editor command line.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        public enum State { Title, Play, Result }

        const float RefW = 1080f, RefH = 2340f;
        const string BestKey = "best_score"; // PlayerPrefs is plain text: non-sensitive values only

        [SerializeField] Font font;
        [SerializeField] Camera cam;

        public State Current { get; private set; }

        GameSim _sim;
        float _acc, _nextAutoTap;
        bool _autoplay, _built;
        Rect _lastSafe;
        Vector2Int _lastScreen;
        RectTransform _safe, _arena, _target;
        Text _title, _hint, _hud;

        void Awake()
        {
            Application.targetFrameRate = 60;
            _autoplay = PlayerPrefs.GetInt("autoplay", 0) == 1
                || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-autoplay") >= 0;
            EnsureBuilt();
            Enter(_autoplay ? State.Play : State.Title);
        }

        /// <summary>Builds the UI once. Public so Editor/RenderCheck can render it in batchmode.</summary>
        public void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            var canvasGo = new GameObject("Ui", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            // Screen Space - Camera so RenderCheck can capture the UI through cam.Render().
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefW, RefH);
            // Expand: the canvas is never smaller than 1080×2340 — tall phones get more height, wide windows
            // (iPhone Duo unfolded) more width, and nothing laid out for the reference size is clipped.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            _safe = Rect("SafeArea", canvasGo.transform);
            Stretch(_safe);
            // The arena keeps the sim's aspect inside the safe area; sim units map linearly onto it.
            var rules = new GameRules();
            _arena = Rect("Arena", _safe);
            Stretch(_arena);
            var fitter = _arena.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = rules.ArenaWidth / rules.ArenaHeight;

            var targetImg = Rect("Target", _arena).gameObject.AddComponent<Image>();
            targetImg.sprite = Disc();
            targetImg.color = Theme.Accent;
            _target = targetImg.rectTransform;
            _target.anchorMin = _target.anchorMax = Vector2.zero;

            _hud = Label("Hud", _safe, Theme.Body, TextAnchor.UpperCenter);
            Place(_hud.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -Theme.Gutter), new Vector2(RefW, 80));
            _title = Label("Title", _safe, Theme.Display, TextAnchor.MiddleCenter);
            Place(_title.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(RefW, 360));
            _hint = Label("Hint", _safe, Theme.Body, TextAnchor.MiddleCenter);
            Place(_hint.rectTransform, new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(RefW, 120));
        }

        public void Enter(State state)
        {
            Current = state;
            Debug.Log($"[Game] state -> {state}");
            if (state == State.Play)
            {
                _sim = new GameSim((uint)System.Environment.TickCount);
                _acc = 0f;
            }
            if (state == State.Result && !_autoplay && _sim.Score > PlayerPrefs.GetInt(BestKey, 0))
            {
                PlayerPrefs.SetInt(BestKey, _sim.Score);
                PlayerPrefs.Save();
            }
            Render();
        }

        void Update()
        {
            ApplySafeArea();
            var pointer = Pointer.current;
            bool pressed = pointer != null && pointer.press.wasPressedThisFrame;
            switch (Current)
            {
                case State.Title:
                case State.Result:
                    if (pressed) Enter(State.Play);
                    break;
                case State.Play:
                    // Fixed step: the sim advances in whole steps however long the frame was.
                    _acc += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
                    while (_acc >= GameSim.StepSeconds && !_sim.Done)
                    {
                        _sim.Tick();
                        _acc -= GameSim.StepSeconds;
                    }
                    if (pressed) Tap(pointer.position.ReadValue());
                    if (_autoplay && Time.unscaledTime >= _nextAutoTap)
                    {
                        _nextAutoTap = Time.unscaledTime + 0.45f;
                        if (_sim.Tap(_sim.TargetX, _sim.TargetY)) Haptics.Play(HapticKind.Light);
                    }
                    if (_sim.Done) Enter(State.Result);
                    else Render();
                    break;
            }
        }

        void Tap(Vector2 screen)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_arena, screen, cam, out var local)) return;
            var size = _arena.rect.size;
            float x = (local.x / size.x + _arena.pivot.x) * _sim.Rules.ArenaWidth;
            float y = (local.y / size.y + _arena.pivot.y) * _sim.Rules.ArenaHeight;
            if (_sim.Tap(x, y)) Haptics.Play(HapticKind.Light);
        }

        void Render()
        {
            bool play = Current == State.Play;
            _target.gameObject.SetActive(play);
            _hud.text = play ? $"{_sim.Score}   ·   {Mathf.CeilToInt(_sim.SecondsLeft)}s" : "";
            _title.text = Current == State.Title ? "TAP TARGETS" : Current == State.Result ? $"{_sim.Score}" : "";
            _hint.text = Current == State.Title ? "화면을 눌러 시작"
                : Current == State.Result ? $"최고 {Mathf.Max(PlayerPrefs.GetInt(BestKey, 0), _sim.Score)} · 눌러서 다시" : "";
            if (!play) return;
            var size = _arena.rect.size;
            float px = size.x / _sim.Rules.ArenaWidth;
            _target.sizeDelta = Vector2.one * (_sim.Rules.TargetRadius * 2f * px);
            _target.anchoredPosition = new Vector2(_sim.TargetX * px, _sim.TargetY * size.y / _sim.Rules.ArenaHeight);
        }

        void ApplySafeArea()
        {
            // The window resizes at runtime (iPhone Duo fold/unfold), so the screen size is part of the key.
            var safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if ((safe == _lastSafe && screen == _lastScreen) || screen.x == 0 || screen.y == 0) return;
            _lastSafe = safe;
            _lastScreen = screen;
            _safe.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _safe.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
        }

        Text Label(string name, Transform parent, int size, TextAnchor anchor)
        {
            var text = Rect(name, parent).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Theme.Ink;
            text.alignment = anchor;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static Sprite Disc()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(n / 2f - d) * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
    }
}
