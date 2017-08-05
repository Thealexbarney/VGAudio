using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers.Bxstm
{
    public class BfstmWriter : AudioWriter<BfstmWriter, BxstmConfiguration>
    {
        private BCFstmWriter Writer { get; set; }

        protected override int FileSize => Writer.FileSize;
        protected override void SetupWriter(AudioData audio)
        {
            Writer = new BCFstmWriter(Configuration, BCFstmWriter.BCFstmType.Bfstm);
            Writer.SetupWriter(audio);
        }

        protected override void WriteStream(Stream stream) => Writer.WriteStream(stream);
    }
}
