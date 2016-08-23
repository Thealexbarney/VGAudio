using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DspAdpcm.Lib.Adpcm.Formats
{
    /// <summary>
    /// Defines the structure and metadata
    /// of a BCSTM file.
    /// </summary>
    public class BcstmStructure
    {
        /// <summary>
        /// The length of the entire BCSTM file.
        /// </summary>
        public int FileLength { get; set; }
        /// <summary>
        /// The length of the CSTM header.
        /// </summary>
        public int CstmHeaderLength { get; set; }
        /// <summary>
        /// The number of sections listed in the CSTM header.
        /// </summary>
        public int CstmHeaderSections { get; set; }
        /// <summary>
        /// The offset of the INFO chunk.
        /// </summary>
        public int InfoChunkOffset { get; set; }
        /// <summary>
        /// The length of the INFO chunk as stated in the
        /// CSTM header.
        /// </summary>
        public int InfoChunkLengthCstm { get; set; }
        /// <summary>
        /// The offset of the SEEK chunk.
        /// </summary>
        public int SeekChunkOffset { get; set; }
        /// <summary>
        /// The length of the SEEK chunk as stated in the
        /// CSTM header.
        /// </summary>
        public int SeekChunkLengthCstm { get; set; }
        /// <summary>
        /// The offset of the DATA chunk.
        /// </summary>
        public int DataChunkOffset { get; set; }
        /// <summary>
        /// The length of the DATA chunk as stated in the
        /// CSTM header.
        /// </summary>
        public int DataChunkLengthCstm { get; set; }
    }
}
