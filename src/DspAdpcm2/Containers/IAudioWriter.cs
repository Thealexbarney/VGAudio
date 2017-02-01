using System.IO;

namespace DspAdpcm.Containers
{
    public interface IAudioWriter
    {
        void WriteToStream(AudioStream audio, Stream stream);
        byte[] GetFile(AudioStream audio);
    }
}
