namespace Game.Domain
{
    /// <summary>
    /// Seeded xorshift32. Same seed, same sequence on every runtime (Unity Mono/IL2CPP and dotnet test),
    /// which System.Random and UnityEngine.Random do not promise. Domain code uses only this.
    /// </summary>
    public struct Rng
    {
        uint _state;

        public Rng(uint seed) => _state = seed == 0 ? 0x9E3779B9u : seed;

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return _state = x;
        }

        /// <summary>Uniform in [0, 1).</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / (1 << 24));

        public float Range(float min, float max) => min + (max - min) * NextFloat();
    }
}
