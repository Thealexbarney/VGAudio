using System;
using System.IO;
using DspAdpcm.Adpcm.Formats.Internal;
using DspAdpcm.Adpcm.Formats.Structures;

namespace DspAdpcm.Adpcm.Formats
{
    /// <summary>
    /// Represents a BFSTM file.
    /// </summary>
    public class Bfstm
    {
        private BCFstm BCFstm { get; set; }

        /// <summary>
        /// The underlying <see cref="AdpcmStream"/> used to build the BFSTM file.
        /// </summary>
        public AdpcmStream AudioStream => BCFstm.AudioStream;

        /// <summary>
        /// Contains various settings used when building the BFSTM file.
        /// </summary>
        public BfstmConfiguration Configuration { get; } = new BfstmConfiguration();

        /// <summary>
        /// The size in bytes of the BFSTM file.
        /// </summary>
        public int FileLength => BCFstm.FileLength;

        /// <summary>
        /// Initializes a new <see cref="Bfstm"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Bfstm"/>.</param>
        /// <param name="configuration">A <see cref="BfstmConfiguration"/>
        /// to use for the <see cref="Bfstm"/></param>
        public Bfstm(AdpcmStream stream, BfstmConfiguration configuration = null)
        {
            if (stream.Channels.Count < 1)
            {
                throw new InvalidDataException("Stream must have at least one channel ");
            }

            BCFstm = new BCFstm { AudioStream = stream };
            Configuration = configuration ?? Configuration;
        }

        /// <summary>
        /// Initializes a new <see cref="Bfstm"/> by parsing an existing
        /// BFSTM file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BFSTM file. Must be seekable.</param>
        /// <param name="configuration">A <see cref="BfstmConfiguration"/>
        /// to use for the <see cref="Bfstm"/></param>
        public Bfstm(Stream stream, BfstmConfiguration configuration = null)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            BCFstm = new BCFstm();
            BCFstm.ReadBfstmFile(stream);
            Configuration.Configuration = BCFstm.Configuration;
            Configuration = configuration ?? Configuration;
        }

        /// <summary>
        /// Initializes a new <see cref="Bfstm"/> by parsing an existing
        /// BFSTM file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the BFSTM file.</param>
        /// <param name="configuration">A <see cref="BfstmConfiguration"/>
        /// to use for the <see cref="Bfstm"/></param>
        public Bfstm(byte[] file, BfstmConfiguration configuration = null)
        {
            using (var stream = new MemoryStream(file))
            {
                BCFstm = new BCFstm();
                BCFstm.ReadBfstmFile(stream);
                Configuration.Configuration = BCFstm.Configuration;
            }
            Configuration = configuration ?? Configuration;
        }
        
        /// <summary>
        /// Parses the header of a BFSTM file and returns the metadata
        /// and structure data of that file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BFSTM file. Must be seekable.</param>
        /// <returns>A <see cref="BfstmStructure"/> containing
        /// the data from the BFSTM header.</returns>
        public static BfstmStructure ReadMetadata(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return new BCFstm().ReadBfstmFile(stream, false);
        }

        /// <summary>
        /// Builds a BFSTM file from the current <see cref="AudioStream"/>.
        /// </summary>
        /// <returns>A BFSTM file.</returns>
        public byte[] GetFile()
        {
            var file = new byte[FileLength];
            var stream = new MemoryStream(file);
            WriteFile(stream);
            return file;
        }

        /// <summary>
        /// Writes the BFSTM file to a <see cref="Stream"/>.
        /// The file is written starting at the beginning
        /// of the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write the
        /// BFSTM file to.</param>
        public void WriteFile(Stream stream)
        {
            BCFstm.Configuration = Configuration.Configuration;
            BCFstm.WriteBCFstmFile(stream, BCFstm.BCFstmType.Bfstm);
        }
    }

    /// <summary>
    /// Contains the options used to build the BFSTM file.
    /// </summary>
    public class BfstmConfiguration
    {
        internal BCFstmConfiguration Configuration { get; set; } = new BCFstmConfiguration()
        {
            IncludeTrackInformation = false,
            InfoPart1Extra = true
        };

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.RecalculateSeekTable"/>
        /// </summary>
        public bool RecalculateSeekTable
        {
            get { return Configuration.RecalculateSeekTable; }
            set { Configuration.RecalculateSeekTable = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.RecalculateLoopContext"/>
        /// </summary>
        public bool RecalculateLoopContext
        {
            get { return Configuration.RecalculateLoopContext; }
            set { Configuration.RecalculateLoopContext = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.SamplesPerInterleave"/>
        /// </summary>
        public int SamplesPerInterleave
        {
            get { return Configuration.SamplesPerInterleave; }
            set { Configuration.SamplesPerInterleave = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.SamplesPerSeekTableEntry"/>
        /// </summary>
        public int SamplesPerSeekTableEntry
        {
            get { return Configuration.SamplesPerSeekTableEntry; }
            set { Configuration.SamplesPerSeekTableEntry = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.LoopPointAlignment"/>
        /// </summary>
        public int LoopPointAlignment
        {
            get { return Configuration.LoopPointAlignment; }
            set { Configuration.LoopPointAlignment = value; }
        }

        /// <summary>
        /// If <c>true</c>, include the loop points, before alignment,
        /// in the header of the BFSTM.
        /// </summary>
        public bool IncludeUnalignedLoopPoints
        {
            get { return Configuration.IncludeUnalignedLoopPoints; }
            set { Configuration.IncludeUnalignedLoopPoints = value; }
        }
    }
}
