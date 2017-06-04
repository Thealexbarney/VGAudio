// ReSharper disable once CheckNamespace
namespace VGAudio.Formats
{
    public class CriAdxChannel
    {
        public byte[] Audio { get; }
        public short History { get; }
        public int Version { get; }

        public CriAdxChannel(byte[] audio, short history = 0, int version = 0)
        {
            Audio = audio;
            History = history;
            Version = version;
        }
    }
}
