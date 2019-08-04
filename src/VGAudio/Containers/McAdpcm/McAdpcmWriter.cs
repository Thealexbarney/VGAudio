using System;
using System.IO;
using System.Linq;
using VGAudio.Codecs.GcAdpcm;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.McAdpcm
{
    public class McAdpcmWriter : AudioWriter<McAdpcmWriter, McAdpcmConfiguration>
    {
        private static int DspHeaderSize => 0x60;

        private GcAdpcmFormat Adpcm { get; set; }

        protected override int FileSize => (ChannelCount == 1 ? 0x0C : 0x014) + (DspHeaderSize + AudioDataSize) * ChannelCount;

        private int ChannelCount => Adpcm.ChannelCount;

        private int McAdpcmHeaderSize => (ChannelCount == 1 ? 0x0C : 0x14);

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
            WriteMcAdpcmHeader(GetBinaryWriter(stream, Endianness.LittleEndian));
            for (int i = 0; i < ChannelCount; i++)
            {
                WriteDspMcAdpcmHeader(GetBinaryWriter(stream, Endianness.LittleEndian), i);
                WriteData(GetBinaryWriter(stream, Endianness.BigEndian), i);
            }
        }

        private void WriteMcAdpcmHeader(BinaryWriter writer)
        {
            writer.BaseStream.Position = 0;

            writer.Write(ChannelCount); // channel count
            writer.Write(McAdpcmHeaderSize); // header size
            writer.Write(DspHeaderSize + AudioDataSize); // channel 0 data size
            if (ChannelCount == 2)
            {
                writer.Write(McAdpcmHeaderSize + DspHeaderSize + AudioDataSize); // chabnel 1 offset
                writer.Write(DspHeaderSize + AudioDataSize); // channel 1 data size
            }
        }

        private void WriteDspMcAdpcmHeader(BinaryWriter writer, int i)
        {
            writer.BaseStream.Position = McAdpcmHeaderSize + (DspHeaderSize + AudioDataSize) * i;

            GcAdpcmChannel channel = Adpcm.Channels[i];
            writer.Write(SampleCount);
            writer.Write(SampleCountToNibbleCount(SampleCount));
            writer.Write(Adpcm.SampleRate);
            writer.Write((short)(Adpcm.Looping ? 1 : 0));
            writer.Write(Format);
            writer.Write(StartAddr);
            writer.Write(EndAddr);
            writer.Write(CurAddr);
            writer.Write(channel.Coefs.ToByteArray());
            writer.Write(channel.Gain);
            channel.StartContext.Write(writer);
            if (!Adpcm.Looping)
            {
                channel.LoopContext.Write(writer);
            }
            else
            {
                writer.Write(new byte[3 * sizeof(short)]);
            }
            writer.Write((short)(0));
            writer.Write((short)(0));
        }

        private void WriteData(BinaryWriter writer, int i)
        {
            writer.BaseStream.Position = McAdpcmHeaderSize + DspHeaderSize * (i+1) + AudioDataSize * i;
            writer.Write(Adpcm.Channels[i].GetAdpcmAudio(), 0, SampleCountToByteCount(SampleCount));
        }

        /// <summary>
        /// Size of a single channel's ADPCM audio data with padding when written to a file
        /// </summary>
        private int AudioDataSize => SampleCountToByteCount(SampleCount);
    }
}