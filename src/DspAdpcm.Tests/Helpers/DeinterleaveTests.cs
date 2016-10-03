using System.Collections.Generic;
using Xunit;

namespace DspAdpcm.Tests.Helpers
{
    public class DeinterleaveTests
    {
        private static short[] Interleaved1 { get; } = {
            00, 01, 02, 03, 04, 05, 06, 07,
            08, 09, 10, 11, 12, 13, 14, 15
        };

        private static short[][] Deinterleaved1Size8Count2 { get; } = {
            new short[] { 00, 01, 02, 03, 04, 05, 06, 07 },
            new short[] { 08, 09, 10, 11, 12, 13, 14, 15 }
        };

        private static short[][] Deinterleaved1Size4Count2 { get; } = {
            new short[] { 00, 01, 02, 03, 08, 09, 10, 11 },
            new short[] { 04, 05, 06, 07, 12, 13, 14, 15 }
        };

        private static short[][] Deinterleaved1Size4Count4 { get; } = {
            new short[] { 00, 01, 02, 03 },
            new short[] { 04, 05, 06, 07 },
            new short[] { 08, 09, 10, 11 },
            new short[] { 12, 13, 14, 15 }
        };

        private static IEnumerable<object[]> ShortArrayData()
        {
            yield return new object[] { Interleaved1, 2, 8, Deinterleaved1Size8Count2 };
            yield return new object[] { Interleaved1, 2, 4, Deinterleaved1Size4Count2 };
            yield return new object[] { Interleaved1, 4, 4, Deinterleaved1Size4Count4 };
            yield return new object[] { Interleaved1, 1, 8, new[] { Interleaved1 } };
        }

        [Theory]
        [MemberData(nameof(ShortArrayData))]
        private static void ShortArrayDeInterleaveTest(short[] input, int numInputs, int interleaveSize, short[][] expectedOutput)
        {
            var output = input.DeInterleave(interleaveSize, numInputs);
            Assert.Equal(output, expectedOutput);
        }
    }
}