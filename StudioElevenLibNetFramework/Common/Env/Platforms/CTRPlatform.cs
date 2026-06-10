namespace StudioElevenLib.Common.Env.Platforms
{
    /// <summary>
    /// Nintendo 3DS platform.
    /// </summary>
    public sealed class CTRPlatform : IPlatform
    {
        public static readonly CTRPlatform Instance = new();

        private CTRPlatform() { }

        public string Name => "CTR";

        public char ShortName => 'C';
    }
}
