namespace StudioElevenLib.Common.Env.Platforms
{
    /// <summary>
    /// Nintendo Switch platform.
    /// </summary>
    public sealed class NXPlatform : IPlatform
    {
        public static readonly NXPlatform Instance = new();

        private NXPlatform() { }

        public string Name => "NX";

        public char ShortName => 'N';
    }
}
