using System;
using VGAudio.Codecs.CriAdx;
using VGAudio.Formats.Pcm16;
using VGAudio.Utilities;
using static VGAudio.Formats.CriAdx.CriAdxHelpers;

namespace VGAudio.Formats.CriAdx
{
    public class CriAdxFormat : AudioFormatBase<CriAdxFormat, CriAdxFormatBuilder, CriAdxParameters>
    {
        public CriAdxChannel[] Channels { get; }
        public short HighpassFrequency { get; }
        public int FrameSize { get; }
        public int AlignmentSamples { get; }
        public override int SampleCount => UnalignedSampleCount + AlignmentSamples;
        public override int LoopStart => UnalignedLoopStart + AlignmentSamples;
        public override int LoopEnd => UnalignedLoopEnd + AlignmentSamples;
        public int Version { get; }
        public CriAdxType Type { get; } = CriAdxType.Linear;

        public CriAdxFormat() => Channels = new CriAdxChannel[0];
        internal CriAdxFormat(CriAdxFormatBuilder b) : base(b)
        {
            Channels = b.Channels;
            FrameSize = b.FrameSize;
            HighpassFrequency = b.HighpassFrequency;
            Type = b.Type;
            Version = b.Version;
            AlignmentSamples = b.AlignmentSamples;
        }

        public override Pcm16Format ToPcm16()
        {
            var pcmChannels = new short[Channels.Length][];
            Parallel.For(0, Channels.Length, i =>
            {
                var options = new CriAdxParameters
                {
                    SampleRate = SampleRate,
                    FrameSize = FrameSize,
                    Padding = AlignmentSamples,
                    HighpassFrequency = HighpassFrequency,
                    Type = Type,
                    Version = Version
                };
                pcmChannels[i] = CriAdxCodec.Decode(Channels[i].Audio, UnalignedSampleCount, options);
            });

            return new Pcm16FormatBuilder(pcmChannels, SampleRate)
                .WithLoop(Looping, UnalignedLoopStart, UnalignedLoopEnd)
                .WithTracks(Tracks)
                .Build();
        }

        public override CriAdxFormat EncodeFromPcm16(Pcm16Format pcm16, CriAdxParameters config)
        {
            var channels = new CriAdxChannel[pcm16.ChannelCount];
            int samplesPerFrame = (config.FrameSize - 2) * 2;
            int alignmentMultiple = pcm16.ChannelCount == 1 ? samplesPerFrame * 2 : samplesPerFrame;
            int alignmentSamples = Helpers.GetNextMultiple(pcm16.LoopStart, alignmentMultiple) - pcm16.LoopStart;

            int frameCount = SampleCountToByteCount(pcm16.SampleCount, config.FrameSize).DivideByRoundUp(config.FrameSize) * pcm16.ChannelCount;
            config.Progress?.ReportTotal(frameCount);

            Parallel.For(0, pcm16.ChannelCount, i =>
            {
                var channelConfig = new CriAdxParameters
                {
                    Progress = config.Progress,
                    SampleRate = pcm16.SampleRate,
                    FrameSize = config.FrameSize,
                    Padding = alignmentSamples,
                    Filter = config.Filter,
                    Type = config.Type,
                    Version = config.Version
                };
                byte[] adpcm = CriAdxCodec.Encode(pcm16.Channels[i], channelConfig);
                channels[i] = new CriAdxChannel(adpcm, channelConfig.History, channelConfig.Version);
            });

            return new CriAdxFormatBuilder(channels, pcm16.SampleCount, pcm16.SampleRate, config.FrameSize, 500)
                .WithLoop(pcm16.Looping, pcm16.LoopStart, pcm16.LoopEnd)
                .WithAlignmentSamples(alignmentSamples)
                .WithEncodingType(config.Type)
                .Build();
        }

        public override CriAdxFormat EncodeFromPcm16(Pcm16Format pcm16) => EncodeFromPcm16(pcm16, new CriAdxParameters());

        protected override CriAdxFormat GetChannelsInternal(int[] channelRange)
        {
            throw new NotImplementedException();
        }

        protected override CriAdxFormat AddInternal(CriAdxFormat format)
        {
            throw new NotImplementedException();
        }

        public override CriAdxFormatBuilder GetCloneBuilder()
        {
            throw new NotImplementedException();
        }
    }
}
