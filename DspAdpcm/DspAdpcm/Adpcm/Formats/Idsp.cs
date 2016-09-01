using System;
using System.IO;
using System.Linq;
using System.Text;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Adpcm.Formats
{
    /// <summary>
    /// Represents an IDSP file.
    /// </summary>
    public class Idsp
    {
        /// <summary>
        /// The underlying <see cref="AdpcmStream"/> used to build the IDSP file.
        /// </summary>
        public AdpcmStream AudioStream { get; set; }

        /// <summary>
        /// Contains various settings used when building the IDSP file.
        /// </summary>
        public IdspConfiguration Configuration { get; } = new IdspConfiguration();

        /// <summary>
        /// The size in bytes of the IDSP file.
        /// </summary>
        public int FileLength => 0; // Placeholder

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Idsp"/>.</param>
        public Idsp(AdpcmStream stream)
        {
            if (stream.Channels.Count < 1)
            {
                throw new InvalidDataException("Stream must have at least one channel ");
            }

            AudioStream = stream;
        }

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> by parsing an existing
        /// IDSP file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the IDSP file. Must be seekable.</param>
        public Idsp(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            ReadIdspFile(stream);
        }

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> by parsing an existing
        /// IDSP file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the IDSP file.</param>
        public Idsp(byte[] file)
        {
            using (var stream = new MemoryStream(file))
            {
                ReadIdspFile(stream);
            }
        }

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Idsp"/>.</param>
        /// <param name="configuration">A <see cref="IdspConfiguration"/>
        /// to use for the <see cref="Idsp"/></param>
        public Idsp(AdpcmStream stream, IdspConfiguration configuration) : this(stream)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> by parsing an existing
        /// IDSP file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the IDSP file. Must be seekable.</param>
        /// <param name="configuration">A <see cref="IdspConfiguration"/>
        /// to use for the <see cref="Idsp"/></param>
        public Idsp(Stream stream, IdspConfiguration configuration) : this(stream)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> by parsing an existing
        /// IDSP file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the IDSP file.</param>
        /// <param name="configuration">A <see cref="IdspConfiguration"/>
        /// to use for the <see cref="Idsp"/></param>
        public Idsp(byte[] file, IdspConfiguration configuration) : this(file)
        {
            Configuration = configuration;
        }

        private Idsp() { }

        /// <summary>
        /// Parses the header of an IDSP file and returns the metadata
        /// and structure data of that file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the IDSP file. Must be seekable.</param>
        /// <returns>A <see cref="IdspStructure"/> containing
        /// the data from the IDSP header.</returns>
        public static IdspStructure ReadMetadata(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return new Idsp().ReadIdspFile(stream, false);
        }

        /// <summary>
        /// Builds an IDSP file from the current <see cref="AudioStream"/>.
        /// </summary>
        /// <returns>An IDSP file</returns>
        public byte[] GetFile()
        {
            var file = new byte[FileLength];
            var stream = new MemoryStream(file);
            WriteFile(stream);
            return file;
        }

        /// <summary>
        /// Writes the IDSP file to a <see cref="Stream"/>.
        /// The file is written starting at the beginning
        /// of the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write the
        /// IDSP to.</param>
        public void WriteFile(Stream stream)
        {
            if (stream.Length != FileLength)
            {
                try
                {
                    stream.SetLength(FileLength);
                }
                catch (NotSupportedException ex)
                {
                    throw new ArgumentException("Stream is too small.", nameof(stream), ex);
                }
            }
        }

        private IdspStructure ReadIdspFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = new BinaryReaderBE(stream, Encoding.UTF8, true))
            {
                if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "IDSP")
                {
                    throw new InvalidDataException("File has no IDSP header");
                }

                var structure = new IdspStructure();

                ParseIdspHeader(reader, structure);
                if (readAudioData)
                {
                    ParseIdspData(reader, structure);
                    SetProperties(structure);
                }

                return structure;
            }
        }

        private void SetProperties(IdspStructure structure)
        {
            //Todo: set configuration

            AudioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
            if (structure.Looping)
            {
                AudioStream.SetLoop(structure.LoopStart, structure.NumSamples);
            }

            for (int c = 0; c < structure.NumChannels; c++)
            {
                var channel = new AdpcmChannel(structure.NumSamples, structure.AudioData[c])
                {
                    Coefs = structure.Channels[c].Coefs,
                    Gain = structure.Channels[c].Gain,
                    Hist1 = structure.Channels[c].Hist1,
                    Hist2 = structure.Channels[c].Hist2,
                };
                channel.SetLoopContext(structure.Channels[c].LoopPredScale, structure.Channels[c].LoopHist1,
                    structure.Channels[c].LoopHist2);
                AudioStream.Channels.Add(channel);
            }
        }

        private static void ParseIdspHeader(BinaryReader reader, IdspStructure structure)
        {
            reader.BaseStream.Position += 4;
            structure.NumChannels = reader.ReadInt32();
            structure.SampleRate = reader.ReadInt32();
            structure.NumSamples = reader.ReadInt32();
            structure.LoopStart = reader.ReadInt32();
            structure.LoopEnd = reader.ReadInt32();
            structure.InterleaveSize = reader.ReadInt32();
            structure.HeaderSize = reader.ReadInt32();
            structure.ChannelInfoSize = reader.ReadInt32();
            structure.AudioDataOffset = reader.ReadInt32();
            structure.ChannelSize = reader.ReadInt32();

            for (int i = 0; i < structure.NumChannels; i++)
            {
                reader.BaseStream.Position = structure.HeaderSize + i * structure.ChannelInfoSize;
                var channel = new IDSPChannelInfo();
                channel.NumSamples = reader.ReadInt32();
                channel.NumNibbles = reader.ReadInt32();
                channel.SampleRate = reader.ReadInt32();
                channel.Looping = reader.ReadInt16() == 1;
                reader.BaseStream.Position += 2;
                channel.StartLoop = reader.ReadInt32();
                channel.EndLoop = reader.ReadInt32();
                channel.Unknown = reader.ReadInt32();
                channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray();
                channel.Gain = reader.ReadInt16();
                channel.PredScale = reader.ReadInt16();
                channel.Hist1 = reader.ReadInt16();
                channel.Hist2 = reader.ReadInt16();
                channel.LoopPredScale = reader.ReadInt16();
                channel.LoopHist1 = reader.ReadInt16();
                channel.LoopHist2 = reader.ReadInt16();

                structure.Channels.Add(channel);
            }

            //There isn't a loop flag for the file in general, so we set it
            //if any of the single channels loop.
            structure.Looping = structure.Channels.Any(x => x.Looping);
        }

        private static void ParseIdspData(BinaryReader reader, IdspStructure structure)
        {
            reader.BaseStream.Position = structure.AudioDataOffset;
            int interleave = structure.InterleaveSize == 0 ? structure.ChannelSize : structure.InterleaveSize;
            //If the file isn't interleaved, there is no padding/alignment at the break between channels.
            int length = GetNextMultiple(structure.ChannelSize, interleave) * structure.NumChannels;
            structure.AudioData = reader.BaseStream.DeInterleave(length, interleave,
                structure.NumChannels);
        }

        public class IdspConfiguration
        {

        }
    }
}
