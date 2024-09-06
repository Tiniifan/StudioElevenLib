using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Resource
{
    public interface IResource
    {
        string Name { get; }

        List<string> StringTable { get; set; }

        Dictionary<RESType, List<byte[]>> Items { get; set; }

        void Save(string path);
    }
}
