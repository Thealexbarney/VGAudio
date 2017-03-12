using System.Collections.Generic;

namespace VGAudio.Cli.Metadata
{
    internal enum Formats
    {
        Pcm16,
        Pcm8,
        GcAdpcm
    }

    internal class FormatsDictionary
    {
        public static readonly Dictionary<Formats, string> Display = new Dictionary<Formats, string>
        {
            [Formats.Pcm16] = "16-bit PCM",
            [Formats.Pcm8] = "8-bit PCM",
            [Formats.GcAdpcm] = "GameCube \"DSP\" 4-bit ADPCM"
        };
    }
}
