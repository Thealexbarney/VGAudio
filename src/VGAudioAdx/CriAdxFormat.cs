using System;
using VGAudio.Codecs;
using VGAudio.Containers.Adx;
using VGAudio.Utilities;

// ReSharper disable once CheckNamespace
namespace VGAudio.Formats
{
    public class CriAdxFormat : AudioFormatBase<CriAdxFormat, CriAdxFormatBuilder, CriAdxConfiguration>
    {
        public CriAdxChannel[] Channels { get; }
        public short HighpassFrequency { get; }
        public int FrameSize { get; }
        public int AlignmentSamples { get; }
        public override int LoopStart => UnalignedLoopStart + AlignmentSamples;
        public override int LoopEnd => UnalignedLoopEnd + AlignmentSamples;
        public int Version { get; }
        public AdxType Type { get; } = AdxType.Linear;

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
                var options = new CriAdxConfiguration
                {
                    SampleRate = SampleRate,
                    FrameSize = FrameSize,
                    Padding = AlignmentSamples,
                    HighpassFrequency = HighpassFrequency,
                    Type = Type,
                    Version = Version
                };
                pcmChannels[i] = CriAdxCodec.Decode(Channels[i].Audio, SampleCount, options);
            });

            return new Pcm16Format.Builder(pcmChannels, SampleRate)
                .WithLoop(Looping, LoopStart, LoopEnd)
                .WithTracks(Tracks)
                .Build();
        }

        public override CriAdxFormat EncodeFromPcm16(Pcm16Format pcm16, CriAdxConfiguration config)
        {
            var channels = new CriAdxChannel[pcm16.ChannelCount];
            int samplesPerFrame = (config.FrameSize - 2) * 2;
            int alignmentMultiple = pcm16.ChannelCount == 1 ? samplesPerFrame * 2 : samplesPerFrame;
            int alignmentSamples = Helpers.GetNextMultiple(pcm16.LoopStart, alignmentMultiple) - pcm16.LoopStart;

            Parallel.For(0, pcm16.ChannelCount, i =>
            {
                var channelConfig = new CriAdxConfiguration
                {
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

        public override CriAdxFormat EncodeFromPcm16(Pcm16Format pcm16) => EncodeFromPcm16(pcm16, new CriAdxConfiguration());

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
