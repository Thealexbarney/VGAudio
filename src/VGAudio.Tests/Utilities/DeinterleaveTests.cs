using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using VGAudio.Utilities;

namespace VGAudio.Tests.Utilities
{
    public static class DeinterleaveTests
    {
        private static byte[] Interleaved(int count) => Enumerable.Range(0, count).Select(x => (byte)x).ToArray();

        //Standard test
        private static byte[][] Deinterleaved16Size8Count2 { get; } = {
            new byte[] { 00, 01, 02, 03, 04, 05, 06, 07 },
            new byte[] { 08, 09, 10, 11, 12, 13, 14, 15 }
        };

        //Tests truncating output
        private static byte[][] Deinterleaved16Size8Count2Length6 { get; } = {
            new byte[] { 00, 01, 02, 03, 04, 05 },
            new byte[] { 08, 09, 10, 11, 12, 13 }
        };

        //Tests truncating output to multiple blocks shorter than the input
        private static byte[][] Deinterleaved26Size4Count2Length6 { get; } = {
            new byte[] { 00, 01, 02, 03, 08, 09 },
            new byte[] { 04, 05, 06, 07, 12, 13 }
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

        //Tests shortened last block as only block
        private static byte[][] Deinterleaved16Size8Count4 => Deinterleaved16Size4Count4;

        //Tests longer output than input
        private static byte[][] Deinterleaved16Size4Count4Length6 { get; } = {
            new byte[] { 00, 01, 02, 03, 00, 00 },
            new byte[] { 04, 05, 06, 07, 00, 00 },
            new byte[] { 08, 09, 10, 11, 00, 00 },
            new byte[] { 12, 13, 14, 15, 00, 00 }
        };

        //Tests longer output than input and shortened last block
        private static byte[][] Deinterleaved12Size2Count4Length6 { get; } = {
            new byte[] { 00, 01, 08, 00, 00, 00 },
            new byte[] { 02, 03, 09, 00, 00, 00 },
            new byte[] { 04, 05, 10, 00, 00, 00 },
            new byte[] { 06, 07, 11, 00, 00, 00 }
        };

        //Tests shortened last block and truncated output
        private static byte[][] Deinterleaved15Size2Count5Length2 { get; } = {
            new byte[] { 00, 01 },
            new byte[] { 02, 03 },
            new byte[] { 04, 05 },
            new byte[] { 06, 07 },
            new byte[] { 08, 09 }
        };

        //Tests shortened last block
        private static byte[][] Deinterleaved15Size2Count5Length3 { get; } = {
            new byte[] { 00, 01, 10 },
            new byte[] { 02, 03, 11 },
            new byte[] { 04, 05, 12 },
            new byte[] { 06, 07, 13 },
            new byte[] { 08, 09, 14 }
        };

        //Tests shortened last input and output blocks and truncated output
        private static byte[][] Deinterleaved10Size2Count5Length1 { get; } = {
            new byte[] { 00 },
            new byte[] { 02 },
            new byte[] { 04 },
            new byte[] { 06 },
            new byte[] { 08 }
        };

        public static IEnumerable<object[]> ArrayData()
        {
            return new[]
            {
                new object[] {Interleaved(16), Deinterleaved16Size8Count2, 8, 2, -1 },
                new object[] {Interleaved(16), Deinterleaved16Size8Count2Length6, 8, 2, 6 },
                new object[] {Interleaved(26), Deinterleaved26Size4Count2Length6, 4, 2, 6 },
                new object[] {Interleaved(16), Deinterleaved16Size4Count2, 4, 2, -1 },
                new object[] {Interleaved(16), Deinterleaved16Size4Count4, 4, 4, -1},
                new object[] {Interleaved(16), Deinterleaved16Size8Count4, 8, 4, -1},
                new object[] {Interleaved(16), Deinterleaved16Size4Count4Length6, 4, 4, 6},
                new object[] {Interleaved(12), Deinterleaved12Size2Count4Length6, 2, 4, 6},
                new object[] {Interleaved(15), Deinterleaved15Size2Count5Length2, 2, 5, 2},
                new object[] {Interleaved(15), Deinterleaved15Size2Count5Length3, 2, 5, 3},
                new object[] {Interleaved(10), Deinterleaved10Size2Count5Length1, 2, 5, 1},
                new object[] {Interleaved(16), new[] { Interleaved(16) }, 8, 1, -1 }
            };
        }

        [Theory]
        [MemberData(nameof(ArrayData))]
        private static void ArrayDeInterleaveTest(byte[] input, byte[][] expectedOutput, int interleaveSize, int numInputs, int outputSize)
        {
            byte[][] output = input.DeInterleave(interleaveSize, numInputs, outputSize);
            Assert.Equal(expectedOutput, output);
        }

        [Theory]
        [MemberData(nameof(ArrayData))]
        private static void StreamDeInterleaveTest(byte[] input, byte[][] expectedOutput, int interleaveSize, int numInputs, int outputSize)
        {
            using (var stream = new MemoryStream(input))
            {
                byte[][] output = stream.DeInterleave(input.Length, interleaveSize, numInputs, outputSize);
                Assert.Equal(expectedOutput, output);
            }
        }

        [Fact]
        private static void FailsIfStreamTooShort()
        {
            int streamLength = 16;
            byte[] input = Interleaved(streamLength);

            using (var stream = new MemoryStream(input))
            {
                stream.DeInterleave(streamLength, 1, 1);
                stream.Position = 0;

                Assert.Throws<ArgumentOutOfRangeException>(() => stream.DeInterleave(streamLength + 1, 1, 1));
            }
        }

        [Fact]
        private static void FailsIfInterleavedLengthUnevenStream()
        {
            int numInputs = 3;
            int numBlocks = 4;
            int streamLength = numInputs * numBlocks;
            byte[] input = Interleaved(streamLength);

            using (var stream = new MemoryStream(input))
            {
                stream.DeInterleave(streamLength, 1, numInputs);
                stream.Position = 0;

                Assert.Throws<ArgumentOutOfRangeException>(() => stream.DeInterleave(streamLength - 1, 1, numInputs));
                stream.Position = 0;

                Assert.Throws<ArgumentOutOfRangeException>(() => stream.DeInterleave(streamLength - 2, 1, numInputs));
                stream.Position = 0;

                stream.DeInterleave(streamLength - 3, 1, numInputs);
            }
        }

        [Fact]
        private static void FailsIfInterleavedLengthUnevenArray()
        {
            int numInputs = 3;
            int numBlocks = 4;
            int streamLength = numInputs * numBlocks;

            Interleaved(streamLength).DeInterleave(1, numInputs);
            Assert.Throws<ArgumentOutOfRangeException>(() => Interleaved(streamLength - 1).DeInterleave(1, numInputs));
            Assert.Throws<ArgumentOutOfRangeException>(() => Interleaved(streamLength - 2).DeInterleave(1, numInputs));
            Interleaved(streamLength - 3).DeInterleave(1, numInputs);

        }
    }
}