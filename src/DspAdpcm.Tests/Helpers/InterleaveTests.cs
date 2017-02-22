using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using DspAdpcm.Utilities;

namespace DspAdpcm.Tests.Helpers
{
    public static class InterleaveTests
    {
        private static byte[] Interleaved(int count) => Enumerable.Range(0, count).Select(x => (byte)x).ToArray();

        private static byte[][] Deinterleaved(params int[] lengths) =>
            lengths.Select(Interleaved).ToArray();

        private static T[] Flatten<T>(this T[][] array) => array.SelectMany(x => x).ToArray();

        //Standard test
        private static byte[][] Deinterleaved16Size8Count2 { get; } = {
            new byte[] { 00, 01, 02, 03, 04, 05, 06, 07 },
            new byte[] { 08, 09, 10, 11, 12, 13, 14, 15 }
        };

        //Standard test
        private static byte[][] Deinterleaved16Size4Count2 { get; } = {
            new byte[] { 00, 01, 02, 03, 08, 09, 10, 11 },
            new byte[] { 04, 05, 06, 07, 12, 13, 14, 15 }
        };

        //Standard test
        private static byte[][] Deinterleaved16Size4Count4 { get; } = {
            new byte[] { 00, 01, 02, 03 },
            new byte[] { 04, 05, 06, 07 },
            new byte[] { 08, 09, 10, 11 },
            new byte[] { 12, 13, 14, 15 }
        };

        //Tests longer input than output
        private static byte[][] Deinterleaved16Size4Out4 { get; } = {
            new byte[] { 00, 01, 02, 03, 00, 00 },
            new byte[] { 04, 05, 06, 07, 00, 00 },
            new byte[] { 08, 09, 10, 11, 00, 00 },
            new byte[] { 12, 13, 14, 15, 00, 00 }
        };

        //Tests shorter input than output
        private static byte[][] Deinterleaved16Size8Length6 { get; } = {
            new byte[] { 00, 01, 02, 03, 04, 05 },
            new byte[] { 08, 09, 10, 11, 12, 13 }
        };

        private static byte[] Interleaved16Size8Length6 { get; } = {
            00, 01, 02, 03, 04, 05, 00, 00,
            08, 09, 10, 11, 12, 13, 00, 00
        };

        //Tests shortened last block
        private static byte[][] Deinterleaved15Size2Count5Length3 { get; } = {
            new byte[] { 00, 01, 10 },
            new byte[] { 02, 03, 11 },
            new byte[] { 04, 05, 12 },
            new byte[] { 06, 07, 13 },
            new byte[] { 08, 09, 14 }
        };

        private static IEnumerable<object[]> ArrayData()
        {
            return new[]
            {
                new object[] { Deinterleaved16Size8Count2, Interleaved(16), 8, -1},
                new object[] { Deinterleaved16Size4Count2, Interleaved(16), 4, -1},
                new object[] { Deinterleaved16Size4Count4, Interleaved(16), 4, -1},
                new object[] { Deinterleaved16Size4Out4, Interleaved(16), 4, 4},
                new object[] { Deinterleaved16Size8Length6, Interleaved16Size8Length6, 8, 8},
                new object[] { Deinterleaved15Size2Count5Length3, Interleaved(15), 2, -1},
                //Tests shorter output than input
                new object[] { Deinterleaved15Size2Count5Length3, Interleaved(10), 2, 2}
            };
        }

        [Theory]
        [MemberData(nameof(ArrayData))]
        private static void ArrayInterleaveTest(byte[][] input, byte[] expectedOutput, int interleaveSize, int outputSize)
        {
            byte[] output = input.Interleave(interleaveSize, outputSize);
            Assert.Equal(expectedOutput, output);
        }

        [Theory]
        [MemberData(nameof(ArrayData))]
        private static void StreamInterleaveTest(byte[][] input, byte[] expectedOutput, int interleaveSize, int outputSize)
        {
            using (var stream = new MemoryStream())
            {
                input.Interleave(stream, interleaveSize, outputSize);

                byte[] output = stream.ToArray();
                Assert.Equal(expectedOutput, output);
            }
        }

        [Fact]
        private static void FailsIfStreamTooShort()
        {
            int length = 16;
            int numInputs = 2;

            byte[][] input = Deinterleaved(Enumerable.Range(0, numInputs).Select(x => length).ToArray());

            //Fixed length memory stream
            using (var stream = new MemoryStream(new byte[length * numInputs]))
            {
                input.Interleave(stream, length, 1);
            }

            //Expandable memory stream
            using (var stream = new MemoryStream())
            {
                input.Interleave(stream, length, 1);
            }

            //Fixed length memory stream that's too short
            using (var stream = new MemoryStream(new byte[length * numInputs - 1]))
            {
                Assert.Throws<NotSupportedException>(() => input.Interleave(stream, 1));
            }
        }

        [Fact]
        private static void FailsIfArrayLengthUneven()
        {
            Deinterleaved(4, 4, 4).Interleave(1);
            Deinterleaved(8, 8, 8).Interleave(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => Deinterleaved(4, 5, 4).Interleave(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Deinterleaved(8, 4, 4).Interleave(1));
        }
    }
}
