using VGAudio.Codecs.CriHca;
using Xunit;

namespace VGAudio.Tests.Formats.CriHca
{
    public class CriHcaTablesTests
    {
        [Fact]
        public void GeneratedDequantizerScalingTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.DequantizerScalingTable, CriHcaTablesPrebuilt.DequantizerScalingTable);
        }

        [Fact]
        public void GeneratedDequantizerNormalizeTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.DequantizerNormalizeTable, CriHcaTablesPrebuilt.DequantizerNormalizeTable);
        }

        [Fact]
        public void GeneratedIntensityRatioTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.IntensityRatioTable, CriHcaTablesPrebuilt.IntensityRatioTable);
        }

        [Fact]
        public void GeneratedScaleConversionTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.ScaleConversionTable, CriHcaTablesPrebuilt.ScaleConversionTable);
        }

        [Fact]
        public void GeneratedResolutionLevelsTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.ResolutionLevelsTable, CriHcaTablesPrebuilt.ResolutionLevels);
        }

        [Fact]
        public void UnpackedQuantizedSpectrumMaxBitsMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.QuantizedSpectrumMaxBits, CriHcaTablesPrebuilt.QuantizedSpectrumMaxBits);
        }

        [Fact]
        public void UnpackedQuantizedSpectrumBitsMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.QuantizedSpectrumBits, CriHcaTablesPrebuilt.QuantizedSpectrumBits);
        }

        [Fact]
        public void UnpackedQuantizedSpectrumValueMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.QuantizedSpectrumValue, CriHcaTablesPrebuilt.QuantizedSpectrumValue);
        }

        [Fact]
        public void UnpackedQuantizeSpectrumBitsMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.QuantizeSpectrumBits, CriHcaTablesPrebuilt.QuantizeSpectrumBits);
        }

        [Fact]
        public void UnpackedQuantizeSpectrumValueMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.QuantizeSpectrumValue, CriHcaTablesPrebuilt.QuantizeSpectrumValue);
        }

        [Fact]
        public void UnpackedScaleToResolutionCurveMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.ScaleToResolutionCurve, CriHcaTablesPrebuilt.ScaleToResolutionCurve);
        }

        [Fact]
        public void UnpackedAthCurveMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.AthCurve, CriHcaTablesPrebuilt.AthCurve);
        }

        [Fact]
        public void UnpackedMdctWindowMatchesOriginal()
        {
           Assert.Equal(CriHcaTables.MdctWindow, CriHcaTablesPrebuilt.MdctWindow);
        }
    }
}
