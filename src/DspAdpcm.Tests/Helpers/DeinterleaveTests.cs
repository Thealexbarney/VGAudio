using System.Collections.Generic;
using System.IO;
using Xunit;

namespace DspAdpcm.Tests.Helpers
{
    public class DeinterleaveTests
    {
        private static byte[] Interleaved1 { get; } = {
            00, 01, 02, 03, 04, 05, 06, 07,
            08, 09, 10, 11, 12, 13, 14, 15
        };

        private static byte[][] Deinterleaved1Size8Count2 { get; } = {
            new byte[] { 00, 01, 02, 03, 04, 05, 06, 07 },
            new byte[] { 08, 09, 10, 11, 12, 13, 14, 15 }
        };

        private static byte[][] Deinterleaved1Size4Count2 { get; } = {
            new byte[] { 00, 01, 02, 03, 08, 09, 10, 11 },
            new byte[] { 04, 05, 06, 07, 12, 13, 14, 15 }
        };

        private static byte[][] Deinterleaved1Size4Count4 { get; } = {
            new byte[] { 00, 01, 02, 03 },
            new byte[] { 04, 05, 06, 07 },
            new byte[] { 08, 09, 10, 11 },
            new byte[] { 12, 13, 14, 15 }
        };

        private static IEnumerable<object[]> ArrayData()
        {
            return new[]
            {
                new object[] {Interleaved1, 2, 8, Deinterleaved1Size8Count2},
                new object[] {Interleaved1, 2, 4, Deinterleaved1Size4Count2},
                new object[] {Interleaved1, 4, 4, Deinterleaved1Size4Count4},
                new object[] {Interleaved1, 1, 8, new[] {Interleaved1}}
            };
        }

        [Theory]
        [MemberData(nameof(ArrayData))]
        private static void ArrayDeInterleaveTest(byte[] input, int numInputs, int interleaveSize, byte[][] expectedOutput)
        {
            var output = input.DeInterleave(interleaveSize, numInputs);
            Assert.Equal(output, expectedOutput);
        }

        [Theory]
        [MemberData(nameof(ArrayData))]
        private static void StreamDeInterleaveTest(byte[] input, int numInputs, int interleaveSize, byte[][] expectedOutput)
        {
            using (var stream = new MemoryStream(input))
            {
                byte[][] output = stream.DeInterleave(input.Length, interleaveSize, numInputs);
                Assert.Equal(output, expectedOutput);
            }
        }
    }
}