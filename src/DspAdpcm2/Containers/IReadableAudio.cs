using System.IO;

namespace DspAdpcm.Containers
{
    public interface IReadableAudio
    {
        AudioStream Read(Stream stream);
        AudioStream Read(byte[] file);
    }
}
