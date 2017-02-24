using System;
using System.Collections.Generic;
using DspAdpcm.Containers;

#if NET20
using DspAdpcm.Compatibility.LinqBridge;
using DspAdpcm.Compatibility.Serialization;
#else
using System.Linq;
#endif

namespace DspAdpcm.Cli
{
    internal static class ContainerTypes
    {
        public static readonly Dictionary<FileType, ContainerType> Containers = new Dictionary<FileType, ContainerType>
        {
            [FileType.Wave] = new ContainerType(new[] { "wav", "wave" }, () => new WaveReader(), () => new WaveWriter()),
            [FileType.Dsp] = new ContainerType(new[] { "dsp" }, () => new DspReader(), () => new DspWriter()),
            [FileType.Idsp] = new ContainerType(new[] { "idsp" }, () => new IdspReader(), () => new IdspWriter()),
            [FileType.Brstm] = new ContainerType(new[] { "brstm" }, () => new BrstmReader(), () => new BrstmWriter()),
            [FileType.Bcstm] = new ContainerType(new[] { "bcstm" }, () => new BcstmReader(), () => new BcstmWriter()),
            [FileType.Bfstm] = new ContainerType(new[] { "bfstm" }, () => new BfstmReader(), () => new BfstmWriter()),
            [FileType.Genh] = new ContainerType(new[] { "genh" }, () => new GenhReader(), null)
        };

        public static readonly Dictionary<string, FileType> Extensions =
            Containers.SelectMany(x => x.Value.Names.Select(y => new {y, x.Key}))
            .ToDictionary(x => x.y, x => x.Key);
    }

    internal class ContainerType
    {
        public ContainerType(IEnumerable<string> names, Func<IAudioReader> getReader, Func<IAudioWriter> getWriter)
        {
            Names = names;
            GetReader = getReader;
            GetWriter = getWriter;
        }

        public IEnumerable<string> Names { get; }
        public Func<IAudioReader> GetReader { get; }
        public Func<IAudioWriter> GetWriter { get; }
    }
}