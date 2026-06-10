namespace StudioElevenLib.Common.Env
{
    /// <summary>
    /// Represents a Level-5 target platform.
    /// </summary>
    public interface IPlatform
    {
        /// <summary>
        /// Platform display name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Platform magic short name used in file headers.
        /// </summary>
        char ShortName { get; }
    }
}
