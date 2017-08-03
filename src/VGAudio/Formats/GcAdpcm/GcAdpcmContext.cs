using System.IO;

namespace VGAudio.Formats.GcAdpcm
{
    /// <summary>
    /// Contains the ADPCM coder context for an ADPCM sample.
    /// </summary>
    public class GcAdpcmContext
    {
        /// <summary>The predictor and scale for the current sample's frame.</summary>
        public short PredScale { get; }

        /// <summary>The first PCM history sample. (Current sample - 1).</summary>
        public short Hist1 { get; }

        /// <summary>The second PCM history sample. (Current sample - 2).</summary>
        public short Hist2 { get; }

        public GcAdpcmContext(short predScale, short hist1, short hist2)
        {
            PredScale = predScale;
            Hist1 = hist1;
            Hist2 = hist2;
        }

        /// <summary>
        /// Creates a <see cref="GcAdpcmContext"/> by reading the values from a <see cref="BinaryReader"/>.
        /// 3 <see langword="short"/> values are read. <see cref="PredScale"/>, <see cref="Hist1"/>,
        /// and <see cref="Hist2"/> respectively.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> to be read from. Must be set
        /// at the position to be read from.</param>
        public GcAdpcmContext(BinaryReader reader)
        {
            PredScale = reader.ReadInt16();
            Hist1 = reader.ReadInt16();
            Hist2 = reader.ReadInt16();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(PredScale);
            writer.Write(Hist1);
            writer.Write(Hist2);
        }
    }
}
