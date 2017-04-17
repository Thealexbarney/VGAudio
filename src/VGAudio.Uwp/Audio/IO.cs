using System.IO;
using VGAudio.Containers;
using VGAudio.Formats;

namespace VGAudio.Uwp.Audio
{
    public static class IO
    {
        public static IAudioFormat OpenFile(string path)
        {
            FileType type = AudioInfo.GetFileTypeFromName(path);
            IAudioReader reader = AudioInfo.Containers[type].GetReader();

            using (var stream = new FileStream(path, FileMode.Open))
            {
                return reader.ReadFormat(stream);
            }
        }

        public static void SaveFile(AudioData audio, string path)
        {

        }
    }
}