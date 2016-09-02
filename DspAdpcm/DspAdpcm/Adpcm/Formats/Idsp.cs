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

        private int NumSamples => (Configuration.TrimFile && AudioStream.Looping ? AudioStream.LoopEnd : AudioStream.NumSamples) + AlignmentSamples;
        private int NumChannels => AudioStream.Channels.Count;

        private int AlignmentSamples => GetNextMultiple(AudioStream.LoopStart, Configuration.LoopPointAlignment) - AudioStream.LoopStart;
        private int LoopStart => AudioStream.LoopStart + AlignmentSamples;
        private int LoopEnd => AudioStream.LoopEnd + AlignmentSamples;
        private short Looping => (short)(AudioStream.Looping ? 1 : 0);

        private int StartAddr => GetNibbleAddress(AudioStream.Looping ? LoopStart : 0);
        private int EndAddr => GetNibbleAddress(AudioStream.Looping ? LoopEnd : NumSamples - 1);
        private static int CurAddr => GetNibbleAddress(0);

        private int InterleaveSize => Configuration.BytesPerInterleave == 0 ?
            AudioDataLength : Configuration.BytesPerInterleave;
        private int StreamInfoSize => 0x40;
        private int ChannelInfoSize => 0x60;
        private int HeaderLength => StreamInfoSize + NumChannels * ChannelInfoSize;

        private int AudioDataLength => GetNextMultiple(GetBytesForAdpcmSamples(NumSamples),
            Configuration.BytesPerInterleave == 0 ? BytesPerBlock : InterleaveSize);

        /// <summary>
        /// The size in bytes of the IDSP file.
        /// </summary>
        public int FileLength => HeaderLength + AudioDataLength * NumChannels;

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

        private void RecalculateData()
        {
            var loopContextToCalculate = Configuration.RecalculateLoopContext
                ? AudioStream.Channels.Where(x => !x.SelfCalculatedLoopContext)
                : AudioStream.Channels.Where(x => !x.LoopContextCalculated);

            if (AudioStream.Looping)
            {
                Decode.CalculateLoopAlignment(AudioStream.Channels, Configuration.LoopPointAlignment,
                    AudioStream.LoopStart, AudioStream.LoopEnd);
            }
            Decode.CalculateLoopContext(loopContextToCalculate, AudioStream.Looping ? LoopStart : 0);
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

            RecalculateData();

            using (BinaryWriter writer = new BinaryWriterBE(stream, Encoding.UTF8, true))
            {
                stream.Position = 0;
                GetHeader(writer);
                GetData(writer);
            }
        }

        private void GetHeader(BinaryWriter writer)
        {
            writer.WriteUTF8("IDSP");
            writer.Write(0);
            writer.Write(NumChannels);
            writer.Write(AudioStream.SampleRate);
            writer.Write(NumSamples);
            writer.Write(LoopStart);
            writer.Write(LoopEnd);
            writer.Write(Configuration.BytesPerInterleave);
            writer.Write(StreamInfoSize);
            writer.Write(ChannelInfoSize);
            writer.Write(HeaderLength);
            writer.Write(AudioDataLength);
            
            for (int i = 0; i < NumChannels; i++)
            {
                writer.BaseStream.Position = StreamInfoSize + i * ChannelInfoSize;
                var channel = AudioStream.Channels[i];

                writer.Write(channel.NumSamples);
                writer.Write(channel.NumNibbles);
                writer.Write(AudioStream.SampleRate);
                writer.Write(Looping);
                writer.Write((short)0);
                writer.Write(StartAddr);
                writer.Write(EndAddr);
                writer.Write(CurAddr);
                writer.Write(channel.Coefs.ToByteArray(Endianness.BigEndian));
                writer.Write(channel.Gain);
                writer.Write(channel.PredScale);
                writer.Write(channel.Hist1);
                writer.Write(channel.Hist2);
                writer.Write(channel.LoopPredScale);
                writer.Write(channel.LoopHist1);
                writer.Write(channel.LoopHist2);
            }
        }

        private void GetData(BinaryWriter writer)
        {
            writer.BaseStream.Position = HeaderLength;

            byte[][] channels = AudioStream.Channels.Select(x => x.GetAudioData).ToArray();
            channels.Interleave(writer.BaseStream, GetBytesForAdpcmSamples(NumSamples), InterleaveSize, InterleaveSize);
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
            Configuration.BytesPerInterleave = structure.InterleaveSize;

            AudioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
            if (structure.Looping)
            {
                AudioStream.SetLoop(structure.LoopStart, structure.LoopEnd);
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
            structure.AudioDataLength = reader.ReadInt32();

            for (int i = 0; i < structure.NumChannels; i++)
            {
                reader.BaseStream.Position = structure.HeaderSize + i * structure.ChannelInfoSize;
                var channel = new IdspChannelInfo();
                channel.NumSamples = reader.ReadInt32();
                channel.NumNibbles = reader.ReadInt32();
                channel.SampleRate = reader.ReadInt32();
                channel.Looping = reader.ReadInt16() == 1;
                reader.BaseStream.Position += 2;
                channel.StartAddress = reader.ReadInt32();
                channel.EndAddress = reader.ReadInt32();
                channel.CurrentAddress = reader.ReadInt32();
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
            int interleave = structure.InterleaveSize == 0 ? structure.AudioDataLength : structure.InterleaveSize;
            //If the file isn't interleaved, there is no padding/alignment at the break between channels.
            structure.AudioData = reader.BaseStream.DeInterleave(structure.NumChannels * structure.AudioDataLength, interleave,
                structure.NumChannels);
        }

        /// <summary>
        /// Contains the options used to build the IDSP file.
        /// </summary>
        public class IdspConfiguration
        {
            private int _bytesPerInterleave = BytesPerBlock * 2;

            /// <summary>
            /// If <c>true</c>, recalculates the loop context when building the file.
            /// If <c>false</c>, reuses the loop context read from the imported file,
            /// if available.
            /// Default is <c>true</c>.
            /// </summary>
            public bool RecalculateLoopContext { get; set; } = true;

            /// <summary>
            /// If <c>true</c>, trims the output file length to the set end loop point.
            /// If <c>false</c> or if the <see cref="Idsp"/> does not loop,
            /// the output file is not trimmed.
            /// if available.
            /// Default is <c>true</c>.
            /// </summary>
            public bool TrimFile { get; set; } = true;

            /// <summary>
            /// When building the audio file, the loop points and audio will
            /// be adjusted so that the start loop point is a multiple of
            /// this number. Default is 28.
            /// </summary>
            public int LoopPointAlignment { get; set; } = SamplesPerBlock * 2;

            /// <summary>
            /// The number of bytes in each block when interleaving
            /// the audio data.
            /// Must be divisible by 8.
            /// Default is 16.
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if value is negative 
            /// or not divisible by 8.</exception>
            public int BytesPerInterleave
            {
                get { return _bytesPerInterleave; }
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value,
                            "Number of samples per interleave must be non-negative");
                    }
                    if (value % BytesPerBlock != 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value,
                            "Number of samples per interleave must be divisible by 14");
                    }
                    _bytesPerInterleave = value;
                }
            }
        }
    }
}
