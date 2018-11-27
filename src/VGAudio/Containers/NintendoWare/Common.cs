using System.Collections.Generic;
using System.Linq;
using VGAudio.Codecs.GcAdpcm;
using VGAudio.Containers.NintendoWare.Structures;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Formats.Pcm16;
using VGAudio.Formats.Pcm8;
using VGAudio.Utilities;

namespace VGAudio.Containers.NintendoWare
{
    internal static class Common
    {
        public static int SamplesToBytes(int sampleCount, NwCodec codec)
        {
            switch (codec)
            {
                case NwCodec.GcAdpcm:
                    return GcAdpcmMath.SampleCountToByteCount(sampleCount);
                case NwCodec.Pcm16Bit:
                    return sampleCount * 2;
                case NwCodec.Pcm8Bit:
                    return sampleCount;
                default:
                    return 0;
            }
        }

        public static int BytesToSamples(int byteCount, NwCodec codec)
        {
            switch (codec)
            {
                case NwCodec.GcAdpcm:
                    return GcAdpcmMath.ByteCountToSampleCount(byteCount);
                case NwCodec.Pcm16Bit:
                    return byteCount / 2;
                case NwCodec.Pcm8Bit:
                    return byteCount;
                default:
                    return 0;
            }
        }

        public static IAudioFormat ToAudioStream(BxstmStructure structure)
        {
            if (structure.PrefetchData != null)
            {
                // Read only the first prefetch region for now
                PrefetchData pdat = structure.PrefetchData[0];
                structure.StreamInfo.Looping = false;
                structure.StreamInfo.SampleCount = pdat.SampleCount;
            }
            switch (structure.StreamInfo.Codec)
            {
                case NwCodec.GcAdpcm:
                    return ToAdpcmStream(structure);
                case NwCodec.Pcm16Bit:
                    return ToPcm16Stream(structure);
                case NwCodec.Pcm8Bit:
                    return ToPcm8Stream(structure);
                default:
                    return null;
            }
        }

        private static GcAdpcmFormat ToAdpcmStream(BxstmStructure structure)
        {
            StreamInfo streamInfo = structure.StreamInfo;
            List<GcAdpcmChannelInfo> channelInfo = structure.ChannelInfo.Channels;
            var channels = new GcAdpcmChannel[streamInfo.ChannelCount];

            for (int c = 0; c < channels.Length; c++)
            {
                var channelBuilder = new GcAdpcmChannelBuilder(structure.AudioData[c], channelInfo[c].Coefs, streamInfo.SampleCount)
                {
                    Gain = channelInfo[c].Gain,
                    StartContext = channelInfo[c].Start
                };

                channelBuilder.WithLoop(streamInfo.Looping, streamInfo.LoopStart, streamInfo.SampleCount)
                    .WithLoopContext(streamInfo.LoopStart, channelInfo[c].Loop.PredScale,
                        channelInfo[c].Loop.Hist1, channelInfo[c].Loop.Hist2);

                if (structure.SeekTable != null)
                {
                    channelBuilder.WithSeekTable(structure.SeekTable[c], streamInfo.SamplesPerSeekTableEntry);
                }

                channels[c] = channelBuilder.Build();
            }

            return new GcAdpcmFormatBuilder(channels, streamInfo.SampleRate)
                .WithTracks(structure.TrackInfo?.Tracks)
                .WithLoop(streamInfo.Looping, streamInfo.LoopStart, streamInfo.SampleCount)
                .Build();
        }

        private static Pcm16Format ToPcm16Stream(BxstmStructure structure)
        {
            StreamInfo info = structure.StreamInfo;
            short[][] channels = structure.AudioData.Select(x => x.ToShortArray(structure.Endianness)).ToArray();
            return new Pcm16FormatBuilder(channels, info.SampleRate)
                .WithTracks(structure.TrackInfo?.Tracks)
                .WithLoop(info.Looping, info.LoopStart, info.SampleCount)
                .Build();
        }

        private static Pcm8SignedFormat ToPcm8Stream(BxstmStructure structure)
        {
            StreamInfo info = structure.StreamInfo;
            return new Pcm8FormatBuilder(structure.AudioData, info.SampleRate, true)
                .WithTracks(structure.TrackInfo?.Tracks)
                .WithLoop(info.Looping, info.LoopStart, info.SampleCount)
                .Build() as Pcm8SignedFormat;
        }

        private static NwVersion IncludeTrackInfoBfstm { get; } = new NwVersion(0, 2);
        private static NwVersion IncludeUnalignedLoopBfstm { get; } = new NwVersion(0, 4);
        private static NwVersion IncludeChecksumBfstm { get; } = new NwVersion(0, 5);

        //This one is weird. Some version 2.1 BCSTM files have the region offset, and some don't.
        private static NwVersion IncludeRegionBcstm { get; } = new NwVersion(2, 1);
        private static NwVersion IncludeTrackInfoBcstm { get; } = new NwVersion(2, 1);
        private static NwVersion IncludeUnalignedLoopBcstm { get; } = new NwVersion(2, 3);

        private static NwVersion IncludeUnalignedLoopBfwav { get; } = new NwVersion(0, 1, 2);
        private static NwVersion IncludeUnalignedLoopBcwav { get; } = new NwVersion(2, 1, 1);

        public static bool IncludeTrackInfo(NwVersion version)
        {
            return version.Major == 0 && version.Version <= IncludeTrackInfoBfstm.Version ||
                   version.Major >= 2 && version.Version <= IncludeTrackInfoBcstm.Version;
        }

        public static bool IncludeRegionInfo(NwVersion version)
        {
            return version.Major >= 2 && version.Version >= IncludeRegionBcstm.Version ||
                   version.Major == 0;
        }

        public static bool IncludeUnalignedLoop(NwVersion version)
        {
            return version.Major == 0 && version.Version >= IncludeUnalignedLoopBfstm.Version ||
                   version.Major >= 2 && version.Version >= IncludeUnalignedLoopBcstm.Version;
        }

        public static bool IncludeChecksum(NwVersion version)
        {
            return version.Major == 0 && version.Version >= IncludeChecksumBfstm.Version;
        }

        public static bool IncludeUnalignedLoopWave(NwVersion version)
        {
            return version.Major == 0 && version.Version >= IncludeUnalignedLoopBfwav.Version ||
                   version.Major >= 2 && version.Version >= IncludeUnalignedLoopBcwav.Version;
        }
    }
}
