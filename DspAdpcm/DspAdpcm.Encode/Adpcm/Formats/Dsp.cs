using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DspAdpcm.Encode.Adpcm.Formats
{
    public class Dsp
    {
        private const int HeaderSize = 0x60;
        public int FileSize => HeaderSize + Helpers.GetBytesForAdpcmSamples(AudioStream.NumSamples);
        public IAdpcmStream AudioStream { get; set; }
        public IAdpcmChannel AudioChannel => AudioStream.Channels[0];

        private short Format { get; } = 0; /* 0 for ADPCM */

        private int StartAddr => Helpers.GetNibbleAddress(AudioStream.Looping ? AudioStream.LoopStart : 0);
        private int EndAddr => Helpers.GetNibbleAddress(AudioStream.Looping ? AudioStream.LoopEnd : AudioStream.NumSamples - 1);
        private static int CurAddr => Helpers.GetNibbleAddress(0);

        public short Gain { get; set; }
        private short PredScale => AudioChannel.AudioData.First();

        public Dsp(IAdpcmStream stream)
        {
            if (stream.Channels.Count != 1)
            {
                throw new InvalidDataException($"Stream has {stream.Channels.Count} channels, not 1");
            }

            AudioStream = stream;
        }

        public IEnumerable<byte> GetHeader()
        {
            if (AudioStream.Looping)
            {
                Adpcm.Encode.SetLoopContext(AudioChannel, AudioStream.LoopStart);
            }

            var header = new List<byte>();
            header.AddRange(AudioStream.NumSamples.ToBytesBE());
            header.AddRange(Helpers.GetNibbleFromSample(AudioStream.NumSamples).ToBytesBE());
            header.AddRange(AudioStream.SampleRate.ToBytesBE());
            header.AddRange(((short)(AudioStream.Looping ? 1 : 0)).ToBytesBE());
            header.AddRange(Format.ToBytesBE());
            header.AddRange(StartAddr.ToBytesBE());
            header.AddRange(EndAddr.ToBytesBE());
            header.AddRange(CurAddr.ToBytesBE());
            header.AddRange(AudioChannel.Coefs.SelectMany(x => x.ToBytesBE()));
            header.AddRange(Gain.ToBytesBE());
            header.AddRange(PredScale.ToBytesBE());
            header.AddRange(AudioChannel.Hist1.ToBytesBE());
            header.AddRange(AudioChannel.Hist2.ToBytesBE());
            header.AddRange(AudioChannel.LoopPredScale.ToBytesBE());
            header.AddRange(AudioChannel.LoopHist1.ToBytesBE());
            header.AddRange(AudioChannel.LoopHist2.ToBytesBE());
            header.AddRange(new byte[HeaderSize - header.Count]); //Padding

            return header;
        }

        public IEnumerable<byte> GetFile()
        {
            return GetHeader().Concat(AudioChannel.AudioData);
        }
    }
}
