using System;
using System.IO;
using System.Linq;
using VGAudio.Codecs.GcAdpcm;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Dsp
{
    public class DspWriter : AudioWriter<DspWriter, DspConfiguration>
    {
        private GcAdpcmFormat Adpcm { get; set; }

        protected override int FileSize => (HeaderSize + AudioDataSize) * ChannelCount;

        private static int HeaderSize => 0x60;
        private int ChannelCount => Adpcm.ChannelCount;

        private int SampleCount => (Configuration.TrimFile && Adpcm.Looping ? LoopEnd : Math.Max(Adpcm.SampleCount, LoopEnd));
        private short Format { get; } = 0; /* 0 for ADPCM */

        private int SamplesPerInterleave => Configuration.SamplesPerInterleave;
        private int BytesPerInterleave => SampleCountToByteCount(SamplesPerInterleave);
        private int FramesPerInterleave => BytesPerInterleave / BytesPerFrame;

        private int AlignmentSamples => GetNextMultiple(Adpcm.LoopStart, Configuration.LoopPointAlignment) - Adpcm.LoopStart;
        private int LoopStart => Adpcm.LoopStart + AlignmentSamples;
        private int LoopEnd => Adpcm.LoopEnd + AlignmentSamples;

        private int StartAddr => SampleToNibble(Adpcm.Looping ? LoopStart : 0);
        private int EndAddr => SampleToNibble(Adpcm.Looping ? LoopEnd : SampleCount - 1);
        private static int CurAddr => SampleToNibble(0);

        protected override void SetupWriter(AudioData audio)
        {
            Adpcm = audio.GetFormat<GcAdpcmFormat>(new GcAdpcmParameters { Progress = Configuration.Progress });
        }

        protected override void WriteStream(Stream stream)
        {
            //RecalculateData();

            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness.BigEndian))
            {
                stream.Position = 0;
                WriteHeader(writer);
                WriteData(writer);
            }
        }

        private void WriteHeader(BinaryWriter writer)
        {
            for (int i = 0; i < ChannelCount; i++)
            {
                GcAdpcmChannel channel = Adpcm.Channels[i];
                writer.BaseStream.Position = HeaderSize * i;
                writer.Write(SampleCount);
                writer.Write(SampleCountToNibbleCount(SampleCount));
                writer.Write(Adpcm.SampleRate);
                writer.Write((short)(Adpcm.Looping ? 1 : 0));
                writer.Write(Format);
                writer.Write(StartAddr);
                writer.Write(EndAddr);
                writer.Write(CurAddr);
                writer.Write(channel.Coefs.ToByteArray(Endianness.BigEndian));
                writer.Write(channel.Gain);
                channel.StartContext.Write(writer);
                if (Adpcm.Looping)
                {
                    channel.LoopContext.Write(writer);
                }
                else
                {
                    writer.Write(new byte[3 * sizeof(short)]);
                }
                writer.Write((short)(ChannelCount == 1 ? 0 : ChannelCount));
                writer.Write((short)(ChannelCount == 1 ? 0 : FramesPerInterleave));
            }
        }

        private void WriteData(BinaryWriter writer)
        {
            writer.BaseStream.Position = HeaderSize * ChannelCount;
            if (ChannelCount == 1)
            {
                writer.Write(Adpcm.Channels[0].GetAdpcmAudio(), 0, SampleCountToByteCount(SampleCount));
            }
            else
            {
                byte[][] channels = Adpcm.Channels.Select(x => x.GetAdpcmAudio()).ToArray();
                channels.Interleave(writer.BaseStream, BytesPerInterleave, AudioDataSize);
            }
        }

        /// <summary>
        /// Size of a single channel's ADPCM audio data with padding when written to a file
        /// </summary>
        private int AudioDataSize
            => GetNextMultiple(SampleCountToByteCount(SampleCount), ChannelCount == 1 ? 1 : BytesPerFrame);
    }
}