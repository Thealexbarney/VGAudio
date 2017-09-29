using VGAudio.Formats;

namespace VGAudio.Containers
{
    public class AudioWithConfig
    {
        public AudioWithConfig(IAudioFormat audioFormat, Configuration configuration)
        {
            AudioFormat = audioFormat;
            Configuration = configuration;
        }

        public IAudioFormat AudioFormat { get; }
        public AudioData Audio => new AudioData(AudioFormat);
        public Configuration Configuration { get; }
    }
}