using System.Collections.Generic;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class RegionInfo
    {
        public int StartSample { get; set; }
        public int EndSample { get; set; }
        public List<GcAdpcmContext> Channels { get; set; } = new List<GcAdpcmContext>();
    }
}
