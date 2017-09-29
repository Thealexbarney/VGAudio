using System;
using VGAudio.Containers;

namespace VGAudio.Tools
{
    internal enum FileType
    {
        NotSet = 0,
        Wave,
        Dsp,
        Idsp,
        Brstm,
        Bcstm,
        Bfstm,
        Hps,
        Genh,
        Adx,
        Hca
    }

    internal class FileTypeInfo
    {
        public FileType Type { get; }
        public string Extension { get; }
        public Func<IAudioReader> GetReader { get; }
        public Func<IAudioWriter> GetWriter { get; }

        public FileTypeInfo(FileType type, string extension, Func<IAudioReader> getReader, Func<IAudioWriter> getWriter)
        {
            Type = type;
            Extension = extension;
            GetReader = getReader;
            GetWriter = getWriter;
        }
    }
}