using System;
using System.Collections.Generic;
using System.IO;
using VGAudio.Containers;
using VGAudio.Formats;

namespace VGAudio.Uwp.Audio
{
    public static class IO
    {
        public static List<IAudioFormat> OpenFiles(string path)
        {
            var output = new List<IAudioFormat>();

            FileType type = AudioInfo.GetFileTypeFromName(path);
            IAudioReader reader = AudioInfo.FileTypes[type].GetReader();

            using (var stream = new FileStream(path, FileMode.Open))
            {
                output.Add(reader.ReadFormat(stream));
            }

            return output;
        }

        public static void SaveFile(AudioData audio, string path)
        {

        }
    }
}