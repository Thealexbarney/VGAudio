using System.IO;
using System.Linq;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class RwavChannelInfo
    {
        public int AudioDataOffset { get; set; }
        public int AdpcmInfoOffset { get; set; }
        public int VolumeFrontRight { get; set; }
        public int VolumeFrontLeft { get; set; }
        public int VolumeBackRight { get; set; }
        public int VolumeBackLeft { get; set; }
        public GcAdpcmChannelInfo AdpcmInfo { get; set; }

        public static RwavChannelInfo Read(BinaryReader reader, int baseOffset)
        {
            var info = new RwavChannelInfo
            {
                AudioDataOffset = reader.ReadInt32(),
                AdpcmInfoOffset = reader.ReadInt32(),
                VolumeFrontRight = reader.ReadInt32(),
                VolumeFrontLeft = reader.ReadInt32(),
                VolumeBackRight = reader.ReadInt32(),
                VolumeBackLeft = reader.ReadInt32()
            };

            reader.BaseStream.Position = baseOffset + info.AdpcmInfoOffset;
            var channel = new GcAdpcmChannelInfo
            {
                Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray(),
                Gain = reader.ReadInt16(),
                Start = new GcAdpcmContext(reader),
                Loop = new GcAdpcmContext(reader)
            };

            info.AdpcmInfo = channel;

            return info;
        }
    }
}
