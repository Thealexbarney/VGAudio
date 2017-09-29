using System.IO;

namespace VGAudio.Utilities.Riff
{
    public class WaveSmplChunk : RiffSubChunk
    {
        public int Manufacturer { get; set; }
        public int Product { get; set; }
        public int SamplePeriod { get; set; }
        public int MidiUnityNote { get; set; }
        public int MidiPitchFraction { get; set; }
        public int SmpteFormat { get; set; }
        public int SmpteOffset { get; set; }
        public int SampleLoops { get; set; }
        public int SamplerData { get; set; }
        public SampleLoop[] Loops { get; set; }

        protected WaveSmplChunk(BinaryReader reader) : base(reader)
        {
            Manufacturer = reader.ReadInt32();
            Product = reader.ReadInt32();
            SamplePeriod = reader.ReadInt32();
            MidiUnityNote = reader.ReadInt32();
            MidiPitchFraction = reader.ReadInt32();
            SmpteFormat = reader.ReadInt32();
            SmpteOffset = reader.ReadInt32();
            SampleLoops = reader.ReadInt32();
            SamplerData = reader.ReadInt32();
            Loops = new SampleLoop[SampleLoops];

            for(int i = 0; i < SampleLoops; i++)
            {
                Loops[i] = new SampleLoop
                {
                    CuePointId = reader.ReadInt32(),
                    Type = reader.ReadInt32(),
                    Start = reader.ReadInt32(),
                    End = reader.ReadInt32(),
                    Fraction = reader.ReadInt32(),
                    PlayCount = reader.ReadInt32()
                };
            }
        }

        public static WaveSmplChunk Parse(RiffParser parser, BinaryReader reader) => new WaveSmplChunk(reader);
    }

    public class SampleLoop
    {
        public int CuePointId { get; set; }
        public int Type { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public int Fraction { get; set; }
        public int PlayCount { get; set; }
    }
}
