using System.IO;

namespace DspAdpcm.Containers
{
    public interface IAudioReader
    {
        AudioStream Read(Stream stream);
        AudioStream Read(byte[] file);
    }
}
