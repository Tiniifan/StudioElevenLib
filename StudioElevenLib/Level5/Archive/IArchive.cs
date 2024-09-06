using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Archive
{
    public interface IArchive
    {
        string Name { get; }

        VirtualDirectory Directory { get; set; }

        void Save(string path);

        void Close();
    }
}