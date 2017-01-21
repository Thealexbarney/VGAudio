using System.IO;

namespace DspAdpcm.Containers
{
    public interface IWritableAudio
    {
        void WriteStream(AudioStream audio, Stream stream);
        byte[] GetFile(AudioStream audio);
    }
}
