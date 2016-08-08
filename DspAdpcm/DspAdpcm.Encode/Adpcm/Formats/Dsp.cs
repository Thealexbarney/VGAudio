using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DspAdpcm.Encode.Helpers;

namespace DspAdpcm.Encode.Adpcm.Formats
{
    public class Dsp
    {
        private const int HeaderSize = 0x60;
        public int FileSize => HeaderSize + GetBytesForAdpcmSamples(AudioStream.NumSamples);
        public AdpcmStream AudioStream { get; set; }
        public AdpcmChannel AudioChannel => AudioStream.Channels[0];

        private short Format { get; } = 0; /* 0 for ADPCM */

        private int StartAddr => GetNibbleAddress(AudioStream.Looping ? AudioStream.LoopStart : 0);
        private int EndAddr => GetNibbleAddress(AudioStream.Looping ? AudioStream.LoopEnd : AudioStream.NumSamples - 1);
        private static int CurAddr => GetNibbleAddress(0);

        private short PredScale => AudioChannel.AudioData.First();

        public Dsp(AdpcmStream stream)
        {
            if (stream.Channels.Count != 1)
            {
                throw new InvalidDataException($"Stream has {stream.Channels.Count} channels, not 1");
            }

            AudioStream = stream;
        }

        public Dsp(Stream stream)
        {
            ReadDspFile(stream);
        }

        public IEnumerable<byte> GetHeader()
        {
            if (AudioStream.Looping)
            {
                AudioChannel.SetLoopContext(AudioStream.LoopStart);
            }

            var header = new List<byte>();
            header.Add32BE(AudioStream.NumSamples);
            header.Add32BE(GetNibbleFromSample(AudioStream.NumSamples));
            header.Add32BE(AudioStream.SampleRate);
            header.Add16BE(AudioStream.Looping ? 1 : 0);
            header.Add16BE(Format);
            header.Add32BE(StartAddr);
            header.Add32BE(EndAddr);
            header.Add32BE(CurAddr);
            header.AddRange(AudioChannel.Coefs.SelectMany(x => x.ToBytesBE()));
            header.Add16BE(AudioChannel.Gain);
            header.Add16BE(PredScale);
            header.Add16BE(AudioChannel.Hist1);
            header.Add16BE(AudioChannel.Hist2);
            header.Add16BE(AudioChannel.LoopPredScale);
            header.Add16BE(AudioChannel.LoopHist1);
            header.Add16BE(AudioChannel.LoopHist2);
            header.AddRange(new byte[HeaderSize - header.Count]); //Padding

            return header;
        }

        public IEnumerable<byte> GetFile()
        {
            return GetHeader().Concat(AudioChannel.AudioData);
        }

        public void ReadDspFile(Stream stream)
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
    }
}
