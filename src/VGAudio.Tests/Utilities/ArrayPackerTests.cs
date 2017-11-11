using System;
using VGAudio.Utilities;
using Xunit;
using CriHcaTables = VGAudio.Tests.Formats.CriHca.UnpackedTables;

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
            new object[] { CriHcaTables.QuantizeSpectrumBits, null, null},
            new object[] { CriHcaTables.QuantizeSpectrumValue, null, null},
            new object[] { CriHcaTables.QuantizedSpectrumBits, null, null},
            new object[] { CriHcaTables.QuantizedSpectrumMaxBits, null, null},
            new object[] { CriHcaTables.QuantizedSpectrumValue, null, null},
            new object[] { CriHcaTables.ScaleToResolutionCurve, null, null},
            new object[] { CriHcaTables.AthCurve, null, null},
            new object[] { CriHcaTables.MdctWindowF, CriHcaTables.MdctWindow, typeof(double) }
        };

        public static readonly object[][] AllArraySets =
        {
            new object[] {CriHcaArrays}
        };
    }
}
