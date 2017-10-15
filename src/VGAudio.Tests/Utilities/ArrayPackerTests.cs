using System;
using VGAudio.Tests.Formats.CriHca;
using VGAudio.Utilities;
using Xunit;

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
                packer.Add(array[0] as Array, array[1] as Type);
            }
            byte[] packed = packer.Pack();
            Array[] unpacked = ArrayUnpacker.UnpackArrays(packed);

            for (int i = 0; i < arrays.Length; i++)
            {
                Assert.Equal(arrays[i][0], unpacked[i]);
            }
        }

        [Theory]
        [MemberData(nameof(CriHcaArrays))]
        public static void TestPackingIndividual(Array array, Type type)
        {
            var packer = new ArrayPacker();
            packer.Add(array, type);
            byte[] packed = packer.Pack();

            Array[] unpacked = ArrayUnpacker.UnpackArrays(packed);
            Assert.Equal(array, unpacked[0]);
        }

        public static readonly object[][] CriHcaArrays =
        {
            new object[] { CriHcaTablesPrebuilt.QuantizeSpectrumBits, null},
            new object[] { CriHcaTablesPrebuilt.QuantizeSpectrumValue, null},
            new object[] { CriHcaTablesPrebuilt.QuantizedSpectrumBits, null},
            new object[] { CriHcaTablesPrebuilt.QuantizedSpectrumMaxBits, null},
            new object[] { CriHcaTablesPrebuilt.QuantizedSpectrumValue, null},
            new object[] { CriHcaTablesPrebuilt.ScaleToResolutionCurve, null},
            new object[] { CriHcaTablesPrebuilt.AthCurve, null},
            new object[] { CriHcaTablesPrebuilt.MdctWindow, null}
        };

        public static readonly object[][] AllArraySets =
        {
            new object[] {CriHcaArrays}
        };
    }
}
