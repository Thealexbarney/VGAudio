using System;
using System.Collections.Generic;
using System.Linq;
using VGAudio.Containers;
using VGAudio.Containers.Adx;
using VGAudio.Containers.At9;
using VGAudio.Containers.Dsp;
using VGAudio.Containers.Genh;
using VGAudio.Containers.Hca;
using VGAudio.Containers.Hps;
using VGAudio.Containers.Idsp;
using VGAudio.Containers.NintendoWare;
using VGAudio.Containers.Opus;
using VGAudio.Containers.Wave;

namespace VGAudio.Cli
{
    internal static class ContainerTypes
    {
        public static readonly Dictionary<FileType, ContainerType> Containers = new Dictionary<FileType, ContainerType>
        {
            [FileType.Wave] = new ContainerType(new[] { "wav", "wave", "lwav" }, () => new WaveReader(), () => new WaveWriter(), CreateConfiguration.Wave),
            [FileType.Dsp] = new ContainerType(new[] { "dsp", "mdsp" }, () => new DspReader(), () => new DspWriter(), CreateConfiguration.Dsp),
            [FileType.Idsp] = new ContainerType(new[] { "idsp" }, () => new IdspReader(), () => new IdspWriter(), CreateConfiguration.Idsp),
            [FileType.Brstm] = new ContainerType(new[] { "brstm" }, () => new BrstmReader(), () => new BrstmWriter(), CreateConfiguration.Bxstm),
            [FileType.Bcstm] = new ContainerType(new[] { "bcstm" }, () => new BCFstmReader(), () => new BCFstmWriter(NwTarget.Ctr), CreateConfiguration.Bxstm),
            [FileType.Bfstm] = new ContainerType(new[] { "bfstm" }, () => new BCFstmReader(), () => new BCFstmWriter(NwTarget.Cafe), CreateConfiguration.Bxstm),
            [FileType.Brwav] = new ContainerType(new[] { "brwav", "rwav" }, () => new BrwavReader(), null, CreateConfiguration.Bxstm),
            [FileType.Bcwav] = new ContainerType(new[] { "bcwav", "cwav" }, () => new BCFstmReader(), null, CreateConfiguration.Bxstm),
            [FileType.Bfwav] = new ContainerType(new[] { "bfwav" }, () => new BCFstmReader(), null, CreateConfiguration.Bxstm),
            [FileType.Bcstp] = new ContainerType(new[] { "bcstp" }, () => new BCFstmReader(), null, CreateConfiguration.Bxstm),
            [FileType.Bfstp] = new ContainerType(new[] { "bfstp" }, () => new BCFstmReader(), null, CreateConfiguration.Bxstm),
            [FileType.Hps] = new ContainerType(new[] { "hps" }, () => new HpsReader(), () => new HpsWriter(), CreateConfiguration.Hps),
            [FileType.Adx] = new ContainerType(new[] { "adx" }, () => new AdxReader(), () => new AdxWriter(), CreateConfiguration.Adx),
            [FileType.Hca] = new ContainerType(new[] { "hca" }, () => new HcaReader(), () => new HcaWriter(), CreateConfiguration.Hca),
            [FileType.Genh] = new ContainerType(new[] { "genh" }, () => new GenhReader(), null, null),
            [FileType.Atrac9] = new ContainerType(new[] { "at9" }, () => new At9Reader(), null, null),
            [FileType.NxOpus] = new ContainerType(new[] { "lopus", "nop", "ktss", "kns" }, () => new NxOpusReader(), () => new NxOpusWriter(), CreateConfiguration.NxOpus),
            [FileType.OggOpus] = new ContainerType(new[] { "opus" }, () => new OggOpusReader(), () => new OggOpusWriter(), CreateConfiguration.NxOpus)
        };

        public static readonly Dictionary<string, FileType> Extensions =
            Containers.SelectMany(x => x.Value.Names.Select(y => new { y, x.Key }))
            .ToDictionary(x => x.y, x => x.Key);

        public static readonly string[] ExtensionList = Extensions.Select(x => x.Key).ToArray();

        public static readonly Dictionary<string, FileType> Writable =
            Containers.Where(x => x.Value.GetWriter != null)
                .SelectMany(x => x.Value.Names.Select(y => new { y, x.Key }))
                .ToDictionary(x => x.y, x => x.Key);

        public static readonly string[] WritableList =
            Containers.Where(x => x.Value.GetWriter != null)
            .SelectMany(x => x.Value.Names)
            .ToArray();
    }

    internal class ContainerType
    {
        public ContainerType(IEnumerable<string> names, Func<IAudioReader> getReader, Func<IAudioWriter> getWriter, Func<Options, Configuration, Configuration> getConfiguration)
        {
            Names = names;
            GetReader = getReader;
            GetWriter = getWriter;
            GetConfiguration = getConfiguration;
        }

        public IEnumerable<string> Names { get; }
        public Func<IAudioReader> GetReader { get; }
        public Func<IAudioWriter> GetWriter { get; }
        public Func<Options, Configuration, Configuration> GetConfiguration { get; }
    }
}