using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VGAudio.Containers;

namespace VGAudio.Uwp.Audio
{
    internal static class AudioInfo
    {
        public static readonly Dictionary<FileType, FileTypeInfo> FileTypes = new Dictionary<FileType, FileTypeInfo>
        {
            [FileType.Wave] = new FileTypeInfo("WAVE", new[] { "wav" }, "WAVE Audio File", () => new WaveReader(), () => new WaveWriter()),
            [FileType.Dsp] = new FileTypeInfo("DSP", new[] { "dsp" }, "Nintendo DSP ACPCM Audio File", () => new DspReader(), () => new DspWriter()),
            [FileType.Brstm] = new FileTypeInfo("BRSTM", new[] { "brstm" }, "BRSTM Audio File", () => new BrstmReader(), () => new BrstmWriter())
        };

        public static readonly Dictionary<string, FileType> Extensions =
            FileTypes.SelectMany(x => x.Value.Extensions.Select(y => new { y, x.Key }))
            .ToDictionary(x => x.y, x => x.Key);

        public static FileType GetFileTypeFromName(string fileName)
        {
            FileType fileType;
            string extension = Path.GetExtension(fileName)?.TrimStart('.').ToLower() ?? "";
            Extensions.TryGetValue(extension, out fileType);
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
        Genh
    }

    public class FileTypeInfo
    {
        public string DisplayName { get; }
        public IEnumerable<string> Extensions { get; }
        public string Description { get; }
        public Func<IAudioReader> GetReader { get; }
        public Func<IAudioWriter> GetWriter { get; }

        public FileTypeInfo(string displayName, IEnumerable<string> extensions, string description, Func<IAudioReader> getReader, Func<IAudioWriter> getWriter)
        {
            DisplayName = displayName;
            Extensions = extensions.ToList();
            Description = description;
            GetReader = getReader;
            GetWriter = getWriter;
        }
    }
}