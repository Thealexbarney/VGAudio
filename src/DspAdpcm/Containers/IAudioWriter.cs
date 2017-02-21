using System.IO;
using DspAdpcm.Formats;

namespace DspAdpcm.Containers
{
    public interface IAudioWriter
    {
        void WriteToStream(IAudioFormat audio, Stream stream);
        byte[] GetFile(IAudioFormat audio);

        void WriteToStream(AudioData audio, Stream stream);
        byte[] GetFile(AudioData audio);
    }
}
