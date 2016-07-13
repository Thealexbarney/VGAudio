using System;
using System.Collections.Generic;
using System.Linq;
using DspAdpcm.Encode.Adpcm;
using static DspAdpcm.Encode.Adpcm.Helpers;

namespace DspAdpcm.Encode.Formats
{
    public class Dsp
    {
        private const int HeaderSize = 0x60;
        public int DspFileSize => HeaderSize + GetBytesForAdpcmSamples(AudioStream.NumSamples);
        public AdpcmStream AudioStream { get; set; }
        public AdpcmChannel AudioChannel { get; set; } 

        public int NumSamples => AudioStream.NumSamples;

        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
        public bool LoopFlag { get; }
        private short Format { get; } = 0; /* 0 for ADPCM */

        private int StartAddr => GetNibbleAddress(LoopFlag ? LoopStart : 0);
        private int EndAddr => GetNibbleAddress(LoopFlag ? LoopEnd : NumSamples - 1);
        private static int CurAddr => GetNibbleAddress(0);

        public short Gain { get; set; }
        public short PredScale => AudioChannel.AudioData[0];

        public Dsp(AdpcmStream stream, int channel = 0)
        {
            if (stream.Channels.ElementAtOrDefault(channel) == null)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), $"Channel {channel} does not exist");
            }

            AudioChannel = stream.Channels[channel];
            AudioStream = stream;
        }

        public IEnumerable<byte> GetHeader()
        {
            if (LoopFlag)
            {
                Adpcm.Encode.GetLoopContext(AudioChannel, LoopStart);
            }

            var header = new List<byte>();
            header.AddRange(AudioStream.NumSamples.ToBytesBE());
            header.AddRange(AudioStream.NumNibbles.ToBytesBE());
            header.AddRange(AudioStream.SampleRate.ToBytesBE());
            header.AddRange(((short)(LoopFlag ? 1 : 0)).ToBytesBE());
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

        public IEnumerable<byte> GetDspFile()
        {
            return GetHeader().Concat(AudioChannel.AudioData);
        }
    }
}
