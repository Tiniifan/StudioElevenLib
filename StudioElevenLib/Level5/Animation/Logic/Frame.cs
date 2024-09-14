using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class Frame
    {
        public int Key { get; set; }

        public object Value { get; set; }

        public Frame(int key)
        {
            Key = key;
        }

        public Frame(int key, object value)
        {
            Key = key;
            Value = value;
        }
    }
}
