using System;
using System.IO;
using System.Linq;
using System.Text;
using DspAdpcm.Adpcm.Formats.Configuration;
using DspAdpcm.Adpcm.Formats.Structures;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Adpcm.Formats
{
    /// <summary>
    /// Represents a DSP file.
    /// </summary>
    public class Dsp
    {
        /// <summary>
        /// The underlying <see cref="AdpcmStream"/> used to build the DSP file.
        /// </summary>
        public AdpcmStream AudioStream { get; set; }

        /// <summary>
        /// Contains various settings used when building the BRSTM file.
        /// </summary>
        public DspConfiguration Configuration { get; }

        /// <summary>
        /// The size in bytes of the DSP file.
        /// </summary>
        public int FileLength => (HeaderSize + AudioDataLength) * NumChannels;

        private const int HeaderSize = 0x60;
        private int NumChannels => AudioStream.Channels.Count;

        private int NumSamples => (Configuration.TrimFile && AudioStream.Looping ? LoopEnd :
            Math.Max(AudioStream.NumSamples, LoopEnd));
        private short Format { get; } = 0; /* 0 for ADPCM */

        private int SamplesPerInterleave => Configuration.SamplesPerInterleave;
        private int BytesPerInterleave => GetBytesForAdpcmSamples(SamplesPerInterleave);
        private int FramesPerInterleave => BytesPerInterleave / BytesPerBlock;

        private int AlignmentSamples => GetNextMultiple(AudioStream.LoopStart, Configuration.LoopPointAlignment) - AudioStream.LoopStart;
        private int LoopStart => AudioStream.LoopStart + AlignmentSamples;
        private int LoopEnd => AudioStream.LoopEnd + AlignmentSamples;

        private int StartAddr => GetNibbleAddress(AudioStream.Looping ? LoopStart : 0);
        private int EndAddr => GetNibbleAddress(AudioStream.Looping ? LoopEnd : NumSamples - 1);
        private static int CurAddr => GetNibbleAddress(0);

        private int AudioDataLength
            => GetNextMultiple(GetBytesForAdpcmSamples(NumSamples), NumChannels == 1 ? 1 : BytesPerBlock);

        /// <summary>
        /// Initializes a new <see cref="Dsp"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Dsp"/>.</param>
        /// <param name="configuration">A <see cref="DspConfiguration"/>
        /// to use for the <see cref="Dsp"/></param>
        public Dsp(AdpcmStream stream, DspConfiguration configuration = null)
        {
            if (stream.Channels.Count < 1)
            {
                throw new InvalidDataException("Stream must have at least one channel ");
            }

            AudioStream = stream;
            Configuration = configuration ?? new DspConfiguration();
        }

        /// <summary>
        /// Initializes a new <see cref="Dsp"/> by parsing an existing
        /// DSP file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the DSP file. Must be seekable.</param>
        /// <param name="configuration">A <see cref="DspConfiguration"/>
        /// to use for the <see cref="Dsp"/></param>
        public Dsp(Stream stream, DspConfiguration configuration = null)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            DspStructure dsp = ReadDspFile(stream);
            AudioStream = GetAdpcmStream(dsp);
            Configuration = configuration ?? new DspConfiguration();
        }

        /// <summary>
        /// Initializes a new <see cref="Dsp"/> by parsing an existing
        /// DSP file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the DSP file.</param>
        /// <param name="configuration">A <see cref="DspConfiguration"/>
        /// to use for the <see cref="Dsp"/></param>
        public Dsp(byte[] file, DspConfiguration configuration = null)
        {
            using (var stream = new MemoryStream(file))
            {
                DspStructure dsp = ReadDspFile(stream);
                AudioStream = GetAdpcmStream(dsp);
            }
            Configuration = configuration ?? new DspConfiguration();
        }

        /// <summary>
        /// Parses the header of a DSP file and returns the metadata
        /// and structure data of that file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the DSP file. Must be seekable.</param>
        /// <returns>A <see cref="DspStructure"/> containing
        /// the data from the DSP header.</returns>
        public static DspStructure ReadMetadata(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return ReadDspFile(stream, false);
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
                Decode.CalculateLoopContext(loopContextToCalculate, AudioStream.Looping ? LoopStart : 0);
            }
        }

        /// <summary>
        /// Builds a DSP file from the current <see cref="AudioStream"/>.
        /// </summary>
        /// <returns>A DSP file</returns>
        public byte[] GetFile()
        {
            var file = new byte[FileLength];
            var stream = new MemoryStream(file);
            WriteFile(stream);
            return file;
        }

        /// <summary>
        /// Writes the DSP file to a <see cref="Stream"/>.
        /// The file is written starting at the beginning
        /// of the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write the
        /// DSP to.</param>
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
            for (int i = 0; i < NumChannels; i++)
            {
                var channel = AudioStream.Channels[i];
                writer.BaseStream.Position = HeaderSize * i;
                writer.Write(NumSamples);
                writer.Write(GetNibbleFromSample(NumSamples));
                writer.Write(AudioStream.SampleRate);
                writer.Write((short)(AudioStream.Looping ? 1 : 0));
                writer.Write(Format);
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
                writer.Write((short)(NumChannels == 1 ? 0 : NumChannels));
                writer.Write((short)(NumChannels == 1 ? 0 : FramesPerInterleave));
            }
        }

        private void GetData(BinaryWriter writer)
        {
            writer.BaseStream.Position = HeaderSize * NumChannels;
            if (NumChannels == 1)
            {
                writer.Write(AudioStream.Channels[0].GetAudioData, 0, GetBytesForAdpcmSamples(NumSamples));
            }
            else
            {
                byte[][] channels = AudioStream.Channels.Select(x => x.GetAudioData).ToArray();
                channels.Interleave(writer.BaseStream, GetBytesForAdpcmSamples(NumSamples), BytesPerInterleave, BytesPerBlock);
            }
        }

        private static DspStructure ReadDspFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = new BinaryReaderBE(stream, Encoding.UTF8, true))
            {
                var structure = new DspStructure();

                ParseHeader(reader, structure);

                if (readAudioData)
                {
                    reader.BaseStream.Position = HeaderSize * structure.NumChannels;
                    ParseData(reader, structure);
                }

                return structure;
            }
        }

        private static AdpcmStream GetAdpcmStream(DspStructure structure)
        {
            var audioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
            if (structure.Looping)
            {
                audioStream.SetLoop(structure.LoopStart, structure.NumSamples);
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
                audioStream.Channels.Add(channel);
            }

            return audioStream;
        }

        private static void ParseHeader(BinaryReader reader, DspStructure structure)
        {
            structure.NumSamples = reader.ReadInt32();
            structure.NumNibbles = reader.ReadInt32();
            structure.SampleRate = reader.ReadInt32();
            structure.Looping = reader.ReadInt16() == 1;
            structure.Format = reader.ReadInt16();
            structure.StartAddress = reader.ReadInt32();
            structure.EndAddress = reader.ReadInt32();
            structure.CurrentAddress = reader.ReadInt32();

            reader.BaseStream.Position = 0x4a;
            structure.NumChannels = reader.ReadInt16();
            structure.FramesPerInterleave = reader.ReadInt16();
            structure.NumChannels = structure.NumChannels == 0 ? 1 : structure.NumChannels;

            for (int i = 0; i < structure.NumChannels; i++)
            {
                reader.BaseStream.Position = HeaderSize * i + 0x1c;
                var channel = new AdpcmChannelInfo
                {
                    Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray(),
                    Gain = reader.ReadInt16(),
                    PredScale = reader.ReadInt16(),
                    Hist1 = reader.ReadInt16(),
                    Hist2 = reader.ReadInt16(),
                    LoopPredScale = reader.ReadInt16(),
                    LoopHist1 = reader.ReadInt16(),
                    LoopHist2 = reader.ReadInt16()
                };

                structure.Channels.Add(channel);
            }

            if (reader.BaseStream.Length < HeaderSize + GetBytesForAdpcmSamples(structure.NumSamples))
            {
                throw new InvalidDataException($"File doesn't contain enough data for {structure.NumSamples} samples");
            }

            if (GetNibbleFromSample(structure.NumSamples) != structure.NumNibbles)
            {
                throw new InvalidDataException("Sample count and nibble count do not match");
            }

            if (structure.Format != 0)
            {
                throw new InvalidDataException($"File does not contain ADPCM audio. Specified format is {structure.Format}");
            }
        }

        private static void ParseData(BinaryReader reader, DspStructure structure)
        {
            if (structure.NumChannels == 1)
            {
                structure.AudioData = new[] { reader.ReadBytes(GetBytesForAdpcmSamples(structure.NumSamples)) };
            }
            else
            {
                int dataLength = GetNextMultiple(GetBytesForAdpcmSamples(structure.NumSamples), 8) * structure.NumChannels;
                int interleaveSize = structure.FramesPerInterleave * BytesPerBlock;
                structure.AudioData = reader.BaseStream.DeInterleave(dataLength, interleaveSize, structure.NumChannels);
            }
        }
    }
}
