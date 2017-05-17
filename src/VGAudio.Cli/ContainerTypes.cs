using System;
using System.Collections.Generic;
using System.Linq;
using VGAudio.Containers;

namespace VGAudio.Cli
{
    internal static class ContainerTypes
    {
        public static readonly Dictionary<FileType, ContainerType> Containers = new Dictionary<FileType, ContainerType>
        {
            [FileType.Wave] = new ContainerType(new[] { "wav", "wave", "lwav" }, () => new WaveReader(), () => new WaveWriter(), CreateConfiguration.Wave),
            [FileType.Dsp] = new ContainerType(new[] { "dsp" }, () => new DspReader(), () => new DspWriter(), CreateConfiguration.Dsp),
            [FileType.Idsp] = new ContainerType(new[] { "idsp" }, () => new IdspReader(), () => new IdspWriter(), CreateConfiguration.Idsp),
            [FileType.Brstm] = new ContainerType(new[] { "brstm" }, () => new BrstmReader(), () => new BrstmWriter(), CreateConfiguration.Brstm),
            [FileType.Bcstm] = new ContainerType(new[] { "bcstm" }, () => new BcstmReader(), () => new BcstmWriter(), CreateConfiguration.Bcstm),
            [FileType.Bfstm] = new ContainerType(new[] { "bfstm" }, () => new BfstmReader(), () => new BfstmWriter(), CreateConfiguration.Bfstm),
            [FileType.Genh] = new ContainerType(new[] { "genh" }, () => new GenhReader(), null, null)
        };

        public static readonly Dictionary<string, FileType> Extensions =
            Containers.SelectMany(x => x.Value.Names.Select(y => new { y, x.Key }))
            .ToDictionary(x => x.y, x => x.Key);
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