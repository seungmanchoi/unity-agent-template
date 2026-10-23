namespace Game.Domain
{
    /// <summary>Tunables. Arena units are abstract: Presentation fits the arena into the safe area.</summary>
    public sealed class GameRules
    {
        public float ArenaWidth = 1000f;
        public float ArenaHeight = 1600f;
        public float TargetRadius = 110f;
        public int DurationSteps = 20 * GameSim.StepsPerSecond;
        /// <summary>A target that is not hit in time moves on its own.</summary>
        public int TargetLifeSteps = (int)(1.2f * GameSim.StepsPerSecond);
    }

    /// <summary>
    /// Sample game: tap the target before it moves. Deterministic fixed-step simulation — no clock, no engine,
    /// no global random. Presentation calls Tick() at StepsPerSecond and forwards taps in arena units.
    /// Replace this with the real game; keep the shape (seed + rules in, plain state out).
    /// </summary>
    public sealed class GameSim
    {
        public const int StepsPerSecond = 60;
        public const float StepSeconds = 1f / StepsPerSecond;

        public readonly GameRules Rules;
        Rng _rng;
        int _targetUntil;

        public int Step { get; private set; }
        public int Score { get; private set; }
        public int Misses { get; private set; }
        public float TargetX { get; private set; }
        public float TargetY { get; private set; }
        public bool Done => Step >= Rules.DurationSteps;
        public float SecondsLeft => (float)(Rules.DurationSteps - Step) / StepsPerSecond;

        public GameSim(uint seed, GameRules rules = null)
        {
            Rules = rules ?? new GameRules();
            _rng = new Rng(seed);
            MoveTarget();
        }

        public void Tick()
        {
            if (Done) return;
            Step++;
            if (Step >= _targetUntil)
            {
                Misses++;
                MoveTarget();
            }
        }

        /// <returns>True when the tap hit the target.</returns>
        public bool Tap(float x, float y)
        {
            if (Done) return false;
            float dx = x - TargetX, dy = y - TargetY, r = Rules.TargetRadius;
            if (dx * dx + dy * dy > r * r) return false;
            Score++;
            MoveTarget();
            return true;
        }

        void MoveTarget()
        {
            float r = Rules.TargetRadius;
            TargetX = _rng.Range(r, Rules.ArenaWidth - r);
            TargetY = _rng.Range(r, Rules.ArenaHeight - r);
            _targetUntil = Step + Rules.TargetLifeSteps;
        }
    }
}
