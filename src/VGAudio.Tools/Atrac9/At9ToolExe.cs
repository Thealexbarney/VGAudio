using System.Diagnostics;
using System.IO;
using VGAudio.Containers.Wave;
using VGAudio.Formats.Pcm16;

namespace VGAudio.Tools.Atrac9
{
    public class At9ToolExe : IAt9Tool
    {
        public string At9ToolPath { get; }

        public At9ToolExe(string directory)
        {
            At9ToolPath = Path.Combine(directory, "at9tool.exe");

            if (!File.Exists(At9ToolPath))
            {
                throw new FileNotFoundException($"Could not find {At9ToolPath}");
            }
        }

        public short[][] Decode(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException($"Could not find {filename}");
            }
            var tempOut = Path.GetTempFileName();

            try
            {
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = At9ToolPath,
                        Arguments = $"-d -repeat 1 \"{filename}\" \"{tempOut}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };
                process.Start();
                process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                using (var stream = new FileStream(tempOut, FileMode.Open, FileAccess.Read))
                {
                    if (stream.Length == 0)
                    {
                        throw new FileNotFoundException();
                    }
                    var pcmFormat = (Pcm16Format)new WaveReader().ReadFormat(stream);
                    return pcmFormat.Channels;
                }
            }
            finally
            {
                File.Delete(tempOut);
            }
        }
    }
}
