using System.Collections.Generic;
using VGAudio.Codecs.Atrac9;
using Xunit;
using static VGAudio.Codecs.Atrac9.Tables;
using static VGAudio.Tests.Formats.Atrac9.HuffmanCodebooks;

namespace VGAudio.Tests.Formats.Atrac9
{
    public class HuffmanCodebookTests
    {
        [Theory]
        [MemberData(nameof(GetScaleFactorsSetA))]
        public void UnpackedScaleFactorABitsAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Bits, actual.Bits);
        }

        [Theory]
        [MemberData(nameof(GetScaleFactorsSetB))]
        public void UnpackedScaleFactorBBitsAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Bits, actual.Bits);
        }

        [Theory]
        [MemberData(nameof(GetSpectrumSetA))]
        public void UnpackedSpectrumABitsAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Bits, actual.Bits);
        }

        [Theory]
        [MemberData(nameof(GetSpectrumSetB))]
        public void UnpackedSpectrumBBitsAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Bits, actual.Bits);
        }

        [Theory]
        [MemberData(nameof(GetScaleFactorsSetA))]
        public void UnpackedScaleFactorACodesAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Codes, actual.Codes);
        }

        [Theory]
        [MemberData(nameof(GetScaleFactorsSetB))]
        public void UnpackedScaleFactorBCodesAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Codes, actual.Codes);
        }

        [Theory]
        [MemberData(nameof(GetSpectrumSetA))]
        public void UnpackedSpectrumACodesAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Codes, actual.Codes);
        }

        [Theory]
        [MemberData(nameof(GetSpectrumSetB))]
        public void UnpackedSpectrumBCodesAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Codes, actual.Codes);
        }

        [Theory]
        [MemberData(nameof(GetScaleFactorsSetA))]
        public void GeneratedScaleFactorALookupAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Lookup, actual.Lookup);
        }

        [Theory]
        [MemberData(nameof(GetScaleFactorsSetB))]
        public void GeneratedScaleFactorBLookupAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Lookup, actual.Lookup);
        }

        [Theory]
        [MemberData(nameof(GetSpectrumSetA))]
        public void GeneratedSpectrumALookupAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Lookup, actual.Lookup);
        }

        [Theory]
        [MemberData(nameof(GetSpectrumSetB))]
        public void GeneratedSpectrumBLookupAreCorrect(HuffmanCodeTables expected, HuffmanCodebook actual)
        {
            Assert.Equal(expected.Lookup, actual.Lookup);
        }

        public static IEnumerable<object[]> GetSpectrumSetA()
        {
            for (int i = 2; i < 8; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    yield return new object[] { SpectrumSetA[i][j], HuffmanSpectrumA[i][j] };
                }
            }
        }

        public static IEnumerable<object[]> GetSpectrumSetB()
        {
            for (int i = 2; i < 8; i++)
            {
                for (int j = 1; j < 4; j++)
                {
                    yield return new object[] { SpectrumSetB[i][j], HuffmanSpectrumB[i][j] };
                }
            }
        }

        public static IEnumerable<object[]> GetScaleFactorsSetA()
        {
            for (int i = 1; i < 7; i++)
            {
                yield return new object[] { ScaleFactorsSetA[i], HuffmanScaleFactorsUnsigned[i] };
            }
        }

        public static IEnumerable<object[]> GetScaleFactorsSetB()
        {
            for (int i = 2; i < 6; i++)
            {
                yield return new object[] { ScaleFactorsSetB[i], HuffmanScaleFactorsSigned[i] };
            }
        }

        private static readonly HuffmanCodeTables[] ScaleFactorsSetA =
        {
            null,
            new HuffmanCodeTables(ScaleFactorsA1Bits, ScaleFactorsA1Codes, ScaleFactorsA1Lookup),
            new HuffmanCodeTables(ScaleFactorsA2Bits, ScaleFactorsA2Codes, ScaleFactorsA2Lookup),
            new HuffmanCodeTables(ScaleFactorsA3Bits, ScaleFactorsA3Codes, ScaleFactorsA3Lookup),
            new HuffmanCodeTables(ScaleFactorsA4Bits, ScaleFactorsA4Codes, ScaleFactorsA4Lookup),
            new HuffmanCodeTables(ScaleFactorsA5Bits, ScaleFactorsA5Codes, ScaleFactorsA5Lookup),
            new HuffmanCodeTables(ScaleFactorsA6Bits, ScaleFactorsA6Codes, ScaleFactorsA6Lookup)
        };

        private static readonly HuffmanCodeTables[] ScaleFactorsSetB =
        {
            null,
            null,
            new HuffmanCodeTables(ScaleFactorsB2Bits, ScaleFactorsB2Codes, ScaleFactorsB2Lookup),
            new HuffmanCodeTables(ScaleFactorsB3Bits, ScaleFactorsB3Codes, ScaleFactorsB3Lookup),
            new HuffmanCodeTables(ScaleFactorsB4Bits, ScaleFactorsB4Codes, ScaleFactorsB4Lookup),
            new HuffmanCodeTables(ScaleFactorsB5Bits, ScaleFactorsB5Codes, ScaleFactorsB5Lookup)
        };

        private static readonly HuffmanCodeTables[][] SpectrumSetA =
        {
            null,
            null,
            new[]
            {
                new HuffmanCodeTables(SpectrumA21Bits, SpectrumA21Codes, SpectrumA21Lookup),
                new HuffmanCodeTables(SpectrumA22Bits, SpectrumA22Codes, SpectrumA22Lookup),
                new HuffmanCodeTables(SpectrumA23Bits, SpectrumA23Codes, SpectrumA23Lookup),
                new HuffmanCodeTables(SpectrumA24Bits, SpectrumA24Codes, SpectrumA24Lookup)
            },
            new[]
            {
                new HuffmanCodeTables(SpectrumA31Bits, SpectrumA31Codes, SpectrumA31Lookup),
                new HuffmanCodeTables(SpectrumA32Bits, SpectrumA32Codes, SpectrumA32Lookup),
                new HuffmanCodeTables(SpectrumA33Bits, SpectrumA33Codes, SpectrumA33Lookup),
                new HuffmanCodeTables(SpectrumA34Bits, SpectrumA34Codes, SpectrumA34Lookup)
            },
            new[]
            {
                new HuffmanCodeTables(SpectrumA41Bits, SpectrumA41Codes, SpectrumA41Lookup),
                new HuffmanCodeTables(SpectrumA42Bits, SpectrumA42Codes, SpectrumA42Lookup),
                new HuffmanCodeTables(SpectrumA43Bits, SpectrumA43Codes, SpectrumA43Lookup),
                new HuffmanCodeTables(SpectrumA44Bits, SpectrumA44Codes, SpectrumA44Lookup)
            },
            new[]
            {
                new HuffmanCodeTables(SpectrumA51Bits, SpectrumA51Codes, SpectrumA51Lookup),
                new HuffmanCodeTables(SpectrumA52Bits, SpectrumA52Codes, SpectrumA52Lookup),
                new HuffmanCodeTables(SpectrumA53Bits, SpectrumA53Codes, SpectrumA53Lookup),
                new HuffmanCodeTables(SpectrumA54Bits, SpectrumA54Codes, SpectrumA54Lookup)
            },
            new[]
            {
                new HuffmanCodeTables(SpectrumA61Bits, SpectrumA61Codes, SpectrumA61Lookup),
                new HuffmanCodeTables(SpectrumA62Bits, SpectrumA62Codes, SpectrumA62Lookup),
                new HuffmanCodeTables(SpectrumA63Bits, SpectrumA63Codes, SpectrumA63Lookup),
                new HuffmanCodeTables(SpectrumA64Bits, SpectrumA64Codes, SpectrumA64Lookup)
            },
            new[]
            {
                new HuffmanCodeTables(SpectrumA71Bits, SpectrumA71Codes, SpectrumA71Lookup),
                new HuffmanCodeTables(SpectrumA72Bits, SpectrumA72Codes, SpectrumA72Lookup),
                new HuffmanCodeTables(SpectrumA73Bits, SpectrumA73Codes, SpectrumA73Lookup),
                new HuffmanCodeTables(SpectrumA74Bits, SpectrumA74Codes, SpectrumA74Lookup)
            }
        };

        private static readonly HuffmanCodeTables[][] SpectrumSetB =
        {
            null,
            null,
            new[]
            {
                null,
                new HuffmanCodeTables(SpectrumB22Bits, SpectrumB22Codes, SpectrumB22Lookup),
                new HuffmanCodeTables(SpectrumB23Bits, SpectrumB23Codes, SpectrumB23Lookup),
                new HuffmanCodeTables(SpectrumB24Bits, SpectrumB24Codes, SpectrumB24Lookup)
            },
            new[]
            {
                null,
                new HuffmanCodeTables(SpectrumB32Bits, SpectrumB32Codes, SpectrumB32Lookup),
                new HuffmanCodeTables(SpectrumB33Bits, SpectrumB33Codes, SpectrumB33Lookup),
                new HuffmanCodeTables(SpectrumB34Bits, SpectrumB34Codes, SpectrumB34Lookup)
            },
            new[]
            {
                null,
                new HuffmanCodeTables(SpectrumB42Bits, SpectrumB42Codes, SpectrumB42Lookup),
                new HuffmanCodeTables(SpectrumB43Bits, SpectrumB43Codes, SpectrumB43Lookup),
                new HuffmanCodeTables(SpectrumB44Bits, SpectrumB44Codes, SpectrumB44Lookup)
            },
            new[]
            {
                null,
                new HuffmanCodeTables(SpectrumB52Bits, SpectrumB52Codes, SpectrumB52Lookup),
                new HuffmanCodeTables(SpectrumB53Bits, SpectrumB53Codes, SpectrumB53Lookup),
                new HuffmanCodeTables(SpectrumB54Bits, SpectrumB54Codes, SpectrumB54Lookup)
            },
            new[]
            {
                null,
                new HuffmanCodeTables(SpectrumB62Bits, SpectrumB62Codes, SpectrumB62Lookup),
                new HuffmanCodeTables(SpectrumB63Bits, SpectrumB63Codes, SpectrumB63Lookup),
                new HuffmanCodeTables(SpectrumB64Bits, SpectrumB64Codes, SpectrumB64Lookup)
            },
            new[]
            {
                null,
                new HuffmanCodeTables(SpectrumB72Bits, SpectrumB72Codes, SpectrumB72Lookup),
                new HuffmanCodeTables(SpectrumB73Bits, SpectrumB73Codes, SpectrumB73Lookup),
                new HuffmanCodeTables(SpectrumB74Bits, SpectrumB74Codes, SpectrumB74Lookup)
            }
        };

        public class HuffmanCodeTables
        {
            public HuffmanCodeTables(byte[] bits, short[] codes, byte[] lookup)
            {
                Bits = bits;
                Codes = codes;
                Lookup = lookup;
            }

            public byte[] Bits { get; }
            public short[] Codes { get; }
            public byte[] Lookup { get; }
        }
    }
}
