using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers.NintendoWare
{
    public class BcstmWriter : AudioWriter<BcstmWriter, BxstmConfiguration>
    {
        private BCFstmWriter Writer { get; set; }

        protected override int FileSize => Writer.FileSize;
        protected override void SetupWriter(AudioData audio)
        {
            Writer = new BCFstmWriter(Configuration, BCFstmWriter.BCFstmType.Bcstm);
            Writer.SetupWriter(audio);
        }

        protected override void WriteStream(Stream stream) => Writer.WriteStream(stream);
    }
}
