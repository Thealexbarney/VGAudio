using System.IO;
using VGAudio.Containers.At9;
using VGAudio.Formats;

namespace VGAudio.Tools.Atrac9
{
    public class At9ToolVGAudio : IAt9Tool
    {
        public short[][] Decode(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                IAudioFormat at9 = new At9Reader().ReadFormat(stream);
                return at9.ToPcm16().Channels;
            }
        }
    }
}
