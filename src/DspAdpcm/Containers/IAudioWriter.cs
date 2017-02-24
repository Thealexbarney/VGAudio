using System.IO;
using DspAdpcm.Formats;

namespace DspAdpcm.Containers
{
    public interface IAudioWriter
    {
        void WriteToStream(IAudioFormat audio, Stream stream, IConfiguration configuration = null);
        byte[] GetFile(IAudioFormat audio, IConfiguration configuration = null);

        void WriteToStream(AudioData audio, Stream stream, IConfiguration configuration = null);
        byte[] GetFile(AudioData audio, IConfiguration configuration = null);
    }
}
