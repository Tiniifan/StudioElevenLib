namespace StudioElevenLib.Common.Env.Platforms
{
    /// <summary>
    /// Android platform.
    /// </summary>
    public sealed class AndroidPlatform : IPlatform
    {
        public static readonly AndroidPlatform Instance = new();

        private AndroidPlatform() { }

        public string Name => "ANDROID";

        public char ShortName => 'A';
    }
}
