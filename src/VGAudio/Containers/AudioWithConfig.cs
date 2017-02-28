using VGAudio.Formats;

namespace VGAudio.Containers
{
    public class AudioWithConfig
    {
        public AudioWithConfig(AudioData audio, IConfiguration configuration)
        {
            Audio = audio;
            Configuration = configuration;
        }

        public AudioData Audio { get; set; }
        public IConfiguration Configuration { get; set; }
    }
}