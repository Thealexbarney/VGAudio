using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers
{
    public interface IAudioWriter
    {
        void WriteToStream(IAudioFormat audio, Stream stream, Configuration configuration = null);
        byte[] GetFile(IAudioFormat audio, Configuration configuration = null);

        void WriteToStream(AudioData audio, Stream stream, Configuration configuration = null);
        byte[] GetFile(AudioData audio, Configuration configuration = null);
    }
}
