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
        public void GeneratedDequantizerRangeTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.DequantizerRangeTable, CriHcaTablesPrebuilt.DequantizerRangeTable);
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
        public void GeneratedSinTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.SinTable, CriHcaTablesPrebuilt.SinTable);
        }

        [Fact]
        public void GeneratedCosTableMatchesOriginal()
        {
            Assert.Equal(CriHcaTables.CosTable, CriHcaTablesPrebuilt.CosTable);
        }
    }
}
