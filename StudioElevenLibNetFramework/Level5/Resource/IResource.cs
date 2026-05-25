using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Resource;
using StudioElevenLib.Level5.Resource.Types;

namespace StudioElevenLib.Level5.Resource
{
    public interface IResource
    {
        string Name { get; }

        Dictionary<RESType, List<RESElement>> Items { get; set; }

        void Save(string magic, string filepath);
    }
}
