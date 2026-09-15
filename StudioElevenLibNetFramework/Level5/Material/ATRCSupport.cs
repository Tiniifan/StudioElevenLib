using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StudioElevenLib.Level5.Material
{
    public class ATRCSupport
    {
        public const int HeaderSize = 0xC;

        public const int LegacyHeaderSize = 0x8;

        public const int V1PayloadSize = 0x44;

        public const int V2PayloadSize = 0x3C;

        public const uint Inherit32 = 0xFFFFFFFF;

        public const uint Inherit16 = 0x0000FFFF;

        public const byte InheritByte = 0xFF;

        public const ushort InheritShort = 0xFFFF;

        // The engine gates the V2 floats with a ">= 0.0" test, so the sentinel is -1.0f, not a NaN
        public const float InheritFloat = -1.0f;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ATRCHeader
        {
            public long Magic;
            public ushort DataOffset;
            public ushort Unused;
        }

        public enum CompareFunction
        {
            Never = 0x0200,
            Always = 0x0207,
            Equal = 0x0202,
            NotEqual = 0x0205,
            Less = 0x0201,
            LEqual = 0x0203,
            Greater = 0x0204,
            GEqual = 0x0206
        }

        public enum BlendEquation
        {
            Add = 0x8006,
            Subtract = 0x800A,
            ReverseSubtract = 0x800B,
            Min = 0x8007,
            Max = 0x8008
        }

        public enum BlendFactor
        {
            Zero = 0x0000,
            One = 0x0001,
            DstColor = 0x0306,
            OneMinusDstColor = 0x0307,
            DstAlpha = 0x0304,
            OneMinusDstAlpha = 0x0305,
            SrcColor = 0x0300,
            OneMinusSrcColor = 0x0301,
            SrcAlpha = 0x0302,
            OneMinusSrcAlpha = 0x0303,
            SrcAlphaSaturate = 0x0308,
            ConstantColor = 0x8001,
            OneMinusConstantColor = 0x8002,
            ConstantAlpha = 0x8003,
            OneMinusConstantAlpha = 0x8004
        }

        // ASSUMPTION: V2 stores the internal index of the enums, in the order below
        public static readonly CompareFunction[] CompareFunctionIndexes =
        {
            CompareFunction.Never,
            CompareFunction.Always,
            CompareFunction.Equal,
            CompareFunction.NotEqual,
            CompareFunction.Less,
            CompareFunction.LEqual,
            CompareFunction.Greater,
            CompareFunction.GEqual
        };

        public static readonly BlendEquation[] BlendEquationIndexes =
        {
            BlendEquation.Add,
            BlendEquation.Subtract,
            BlendEquation.ReverseSubtract,
            BlendEquation.Min,
            BlendEquation.Max
        };

        public static readonly BlendFactor[] BlendFactorIndexes =
        {
            BlendFactor.Zero,
            BlendFactor.One,
            BlendFactor.DstColor,
            BlendFactor.OneMinusDstColor,
            BlendFactor.DstAlpha,
            BlendFactor.OneMinusDstAlpha,
            BlendFactor.SrcColor,
            BlendFactor.OneMinusSrcColor,
            BlendFactor.SrcAlpha,
            BlendFactor.OneMinusSrcAlpha,
            BlendFactor.SrcAlphaSaturate,
            BlendFactor.ConstantColor,
            BlendFactor.OneMinusConstantColor,
            BlendFactor.ConstantAlpha,
            BlendFactor.OneMinusConstantAlpha
        };

        public static T? FromIndex<T>(IReadOnlyList<T> indexes, byte value) where T : struct
        {
            if (value == InheritByte || value >= indexes.Count)
            {
                return null;
            }

            return indexes[value];
        }

        public static byte ToIndex<T>(IReadOnlyList<T> indexes, T? value) where T : struct
        {
            if (value.HasValue)
            {
                for (int i = 0; i < indexes.Count; i++)
                {
                    if (indexes[i].Equals(value.Value))
                    {
                        return (byte)i;
                    }
                }
            }

            return InheritByte;
        }

        public static bool IsInherited(uint value)
        {
            return value == Inherit32 || (value & 0xFFFF) == 0xFFFF;
        }
    }
}
