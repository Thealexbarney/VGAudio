using System;
using VGAudio.Utilities;
using Xunit;
using CriHcaTables = VGAudio.Tests.Formats.CriHca.UnpackedTables;
using Atrac9Tables = VGAudio.Tests.Formats.Atrac9.UnpackedTables;

namespace VGAudio.Tests.Utilities
{
    public static class ArrayPackerTests
    {
        [Theory]
        [MemberData(nameof(AllArraySets))]
        public static void TestPackingAll(object[][] arrays)
        {
            var packer = new ArrayPacker();
            foreach (object[] array in arrays)
            {
                packer.Add(array[0] as Array, array[2] as Type);
            }
            byte[] packed = packer.Pack();
            Array[] unpacked = ArrayUnpacker.UnpackArrays(packed);

            for (int i = 0; i < arrays.Length; i++)
            {
                object expected = arrays[i][1] ?? arrays[i][0];
                Assert.Equal(expected, unpacked[i]);
            }
        }

        [Theory]
        [MemberData(nameof(CriHcaArrays))]
        [MemberData(nameof(Atrac9Arrays))]
        public static void TestPackingIndividual(Array array, Array expected, Type type)
        {
            expected = expected ?? array;
            var packer = new ArrayPacker();
            packer.Add(array, type);
            byte[] packed = packer.Pack();

            Array[] unpacked = ArrayUnpacker.UnpackArrays(packed);
            Assert.Equal(expected, unpacked[0]);
        }

        public static readonly object[][] CriHcaArrays =
        {
            new object[] {CriHcaTables.QuantizeSpectrumBits, null, null},
            new object[] {CriHcaTables.QuantizeSpectrumValue, null, null},
            new object[] {CriHcaTables.QuantizedSpectrumBits, null, null},
            new object[] {CriHcaTables.QuantizedSpectrumMaxBits, null, null},
            new object[] {CriHcaTables.QuantizedSpectrumValue, null, null},
            new object[] {CriHcaTables.ScaleToResolutionCurve, null, null},
            new object[] {CriHcaTables.AthCurve, null, null},
            new object[] {CriHcaTables.MdctWindowF, CriHcaTables.MdctWindow, typeof(double)},
            new object[] {CriHcaTables.DefaultChannelMapping, null, null},
            new object[] {CriHcaTables.ValidChannelMappings, null, null}
        };

        public static readonly object[][] Atrac9Arrays =
        {
            new object[] {Atrac9Tables.HuffmanSpectrumABits, null, null}, //0 b
            new object[] {Atrac9Tables.HuffmanSpectrumBBits, null, null}, //1 b
            new object[] {Atrac9Tables.HuffmanScaleFactorsABits, null, null}, //2 b
            new object[] {Atrac9Tables.HuffmanScaleFactorsBBits, null, null}, //3 b
            new object[] {Atrac9Tables.HuffmanSpectrumACodes, null, null}, //4 s
            new object[] {Atrac9Tables.HuffmanSpectrumBCodes, null, null}, //5 s
            new object[] {Atrac9Tables.HuffmanSpectrumAGroupSizes, null, null}, //6 b
            new object[] {Atrac9Tables.HuffmanSpectrumBGroupSizes, null, null}, //7 b
            new object[] {Atrac9Tables.HuffmanScaleFactorsGroupSizes, null, null}, //8 b
            new object[] {Atrac9Tables.HuffmanScaleFactorsACodes, null, null}, //9 s
            new object[] {Atrac9Tables.HuffmanScaleFactorsBCodes, null, null}, //10 s
            new object[] {Atrac9Tables.SfWeights, null, null}, //11 b
            new object[] {Atrac9Tables.BexGroupInfo, null, null}, //12 b
            new object[] {Atrac9Tables.BexEncodedValueCounts, null, null}, //13 b
            new object[] {Atrac9Tables.BexDataLengths, null, null}, //14 b
            new object[] {Atrac9Tables.BexMode0Bands3, null, null}, //15 d
            new object[] {Atrac9Tables.BexMode0Bands4, null, null}, //16 d
            new object[] {Atrac9Tables.BexMode0Bands5, null, null}, //17 d
            new object[] {Atrac9Tables.BexMode2Scale, null, null}, //18 d
            new object[] {Atrac9Tables.BexMode3Initial, null, null}, //19 d
            new object[] {Atrac9Tables.BexMode3Rate, null, null}, //20 d
            new object[] {Atrac9Tables.BexMode4Multiplier, null, null} //21 d
        };

        public static readonly object[][] AllArraySets =
        {
            new object[] {CriHcaArrays},
            new object[] {Atrac9Arrays}
        };
    }
}
