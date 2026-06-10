using System;
using System.Linq;

namespace StudioElevenLib.Common.Env.Platforms
{
    /// <summary>
    /// Central registry for all known platforms.
    /// </summary>
    public static class Platforms
    {
        /// <summary>
        /// Array containing all registered platforms.
        /// </summary>
        public static readonly IPlatform[] All =
        {
            CTRPlatform.Instance,
            NXPlatform.Instance,
            AndroidPlatform.Instance
        };

        /// <summary>
        /// Resolves a platform from its magic character.
        /// </summary>
        public static IPlatform FromMagic(char magic)
        {
            return All.FirstOrDefault(x => x.ShortName == magic)
                ?? throw new NotSupportedException(
                    $"Unknown short name '{magic}'.");
        }

        /// <summary>
        /// Resolves a platform from its name.
        /// </summary>
        public static IPlatform FromName(string name)
        {
            return All.FirstOrDefault(x =>
                x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? throw new NotSupportedException(
                    $"Unknown platform '{name}'.");
        }
    }
}
