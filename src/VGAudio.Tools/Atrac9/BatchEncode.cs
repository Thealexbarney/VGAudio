using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VGAudio.Containers.Wave;

namespace VGAudio.Tools.Atrac9
{
    public class BatchEncode
    {
        private string OutDir { get; }
        private string InFile { get; }
        private string At9ToolPath { get; }
        private WaveStructure Wave { get; }
        private EncodeParams[] Params { get; set; }

        public BatchEncode(string outDir, string inFile, string at9ToolPath)
        {
            OutDir = outDir;
            InFile = inFile;
            At9ToolPath = at9ToolPath;
            using (var stream = new FileStream(InFile, FileMode.Open, FileAccess.Read))
            {
                Wave = new WaveReader().ReadMetadata(stream);
            }
        }

        public void Run(IProgressReport progress)
        {
            Params = GenerateCombinations();
            progress.SetTotal(Params.Length);
            Parallel.ForEach(Params, p =>
            {
                Encode(p, progress);
            });
        }

        private void Encode(EncodeParams encode, IProgressReport progress)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = At9ToolPath,
                    Arguments = encode.BuildCommand(InFile, OutDir),
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            process.WaitForExit();
            progress.ReportAdd(1);
        }

        private EncodeParams[] GenerateCombinations()
        {
            int[] bitrates = Bitrates1Ch;
            switch (Wave.ChannelCount)
            {
                case 1:
                    bitrates = Bitrates1Ch;
                    break;
                case 2:
                    bitrates = Bitrates2Ch;
                    break;
                case 4:
                    bitrates = Bitrates4Ch;
                    break;
                case 6:
                    bitrates = Bitrates6Ch;
                    break;
                case 8:
                    bitrates = Bitrates8Ch;
                    break;
            }
            object[][] sets =
            {
                bitrates.Cast<object>().ToArray(),
                NBandsValues.Cast<object>().ToArray(),
                IsBandValues.Cast<object>().ToArray(),
                GradModeValues.Cast<object>().ToArray(),
                DualModeOptions.Cast<object>().ToArray(),
                SuperFrameOptions.Cast<object>().ToArray(),
                WideBandOptions.Cast<object>().ToArray(),
                SlcOptions.Cast<object>().ToArray(),
                Options.Cast<object>().ToArray()
            };

            return CartesianProduct(sets).Select(x => x.ToArray()).Select(x =>
                new EncodeParams
                {
                    ChannelCount = Wave.ChannelCount,
                    BitRate = (int)x[0],
                    BandCount = (int)x[1],
                    IsBand = (int)x[2],
                    GradMode = (int)x[3],
                    DualMode = (bool)x[4],
                    SuperFrame = (bool)x[5],
                    WideBand = (bool)x[6],
                    Slc = (bool)x[7],
                    BandExtension = (bool)x[8]
                }).Where(x => x.IsValid()).ToArray();
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
                emptyProduct,
                (accumulator, sequence) =>
                    from accseq in accumulator
                    from item in sequence
                    select accseq.Concat(new[] { item }));
        }

        private static readonly bool[] DualModeOptions = { false, true };
        private static readonly bool[] SuperFrameOptions = { false, true };
        private static readonly bool[] WideBandOptions = { false, true };
        private static readonly bool[] SlcOptions = { false, true };
        private static readonly bool[] Options = { false, true };
        private static readonly int[] GradModeValues = { 0, 1, 2, 3, 4 };
        private static readonly int[] NBandsValues = { 5, 12, 18 };
        private static readonly int[] IsBandValues = { -1, 3, 8, 14 };
        //private static readonly int[] GradModeValues = { 0, 1, 2, 3, 4 };
        //private static readonly int[] NBandsValues = { 5, 6, 7, 8, 9, 10 };
        //private static readonly int[] IsBandValues = { -1, 3, 4, 5, 6, 7, 8, 9, 10 };

        private static readonly int[] Bitrates1Ch = { 36, 48, 60, 72, 84, 96, 120, 144 };
        private static readonly int[] Bitrates2Ch = { 72, 96, 120, 144, 168, 192, 240, 288 };
        private static readonly int[] Bitrates4Ch = { 192, 240, 288, 384 };
        private static readonly int[] Bitrates6Ch = { 240, 300, 360, 480 };
        private static readonly int[] Bitrates8Ch = { 336, 420, 504, 672 };

        private class EncodeParams
        {
            public int ChannelCount { get; set; }
            public bool BandExtension { get; set; }
            public bool SuperFrame { get; set; }
            public bool DualMode { get; set; }
            public bool WideBand { get; set; }
            public bool Slc { get; set; }
            public int BandCount { get; set; }
            public int IsBand { get; set; }
            public int BitRate { get; set; }
            public int GradMode { get; set; }

            public bool IsValid()
            {
                if (ChannelCount != 2 && DualMode) return false;
                if (ChannelCount < 5 && Slc) return false;
                if (ChannelCount > 2 && BandExtension) return false;
                if (IsBand > BandCount) return false;
                if (BandExtension && WideBand) return false;
                if (BandExtension && (BandCount < 5 || BandCount > 10)) return false;
                if (BandExtension && ChannelCount == 1 && BitRate > 72) return false;
                if (BandExtension && ChannelCount == 2 && BitRate > 144) return false;
                //if (!BandExtension || !SuperFrame) return false;
                //if (!BandExtension || DualMode || !SuperFrame || GradientMode != 4) return false;

                return true;
            }

            public string BuildCommand(string inFile, string outDir)
            {
                var sb = new StringBuilder("-e");
                if (BandExtension) sb.Append(" -bex");
                if (!SuperFrame) sb.Append(" -supframeoff");
                if (DualMode) sb.Append(" -dual");
                if (WideBand) sb.Append(" -wband");
                if (Slc) sb.Append(" -slc");
                sb.Append($" -br {BitRate}");
                sb.Append($" -nbands {BandCount}");
                sb.Append($" -isband {IsBand}");
                sb.Append($" -gradmode {GradMode}");
                sb.Append($" {inFile}");
                sb.Append($" {Path.Combine(outDir, BuildFilename(inFile))}");
                return sb.ToString();
            }

            public string BuildFilename(string infile)
            {
                infile = Path.GetFileNameWithoutExtension(infile);
                var sb = new StringBuilder(infile);
                sb.Append($"_br{BitRate:D3}");
                sb.Append($"_nb{BandCount:D2}");
                sb.Append($"_ib{Math.Max(IsBand, 0):D2}");
                sb.Append($"_g{GradMode:D2}");
                sb.Append($"_bex{(BandExtension ? 1 : 0)}");
                sb.Append($"_sf{(SuperFrame ? 1 : 0)}");
                sb.Append($"_d{(DualMode ? 1 : 0)}");
                sb.Append($"_wb{(WideBand ? 1 : 0)}");
                sb.Append($"_slc{(Slc ? 1 : 0)}");
                sb.Append(".at9");
                return sb.ToString();
            }
        }
    }
}
