using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VGAudio.Containers;
using VGAudio.Containers.Adx;
using VGAudio.Containers.Dsp;
using VGAudio.Containers.Genh;
using VGAudio.Containers.Hca;
using VGAudio.Containers.Hps;
using VGAudio.Containers.Idsp;
using VGAudio.Containers.NintendoWare;
using VGAudio.Containers.Wave;

namespace VGAudio.Uwp.Audio
{
    internal static class AudioInfo
    {
        public static readonly Dictionary<FileType, ContainerType> Containers = new Dictionary<FileType, ContainerType>
        {
            [FileType.Wave] = new ContainerType("WAVE", new[] { "wav" }, "WAVE Audio File", () => new WaveReader(), () => new WaveWriter()),
            [FileType.Dsp] = new ContainerType("DSP", new[] { "dsp" }, "Nintendo DSP ACPCM Audio File", () => new DspReader(), () => new DspWriter()),
            [FileType.Idsp] = new ContainerType("IDSP", new[] { "idsp" }, "IDSP Audio File", () => new IdspReader(), () => new IdspWriter()),
            [FileType.Brstm] = new ContainerType("BRSTM", new[] { "brstm" }, "BRSTM Audio File", () => new BrstmReader(), () => new BrstmWriter()),
            [FileType.Bcstm] = new ContainerType("BCSTM", new[] { "bcstm" }, "BCSTM Audio File", () => new BCFstmReader(), () => new BCFstmWriter(NwTarget.Ctr)),
            [FileType.Bfstm] = new ContainerType("BFSTM", new[] { "bfstm" }, "BFSTM Audio File", () => new BCFstmReader(), () => new BCFstmWriter(NwTarget.Cafe)),
            [FileType.Hps] = new ContainerType("HPS", new[] { "hps" }, "HPS Audio File", () => new HpsReader(), () => new HpsWriter()),
            [FileType.Adx] = new ContainerType("ADX", new[] { "adx" }, "CRI ADX Audio File", () => new AdxReader(), () => new AdxWriter()),
            [FileType.Hca] = new ContainerType("HCA", new[] { "hca" }, "CRI HCA Audio File", () => new HcaReader(), () => new HcaWriter()),
            [FileType.Genh] = new ContainerType("GENH", new[] { "genh" }, "GENH Audio File", () => new GenhReader(), null)
        };

        public static readonly Dictionary<string, FileType> Extensions =
            Containers.SelectMany(x => x.Value.Extensions.Select(y => new { y, x.Key }))
            .ToDictionary(x => x.y, x => x.Key);

        public static FileType GetFileTypeFromName(string fileName)
        {
            string extension = Path.GetExtension(fileName)?.TrimStart('.').ToLower() ?? "";
            Extensions.TryGetValue(extension, out FileType fileType);
            return fileType;
        }
    }

    public enum FileType
    {
        Unknown = 0,
        Wave,
        Dsp,
        Brstm,
        Bcstm,
        Bfstm,
        Idsp,
        Hps,
        Adx,
        Hca,
        Genh
    }

    public class ContainerType
    {
        public string DisplayName { get; }
        public IEnumerable<string> Extensions { get; }
        public string Description { get; }
        public Func<IAudioReader> GetReader { get; }
        public Func<IAudioWriter> GetWriter { get; }

        public ContainerType(string displayName, IEnumerable<string> extensions, string description, Func<IAudioReader> getReader, Func<IAudioWriter> getWriter)
        {
            DisplayName = displayName;
            Extensions = extensions.ToList();
            Description = description;
            GetReader = getReader;
            GetWriter = getWriter;
        }
    }
}