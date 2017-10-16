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
            new object[] { UnpackedTables.QuantizeSpectrumBits, null, null},
            new object[] { UnpackedTables.QuantizeSpectrumValue, null, null},
            new object[] { UnpackedTables.QuantizedSpectrumBits, null, null},
            new object[] { UnpackedTables.QuantizedSpectrumMaxBits, null, null},
            new object[] { UnpackedTables.QuantizedSpectrumValue, null, null},
            new object[] { UnpackedTables.ScaleToResolutionCurve, null, null},
            new object[] { UnpackedTables.AthCurve, null, null},
            new object[] { UnpackedTables.MdctWindowF, UnpackedTables.MdctWindow, typeof(double) }
        };

        public static readonly object[][] AllArraySets =
        {
            new object[] {CriHcaArrays}
        };
    }
}
