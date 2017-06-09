using System.IO;
using System.Linq;
using VGAudio.Codecs;
using VGAudio.Containers.Adx;
using VGAudio.Formats;
using VGAudio.Utilities;
using static VGAudio.Formats.Adx.CriAdxHelpers;
using static VGAudio.Utilities.Helpers;

// ReSharper disable once CheckNamespace
namespace VGAudio.Containers
{
    public class AdxWriter : AudioWriter<AdxWriter, AdxConfiguration>
    {
        private CriAdxFormat Adpcm { get; set; }

        private const ushort AdxHeaderSignature = 0x8000;
        private const ushort AdxFooterSignature = 0x8001;
        protected override int FileSize => CopyrightOffset + 4 + FrameSize * FrameCount * ChannelCount + FooterSize;

        private int SampleCount => Adpcm.SampleCount;
        private int ChannelCount => Adpcm.ChannelCount;
        private int SamplesPerFrame => (FrameSize - 2) * 2;
        private int FrameSize => Adpcm.FrameSize;
        private int FrameCount => SampleCount.DivideByRoundUp(SamplesPerFrame);
        private int HeaderSize => Adpcm.Looping ? 60 : Version == 4 ? 36 : 32;
        private int FooterSize => Adpcm.Looping ? GetNextMultiple(CopyrightOffset + 4 + FrameSize * FrameCount * ChannelCount, 0x800) - (CopyrightOffset + 4 + FrameSize * FrameCount * ChannelCount) : Adpcm.FrameSize;
        private int AlignmentBytes { get; set; }
        private int CopyrightOffset => AlignmentBytes + HeaderSize + (Version == 4 && ChannelCount > 2 ? 4 * ChannelCount - 8 : 0);
        private int LoopStartOffset => SampleCountToByteCount(Adpcm.LoopStart, FrameSize) * ChannelCount + CopyrightOffset + 4;
        private int LoopEndOffset => GetNextMultiple(SampleCountToByteCount(Adpcm.LoopEnd, FrameSize), FrameSize) * ChannelCount + CopyrightOffset + 4;
        private int Version => Adpcm.Version;

        protected override void SetupWriter(AudioData audio)
        {
            var encodingConfig = new CriAdxConfiguration
            {
                Version = Configuration.Version,
                FrameSize = Configuration.FrameSize
            };
            Adpcm = audio.GetFormat<CriAdxFormat>(encodingConfig);
            if (Adpcm.Looping)
            {
                CalculateAlignmentBytes();
            }
        }

        private void CalculateAlignmentBytes()
        {
            int startLoopOffset = SampleCountToByteCount(Adpcm.LoopStart, FrameSize) * ChannelCount + HeaderSize + 4;
            AlignmentBytes = GetNextMultiple(startLoopOffset, 0x800) - startLoopOffset;
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
            writer.Write((byte)Adpcm.Type); //encoding type
            writer.Write((byte)FrameSize);
            writer.Write((byte)4); //bit-depth
            writer.Write((byte)ChannelCount);
            writer.Write(Adpcm.SampleRate);
            writer.Write(SampleCount);
            writer.Write(Adpcm.HighpassFrequency);
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
            audio.Interleave(writer.BaseStream, FrameSize);
        }

        private void WriteFooter(BinaryWriter writer)
        {
            int padding = FooterSize - 4;
            writer.Write(AdxFooterSignature);
            writer.Write((short)padding);
        }
    }
}
