using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers
{
    public interface IAudioWriter
    {
        void WriteToStream(IAudioFormat audio, Stream stream, IConfiguration configuration = null);
        byte[] GetFile(IAudioFormat audio, IConfiguration configuration = null);

        void WriteToStream(AudioData audio, Stream stream, IConfiguration configuration = null);
        byte[] GetFile(AudioData audio, IConfiguration configuration = null);
    }
}
