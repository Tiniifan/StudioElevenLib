using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Binary.Collections;
using StudioElevenLib.Level5.Binary;
using StudioElevenLib.Level5.Compression;
using System.Runtime.InteropServices;
using StudioElevenLib.Level5.Compression.NoCompression;
using static System.Net.Mime.MediaTypeNames;
using System.Linq.Expressions;
using StudioElevenLib.Level5.Text;
using StudioElevenLib.Level5.Resource.RES;
using StudioElevenLib.Level5.Resource;
using StudioElevenLib.Level5.Resource.Types;
using StudioElevenLib.Level5.Image.IMGC;

using System.Drawing;
using StudioElevenLib.Level5.Image;
using StudioElevenLib.Level5.Image.Color_Formats;

namespace StudioElevenLibTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IMGC bc1 = new IMGC(File.ReadAllBytes("whisperSBC1.xi"));
            bc1.Bitmap.Save("whisperSBC1.png");

            Bitmap image = new Bitmap("whisperSBC1.png");
            IMGC bcNew = new IMGC(image, new BC1());
            bcNew.Save("testBC1.xi", true);

            IMGC bc1Back = new IMGC(File.ReadAllBytes("testBC1.xi"));
            bc1Back.Bitmap.Save("testBC1Back.png");

            IMGC imgcETC = new IMGC(File.ReadAllBytes("000.xi"));
            imgcETC.Bitmap.Save("test.png");

            Bitmap image2 = new Bitmap("test.png");
            IMGC imgc2 = new IMGC(image2, new ETC1());
            imgc2.Save("testETC.xi");

            //IMGC imgc3 = new IMGC(File.ReadAllBytes("testETC.xi"));
            //imgc3.Bitmap.Save("testETC.png");
        }
    }
}
