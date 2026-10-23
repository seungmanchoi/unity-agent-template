namespace Game.Editor
{
    /// <summary>
    /// App identity — the single source. `npm run app:init` rewrites these lines; fastlane/Fastfile and
    /// fastlane/Appfile read them with a regex, so keep each on one line as `Name = "value";`.
    /// </summary>
    public static class AppInfo
    {
        public const string Slug = "my-game";
        public const string Name = "My Game";
        public const string BundleId = "com.seungmanchoi.mygame";
        /// <summary>Store version string. Build number comes from fastlane (latest TestFlight build + 1).</summary>
        public const string Version = "1.0.0";
        public const string Company = "Seungman Choi";
        public const string AppleTeamId = "6Y6T9LPHH3";
    }
}
