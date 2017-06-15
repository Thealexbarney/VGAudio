using System.IO;
using System.Linq;
using VGAudio.Codecs.CriAdx;
using VGAudio.Formats;
using VGAudio.Formats.CriAdx;
using VGAudio.Utilities;
using static VGAudio.Formats.CriAdx.CriAdxHelpers;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Adx
{
    public class AdxWriter : AudioWriter<AdxWriter, AdxConfiguration>
    {
        private CriAdxFormat Adpcm { get; set; }

        private const ushort AdxHeaderSignature = 0x8000;
        private const ushort AdxFooterSignature = 0x8001;
        protected override int FileSize => AudioOffset + AudioSize + FooterSize;

        // Add 3 frames of as a buffer if trimming
        private int SampleCount => Configuration.TrimFile && Adpcm.Looping ? Adpcm.LoopEnd + SamplesPerFrame * 3 : Adpcm.SampleCount;
        private int ChannelCount => Adpcm.ChannelCount;
        private int FrameSize => Adpcm.FrameSize;
        private int Version => Adpcm.Version;

        private int AlignmentBytes { get; set; }
        private int SamplesPerFrame => (FrameSize - 2) * 2;
        private int FrameCount => SampleCount.DivideByRoundUp(SamplesPerFrame);

        private int HeaderSize => Adpcm.Looping ? Version == 4 ? 60 : 52 : Version == 4 ? 36 : 32;
        private int CopyrightOffset => HeaderSize + AlignmentBytes;
        private int AudioOffset => CopyrightOffset + 4;
        private int AudioSize => FrameSize * FrameCount * ChannelCount;
        private int FooterOffset => AudioOffset + AudioSize;
        private int FooterSize => Adpcm.Looping ? GetNextMultiple(FooterOffset, 0x800) - FooterOffset : Adpcm.FrameSize;
        private int LoopStartOffset => AudioOffset + SampleCountToByteCount(Adpcm.LoopStart, FrameSize) * ChannelCount;
        private int LoopEndOffset => AudioOffset + GetNextMultiple(SampleCountToByteCount(Adpcm.LoopEnd, FrameSize), FrameSize) * ChannelCount;

        protected override void SetupWriter(AudioData audio)
        {
            var encodingConfig = new CriAdxConfiguration
            {
                Version = Configuration.Version,
                FrameSize = Configuration.FrameSize,
                Filter = Configuration.Filter,
                Type = Configuration.Type
            };
            Adpcm = audio.GetFormat<CriAdxFormat>(encodingConfig);
            if (Adpcm.Looping)
            {
                CalculateAlignmentBytes();
            }
        }

        private void CalculateAlignmentBytes()
        {
            //Start loop frame offset should be a multiple of 0x800
            int startLoopOffset = SampleCountToByteCount(Adpcm.LoopStart, FrameSize) * ChannelCount + HeaderSize + 4;
            AlignmentBytes = GetNextMultiple(startLoopOffset, 0x800) - startLoopOffset;

            //Version 3 pushes the loop start one block back for every full frame of alignment samples 
            if (Adpcm.Version == 3)
            {
                AlignmentBytes += Adpcm.AlignmentSamples / SamplesPerFrame * 0x800;
            }
        }

        protected override void WriteStream(Stream stream)
        {
            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness.BigEndian))
            {
                stream.Position = 0;
                WriteHeader(writer);
                WriteData(writer);
                WriteFooter(writer);
            }
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.Write(AdxHeaderSignature);
            writer.Write((short)CopyrightOffset);
            writer.Write((byte)Adpcm.Type);
            writer.Write((byte)FrameSize);
            writer.Write((byte)4); //bit-depth
            writer.Write((byte)ChannelCount);
            writer.Write(Adpcm.SampleRate);
            writer.Write(SampleCount);
            writer.Write(Adpcm.Type != CriAdxType.Fixed ? Adpcm.HighpassFrequency : (short)0);
            writer.Write((byte)Version);
            writer.Write((byte)0); //flags

            if (Version == 4)
            {
                writer.Write(0);
                for (int i = 0; i < ChannelCount; i++)
                {
                    writer.Write(Adpcm.Channels[i].History);
                    writer.Write(Adpcm.Channels[i].History);
                }
                if (ChannelCount == 1)
                {
                    writer.Write(0);
                }
            }

            writer.Write((short)Adpcm.AlignmentSamples);
            writer.Write((short)(Adpcm.Looping ? 1 : 0));
            writer.Write(Adpcm.Looping ? 1 : 0);
            writer.Write(Adpcm.LoopStart);
            writer.Write(LoopStartOffset);
            writer.Write(Adpcm.LoopEnd);
            writer.Write(LoopEndOffset);

            writer.BaseStream.Position = CopyrightOffset - 2;
            writer.WriteUTF8("(c)CRI");
        }

        private void WriteData(BinaryWriter writer)
        {
            byte[][] audio = Adpcm.Channels.Select(x => x.Audio).ToArray();
            audio.Interleave(writer.BaseStream, FrameSize, FrameCount * FrameSize);
        }

        private void WriteFooter(BinaryWriter writer)
        {
            int padding = FooterSize - 4;
            writer.Write(AdxFooterSignature);
            writer.Write((short)padding);
        }
    }
}
