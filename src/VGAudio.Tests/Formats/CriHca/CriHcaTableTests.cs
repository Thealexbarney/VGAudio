using VGAudio.Codecs.CriHca;
using Xunit;

namespace VGAudio.Tests.Formats.CriHca
{
    public class CriHcaTableTests
    {
        [Fact]
        public void GeneratedDequantizerScalingTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.DequantizerScalingTable, GeneratedTables.DequantizerScalingTable);
        }

        [Fact]
        public void GeneratedDequantizerNormalizeTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.DequantizerNormalizeTable, GeneratedTables.DequantizerNormalizeTable);
        }

        [Fact]
        public void GeneratedIntensityRatioTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.IntensityRatioTable, GeneratedTables.IntensityRatioTable);
        }

        [Fact]
        public void GeneratedScaleConversionTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.ScaleConversionTable, GeneratedTables.ScaleConversionTable);
        }

        [Fact]
        public void GeneratedResolutionLevelsTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.ResolutionLevelsTable, GeneratedTables.ResolutionLevels);
        }

        [Fact]
        public void UnpackedQuantizedSpectrumMaxBitsMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.QuantizedSpectrumMaxBits, UnpackedTables.QuantizedSpectrumMaxBits);
        }

        [Fact]
        public void UnpackedQuantizedSpectrumBitsMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.QuantizedSpectrumBits, UnpackedTables.QuantizedSpectrumBits);
        }

        [Fact]
        public void UnpackedQuantizedSpectrumValueMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.QuantizedSpectrumValue, UnpackedTables.QuantizedSpectrumValue);
        }

        [Fact]
        public void UnpackedQuantizeSpectrumBitsMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.QuantizeSpectrumBits, UnpackedTables.QuantizeSpectrumBits);
        }

        [Fact]
        public void UnpackedQuantizeSpectrumValueMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.QuantizeSpectrumValue, UnpackedTables.QuantizeSpectrumValue);
        }

        [Fact]
        public void UnpackedScaleToResolutionCurveMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.ScaleToResolutionCurve, UnpackedTables.ScaleToResolutionCurve);
        }

        [Fact]
        public void UnpackedAthCurveMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.AthCurve, UnpackedTables.AthCurve);
        }

        [Fact]
        public void UnpackedMdctWindowMatchesOriginal()
        {
           Assert.Equal(CriHcaTables.MdctWindow, UnpackedTables.MdctWindow);
        }
    }
}
