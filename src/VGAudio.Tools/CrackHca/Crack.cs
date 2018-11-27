using System.Diagnostics;
using System.IO;
using System.Linq;
using VGAudio.Containers.Hca;
using VGAudio.Formats.CriHca;
using VGAudio.Utilities;

namespace VGAudio.Tools.CrackHca
{
    public class Crack
    {
        private IProgressReport Progress { get; }
        public string Path { get; }

        public Crack(string path, IProgressReport progress = null)
        {

            Progress = progress;

            if (Directory.Exists(path))
            {
                Path = path;
            }
            else
            {
                Progress?.LogMessage($"Directory {path} does not exist.");
            }
        }

        public void Run()
        {
            Progress?.LogMessage("Loading files...");
            Frequency[][] freq = LoadFrequencies(Path);
            Progress.SetTotal(0);

            Progress?.LogMessage("Cracking...");
            var solver = new Solver(freq);
            solver.Solve();
            Progress?.LogMessage($"Lower byte of key could be 0x{solver.UpperSeed + 1:X2}");
        }

        public Frequency[][] LoadFrequencies(string path)
        {
            string[] files = Directory.GetFiles(path, "*hca");
            var freq = Helpers.CreateJaggedArray<Frequency[][]>(30, 0x100);
            var counts = Helpers.CreateJaggedArray<int[][]>(30, 0x100);
            int total = 0;
            Progress.SetTotal(files.Length);

            var reader = new HcaReader { Decrypt = false };

            foreach (string file in files)
            {
                CriHcaFormat hca;
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    hca = (CriHcaFormat)reader.ReadFormat(stream);
                }

                foreach (byte[] frame in hca.AudioData)
                {
                    for (int position = 0; position < 30; position++)
                    {
                        counts[position][frame[position]]++;
                    }
                    total++;
                }

                Progress.ReportAdd(1);
            }

            for (int position = 0; position < 30; position++)
            {
                for (int value = 0; value < 0x100; value++)
                {
                    freq[position][value] = new Frequency((byte)value, (float)counts[position][value] / total);
                }
                freq[position] = freq[position].OrderByDescending(x => x.Freq).ToArray();
            }

            return freq;
        }

        [DebuggerDisplay("{Value} - {Freq}")]
        public class Frequency
        {
            public Frequency(byte value, float freq)
            {
                Value = value;
                Freq = freq;
            }

            public byte Value { get; }
            public float Freq { get; }
        }
    }
}
