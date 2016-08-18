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

        private int NumSamples => Configuration.TrimFile && AudioStream.Looping ? AudioStream.LoopEnd : AudioStream.NumSamples;
        private short Format { get; } = 0; /* 0 for ADPCM */

        private int StartAddr => GetNibbleAddress(AudioStream.Looping ? AudioStream.LoopStart : 0);
        private int EndAddr => GetNibbleAddress(AudioStream.Looping ? AudioStream.LoopEnd : NumSamples - 1);
        private static int CurAddr => GetNibbleAddress(0);

        private short PredScale => AudioChannel.AudioData.First();

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

        private void RecalculateData()
        {
            var loopContextToCalculate = Configuration.RecalculateLoopContext
                ? AudioStream.Channels.Where(x => !x.SelfCalculatedLoopContext)
                : AudioStream.Channels.Where(x => !x.LoopContextCalculated);

            Decode.CalculateLoopContext(loopContextToCalculate, AudioStream.Looping ? AudioStream.LoopStart : 0);
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
            
            stream.Write(AudioChannel.AudioByteArray, 0, GetBytesForAdpcmSamples(NumSamples));
        }

        private void ReadDspFile(Stream stream)
        {
            using (var reader = new BinaryReaderBE(stream))
            {
                int numSamples = reader.ReadInt32BE();
                int numNibbles = reader.ReadInt32BE();
                int sampleRate = reader.ReadInt32BE();
                bool looped = reader.ReadInt16BE() == 1;
                short format = reader.ReadInt16BE();

                if (stream.Length < HeaderSize + GetBytesForAdpcmSamples(numSamples))
                {
                    throw new InvalidDataException($"File doesn't contain enough data for {numSamples} samples");
                }

                if (GetNibbleFromSample(numSamples) != numNibbles)
                {
                    throw new InvalidDataException("Sample count and nibble count do not match");
                }

                if (format != 0)
                {
                    throw new InvalidDataException($"File does not contain ADPCM audio. Specified format is {format}");
                }

                AdpcmStream adpcm = new AdpcmStream(numSamples, sampleRate);
                var channel = new AdpcmChannel(numSamples);
                
                int loopStart = GetSampleFromNibble(reader.ReadInt32BE());
                int loopEnd = GetSampleFromNibble(reader.ReadInt32BE());
                reader.ReadInt32BE(); //CurAddr

                if (looped)
                {
                    adpcm.SetLoop(loopStart, loopEnd);
                }
                

                channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16BE()).ToArray();
                channel.Gain = reader.ReadInt16BE();
                reader.ReadInt16BE(); //Initial Predictor/Scale
                channel.Hist1 = reader.ReadInt16BE();
                channel.Hist2 = reader.ReadInt16BE();

                reader.BaseStream.Seek(HeaderSize, SeekOrigin.Begin);

                channel.AudioByteArray = reader.ReadBytes(GetBytesForAdpcmSamples(numSamples));

                adpcm.Channels.Add(channel);
                AudioStream = adpcm;
            }
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
        }
    }
}
