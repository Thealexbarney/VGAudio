using System;
using System.IO;
using System.Linq;
using static DspAdpcm.Lib.Helpers;

namespace DspAdpcm.Lib.Adpcm.Formats
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
        public DspConfiguration Configuration { get; } = new DspConfiguration();

        /// <summary>
        /// The size in bytes of the DSP file.
        /// </summary>
        public int FileLength => HeaderSize + GetBytesForAdpcmSamples(NumSamples);

        private const int HeaderSize = 0x60;
        private AdpcmChannel AudioChannel => AudioStream.Channels[0];

        private int NumSamples => (Configuration.TrimFile && AudioStream.Looping ? AudioStream.LoopEnd : AudioStream.NumSamples) + AlignmentSamples;
        private short Format { get; } = 0; /* 0 for ADPCM */

        private int AlignmentSamples => GetNextMultiple(AudioStream.LoopStart, Configuration.LoopPointAlignment) - AudioStream.LoopStart;
        private int LoopStart => AudioStream.LoopStart + AlignmentSamples;
        private int LoopEnd => AudioStream.LoopEnd + AlignmentSamples;

        private int StartAddr => GetNibbleAddress(AudioStream.Looping ? LoopStart : 0);
        private int EndAddr => GetNibbleAddress(AudioStream.Looping ? LoopEnd : NumSamples - 1);
        private static int CurAddr => GetNibbleAddress(0);

        private short PredScale => AudioChannel.GetAudioData[0];

        /// <summary>
        /// Initializes a new <see cref="Dsp"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Dsp"/>.</param>
        public Dsp(AdpcmStream stream)
        {
            if (stream.Channels.Count != 1)
            {
                throw new InvalidDataException($"Stream has {stream.Channels.Count} channels, not 1");
            }

            AudioStream = stream;
        }

        /// <summary>
        /// Initializes a new <see cref="Dsp"/> by parsing an existing
        /// DSP file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the DSP file. Must be seekable.</param>
        public Dsp(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            ReadDspFile(stream);
        }

        private Dsp() { }

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

            var dsp = new Dsp();
            var a = dsp.ReadDspFile(stream, false);
            return a;
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

        private void GetHeader(Stream stream)
        {
            RecalculateData();

            BinaryWriterBE header = new BinaryWriterBE(stream);

            header.WriteBE(NumSamples);
            header.WriteBE(GetNibbleFromSample(NumSamples));
            header.WriteBE(AudioStream.SampleRate);
            header.WriteBE((short)(AudioStream.Looping ? 1 : 0));
            header.WriteBE(Format);
            header.WriteBE(StartAddr);
            header.WriteBE(EndAddr);
            header.WriteBE(CurAddr);
            header.Write(AudioChannel.Coefs.ToFlippedBytes());
            header.WriteBE(AudioChannel.Gain);
            header.WriteBE(PredScale);
            header.WriteBE(AudioChannel.Hist1);
            header.WriteBE(AudioChannel.Hist2);
            header.WriteBE(AudioChannel.LoopPredScale);
            header.WriteBE(AudioChannel.LoopHist1);
            header.WriteBE(AudioChannel.LoopHist2);
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

            stream.Position = 0;
            GetHeader(stream);
            stream.Position = HeaderSize;

            stream.Write(AudioChannel.GetAudioData, 0, GetBytesForAdpcmSamples(NumSamples));
        }

        private DspStructure ReadDspFile(Stream stream, bool readAudioData = true)
        {
            using (var reader = new BinaryReaderBE(stream))
            {
                var structure = new DspStructure();

                ParseHeader(stream, structure);

                if (!readAudioData)
                {
                    return structure;
                }

                AudioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
                
                if (structure.Looping)
                {
                    AudioStream.SetLoop(structure.LoopStart, structure.LoopEnd);
                }

                reader.BaseStream.Position = HeaderSize;
                ParseData(stream, structure);

                return structure;
            }
        }

        private static void ParseHeader(Stream stream, DspStructure structure)
        {
            var reader = new BinaryReaderBE(stream);

            structure.NumSamples = reader.ReadInt32BE();
            structure.NumNibbles = reader.ReadInt32BE();
            structure.SampleRate = reader.ReadInt32BE();
            structure.Looping = reader.ReadInt16BE() == 1;
            structure.Format = reader.ReadInt16BE();
            structure.StartAddress = reader.ReadInt32BE();
            structure.EndAddress = reader.ReadInt32BE();
            structure.CurrentAddress = reader.ReadInt32BE();

            var channel = new AdpcmChannelInfo
            {
                Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16BE()).ToArray(),
                Gain = reader.ReadInt16BE(),
                PredScale = reader.ReadInt16BE(),
                Hist1 = reader.ReadInt16BE(),
                Hist2 = reader.ReadInt16BE(),
                LoopPredScale = reader.ReadInt16BE(),
                LoopHist1 = reader.ReadInt16BE(),
                LoopHist2 = reader.ReadInt16BE()
            };

            structure.Channels.Add(channel);

            if (stream.Length < HeaderSize + GetBytesForAdpcmSamples(structure.NumSamples))
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

        private void ParseData(Stream stream, DspStructure structure)
        {
            var reader = new BinaryReaderBE(stream);

            byte[] audio = reader.ReadBytes(GetBytesForAdpcmSamples(structure.NumSamples));

            var channel = new AdpcmChannel(structure.NumSamples, audio)
            {
                Coefs = structure.Channels[0].Coefs,
                Gain = structure.Channels[0].Gain,
                Hist1 = structure.Channels[0].Hist1,
                Hist2 = structure.Channels[0].Hist2
            };
            channel.SetLoopContext(structure.Channels[0].LoopPredScale, structure.Channels[0].LoopHist1,
                    structure.Channels[0].LoopHist2);

            AudioStream.Channels.Add(channel);
        }

        /// <summary>
        /// Contains the options used to build the DSP file.
        /// </summary>
        public class DspConfiguration
        {
            /// <summary>
            /// If <c>true</c>, recalculates the loop context when building the DSP.
            /// If <c>false</c>, reuses the loop context read from an imported DSP
            /// if available.
            /// Default is <c>true</c>.
            /// </summary>
            public bool RecalculateLoopContext { get; set; } = true;

            /// <summary>
            /// If <c>true</c>, trims the output file length to the set LoopEnd.
            /// If <c>false</c> or if the <see cref="Dsp"/> does not loop,
            /// the output file is not trimmed.
            /// if available.
            /// Default is <c>true</c>.
            /// </summary>
            public bool TrimFile { get; set; } = true;

            /// <summary>
            /// When building the DSP file, the loop points and audio will
            /// be adjusted so that the start loop point is a multiple of
            /// this number. Default is 1.
            /// </summary>
            public int LoopPointAlignment { get; set; } = 1;
        }
    }
}
