using System.IO;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class PrefetchData
    {
        public int StartSample { get; private set; }
        public int Size { get; private set; }
        public int SampleCount { get; private set; }
        public Reference AudioData { get; private set; }

        public static PrefetchData ReadPrefetchData(BinaryReader reader, StreamInfo info)
        {
            int baseOffset = (int)reader.BaseStream.Position;
            var pdat = new PrefetchData();

            pdat.StartSample = reader.ReadInt32();
            pdat.Size = reader.ReadInt32();
            pdat.SampleCount = Common.BytesToSamples(pdat.Size / info.ChannelCount, info.Codec);
            reader.ReadInt32(); //Padding
            pdat.AudioData = new Reference(reader, baseOffset);
            return pdat;
        }
    }
}
