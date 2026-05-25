using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioEleven.Modules
{
    internal static class CommandExtensions
    {
        internal static byte[] ReadStdinBase64(this ICommand _)
            => StdinHelper.ReadBase64();
    }
}
