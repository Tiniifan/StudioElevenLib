using System.Collections.Generic;

namespace StudioElevenLib.Level5.Text.Logic
{
    public class TextConfig
    {
        public int WashaID;

        public List<StringLevel5> Strings;

        public TextConfig()
        {

        }

        public TextConfig(List<StringLevel5> strings, int washaID = 0)
        {
            WashaID = washaID;
            Strings = strings;
        }
    }
}
