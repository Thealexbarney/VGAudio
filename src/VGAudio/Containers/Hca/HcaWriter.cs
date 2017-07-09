using System;
using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers.Hca
{
    public class HcaWriter : AudioWriter<HcaWriter, Configuration>
    {
        protected override int FileSize => 0;
        protected override void SetupWriter(AudioData audio)
        {
            throw new NotImplementedException();
        }

        protected override void WriteStream(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
