using System.Collections.Generic;
using VGAudio.Containers.Bxstm.Structures;

namespace VGAudio.Containers.Bxstm
{
    /// <summary>
    /// Defines the structure and metadata
    /// of a BCSTM or BFSTM file.
    /// </summary>
    public abstract class BCFstmStructure : BxstmStructure
    {
        /// <summary>The audio regions in the file.</summary>
        public List<RegionInfo> Regions { get; set; }
    }
}