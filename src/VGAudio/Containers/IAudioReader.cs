using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers
{
    public interface IAudioReader
    {
        IAudioFormat ReadFormat(Stream stream);
        IAudioFormat ReadFormat(byte[] file);
        AudioData Read(Stream stream);
        AudioData Read(byte[] file);
        AudioWithConfig ReadWithConfig(Stream stream);
        AudioWithConfig ReadWithConfig(byte[] file);
    }
}
