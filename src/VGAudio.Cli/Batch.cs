using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
// ReSharper disable AccessToDisposedClosure

namespace VGAudio.Cli
{
    internal static class Batch
    {
        public static bool BatchConvert(Options options)
        {
            if (options.Job != JobType.Batch) return false;

            SearchOption searchOption = options.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] files = ContainerTypes.ExtensionList
                .SelectMany(x => Directory.GetFiles(options.InDir, $"*.{x}", searchOption))
                .ToArray();

            using (var progress = new ProgressBar())
            {
                progress.SetTotal(files.Length);

                Parallel.ForEach(files,
                    new ParallelOptions { MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1) },
                    inPath =>
                    {
                        string relativePath = inPath.Substring(options.InDir.Length).TrimStart('\\');
                        string outPath = Path.ChangeExtension(Path.Combine(options.OutDir, relativePath), options.OutTypeName);

                        var jobFiles = new JobFiles();
                        jobFiles.InFiles.Add(new AudioFile(inPath));
                        jobFiles.OutFiles.Add(new AudioFile(outPath));

                        try
                        {
                            progress.LogMessage(Path.GetFileName(inPath));
                            Convert.ConvertFile(options, jobFiles, false);
                        }
                        catch (Exception ex)
                        {
                            progress.LogMessage($"Error converting {Path.GetFileName(inPath)}");
                            progress.LogMessage(ex.ToString());
                        }

                        progress.ReportAdd(1);
                    });
            }

            return true;
        }
    }
}
