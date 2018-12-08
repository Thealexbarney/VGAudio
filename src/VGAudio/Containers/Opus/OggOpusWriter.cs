using System.IO;
using Concentus.Oggfile;
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
            Format = audio.GetFormat<OpusFormat>();
        }

        protected override void WriteStream(Stream stream)
        {
            var oggStream = new OpusOggWriteStream(Format.SampleRate, Format.ChannelCount, stream);

            foreach (var frame in Format.Frames)
            {
                oggStream.WriteSamples(frame);
            }

            oggStream.Finish();
        }
    }
}
