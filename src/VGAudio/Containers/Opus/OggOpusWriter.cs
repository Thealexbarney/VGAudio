using System.IO;
using Concentus.Oggfile;
using VGAudio.Codecs.Opus;
using VGAudio.Formats;
using VGAudio.Formats.Opus;

namespace VGAudio.Containers.Opus
{
    public class OggOpusWriter : AudioWriter<OggOpusWriter, NxOpusConfiguration>
    {
        private OpusFormat Format { get; set; }
        protected override int FileSize => -1;

        protected override void SetupWriter(AudioData audio)
        {
            var encodingConfig = new OpusParameters
            {
                Bitrate = Configuration.Bitrate,
                Progress = Configuration.Progress
            };

            Format = audio.GetFormat<OpusFormat>(encodingConfig);
        }

        protected override void WriteStream(Stream stream)
        {
            var oggStream = new OpusOggWriteStream(stream, Format.SampleRate, Format.ChannelCount, Format.PreSkipCount, Format.SampleCount);

            foreach (OpusFrame frame in Format.Frames)
            {
                oggStream.WriteSamples(frame);
            }

            oggStream.Finish();
        }
    }
}
