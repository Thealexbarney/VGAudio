using System.IO;
using VGAudio.Containers.Adx;
using VGAudio.Formats;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

// ReSharper disable once CheckNamespace
namespace VGAudio.Containers
{
    public class AdxWriter : AudioWriter<AdxWriter, AdxConfiguration>
    {
        private CriAdxFormat Adpcm { get; set; }

        private const int CopyrightOffset = 0x40;
        private const ushort AdxHeaderSignature = 0x8000;
        private const ushort AdxFooterSignature = 0x8001;
        protected override int FileSize => CopyrightOffset + 4 + FrameSize * FrameCount * ChannelCount + FooterSize;

        private int SampleCount => Adpcm.SampleCount;
        private int ChannelCount => Adpcm.ChannelCount;
        private int SamplesPerFrame => (FrameSize - 2) * 2;
        private int FrameSize => Adpcm.FrameSize;
        private int FooterSize => Adpcm.FrameSize;
        private int FrameCount => SampleCount.DivideByRoundUp(SamplesPerFrame);

        protected override void SetupWriter(AudioData audio)
        {
            Adpcm = audio.GetFormat<CriAdxFormat>();
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
            writer.Write((byte)3); //encoding type
            writer.Write((byte)FrameSize);
            writer.Write((byte)4); //bit-depth
            writer.Write((byte)ChannelCount);
            writer.Write(Adpcm.SampleRate);
            writer.Write(SampleCount);
            writer.Write(Adpcm.HighpassFrequency);
            writer.Write((byte)3); //version
            writer.Write((byte)0); //flags
            writer.Write((short)0); //loop alignment samples
            writer.Write((short)(Adpcm.Looping ? 1 : 0));
            writer.Write(Adpcm.Looping ? 1 : 0);
            writer.Write(Adpcm.LoopStart);
            writer.Write(0);//loop start byte
            writer.Write(Adpcm.LoopEnd);
            writer.Write(0); //loop end byte

            writer.BaseStream.Position = CopyrightOffset - 2;
            writer.WriteUTF8("(c)CRI");
        }

        private void WriteData(BinaryWriter writer)
        {
            Adpcm.Channels.Interleave(writer.BaseStream, FrameSize);
        }

        private void WriteFooter(BinaryWriter writer)
        {
            int padding = Adpcm.FrameSize - 4;
            writer.Write(AdxFooterSignature);
            writer.Write((short)padding);
        }
    }
}
