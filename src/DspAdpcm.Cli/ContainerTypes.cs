using System;
using System.Collections.Generic;
using System.IO;
using DspAdpcm.Containers;
using DspAdpcm.Formats;

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
            [FileType.Wave] = new ContainerType(new[] { "wav", "wave" }, WaveReader.Read, (a, s) => new WaveWriter().WriteToStream(a, s)),
            [FileType.Dsp] = new ContainerType(new[] { "dsp" }, DspReader.Read, (a, s) => new DspWriter().WriteToStream(a, s)),
            [FileType.Brstm] = new ContainerType(new[] { "brstm" }, BrstmReader.Read, (a, s) => new BrstmWriter().WriteToStream(a, s))
        };

        public static readonly Dictionary<string, FileType> Extensions =
            Containers.SelectMany(x => x.Value.Names.Select(y => new {y, x.Key}))
            .ToDictionary(x => x.y, x => x.Key);
    }

    internal class ContainerType
    {
        public ContainerType(IEnumerable<string> names, Func<Stream, AudioData> read, Action<AudioData, Stream> write)
        {
            Names = names;
            Read = read;
            Write = write;
        }

        public IEnumerable<string> Names { get; }
        public Func<Stream, AudioData> Read { get; }
        public Action<AudioData, Stream> Write { get; }
    }
}