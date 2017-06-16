using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers.Bxstm
{
    public class BcstmWriter : AudioWriter<BcstmWriter, BcstmConfiguration>
    {
        private BCFstmWriter Writer { get; set; }

        protected override int FileSize => Writer.FileSize;
        protected override void SetupWriter(AudioData audio)
        {
            Writer = new BCFstmWriter(Configuration);
            Writer.SetupWriter(audio);
        }

        protected override void WriteStream(Stream stream) => Writer.WriteStream(stream);
    }
}
