using VGAudio.Formats;

namespace VGAudio.Containers
{
    public class AudioWithConfig
    {
        public AudioWithConfig(IAudioFormat audioFormat, IConfiguration configuration)
        {
            AudioFormat = audioFormat;
            Configuration = configuration;
        }

        public IAudioFormat AudioFormat { get; }
        public AudioData Audio => new AudioData(AudioFormat);
        public IConfiguration Configuration { get; }
    }
}