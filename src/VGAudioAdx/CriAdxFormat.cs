using System;
using System.IO;
using VGAudio.Codecs;
using VGAudio.Containers.Adx;
using VGAudio.Utilities;

// ReSharper disable once CheckNamespace
namespace VGAudio.Formats
{
    public class CriAdxFormat : AudioFormatBase<CriAdxFormat, CriAdxFormat.Builder>
    {
        public CriAdxChannel[] Channels { get; }
        public short HighpassFrequency { get; }
        public int FrameSize { get; }
        public int AlignmentSamples { get; }
        public int Version { get; }
        public AdxType Type { get; } = AdxType.Standard;

        public CriAdxFormat() => Channels = new CriAdxChannel[0];
        private CriAdxFormat(Builder b) : base(b)
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
                var options = new CriAdxOptions
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

        public override CriAdxFormat EncodeFromPcm16(Pcm16Format pcm16)
        {
            var channels = new CriAdxChannel[pcm16.ChannelCount];
            int frameSize = 18;
            int samplesPerFrame = (frameSize - 2) * 2;
            int alignmentMultiple = pcm16.ChannelCount == 1 ? samplesPerFrame * 2 : samplesPerFrame;
            int alignmentSamples = Helpers.GetNextMultiple(pcm16.LoopStart, alignmentMultiple) - pcm16.LoopStart;

            Parallel.For(0, pcm16.ChannelCount, i =>
            {
                var options = new CriAdxOptions
                {
                    SampleRate = pcm16.SampleRate,
                    FrameSize = frameSize,
                    Padding = alignmentSamples,
                    Type = Type
                };
                byte[] adpcm = CriAdxCodec.Encode(pcm16.Channels[i], options);
                channels[i] = new CriAdxChannel(adpcm, options.History);
            });

            return new Builder(channels, pcm16.SampleCount + alignmentSamples, pcm16.SampleRate, frameSize, 500)
                .WithLoop(pcm16.Looping, pcm16.LoopStart + alignmentSamples, pcm16.LoopEnd + alignmentSamples)
                .WithAlignmentSamples(alignmentSamples)
                .Build();
        }

        protected override CriAdxFormat GetChannelsInternal(int[] channelRange)
        {
            throw new NotImplementedException();
        }

        protected override CriAdxFormat AddInternal(CriAdxFormat format)
        {
            throw new NotImplementedException();
        }

        public override Builder GetCloneBuilder()
        {
            throw new NotImplementedException();
        }

        public class Builder : AudioFormatBaseBuilder<CriAdxFormat, Builder>
        {
            public CriAdxChannel[] Channels { get; set; }
            public short HighpassFrequency { get; set; }
            public int FrameSize { get; set; }
            public int AlignmentSamples { get; set; }
            public AdxType Type { get; set; } = AdxType.Standard;
            public int Version => Channels?[0]?.Version ?? 0;
            protected override int ChannelCount => Channels.Length;

            public Builder(CriAdxChannel[] channels, int sampleCount, int sampleRate, int frameSize, short highpassFrequency)
            {
                if (channels == null || channels.Length < 1)
                    throw new InvalidDataException("Channels parameter cannot be empty or null");

                Channels = channels;
                SampleCount = sampleCount;
                SampleRate = sampleRate;
                FrameSize = frameSize;
                HighpassFrequency = highpassFrequency;

                int length = Channels[0]?.Audio?.Length ?? 0;
                foreach (CriAdxChannel channel in Channels)
                {
                    if (channel == null)
                        throw new InvalidDataException("All provided channels must be non-null");

                    if (channel.Audio?.Length != length)
                        throw new InvalidDataException("All channels must have the same length");
                }
            }

            public Builder WithEncodingType(AdxType type)
            {
                Type = type;
                return this;
            }

            public Builder WithAlignmentSamples(int alignmentSamplesCount)
            {
                AlignmentSamples = alignmentSamplesCount;
                return this;
            }

            public override CriAdxFormat Build() => new CriAdxFormat(this);
        }
    }
}
