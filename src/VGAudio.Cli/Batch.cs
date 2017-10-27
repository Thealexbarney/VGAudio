using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VGAudio.Containers;

namespace VGAudio.Cli
{
    internal static class Batch
    {
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public static bool BatchConvert(Options options)
        {
            if (options.Job != JobType.Batch) return false;

            SearchOption searchOption = options.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            ContainerType outType = ContainerTypes.Writable[options.OutTypeName];
            string[] files = ContainerTypes.ExtensionList
                .SelectMany(x => Directory.GetFiles(options.InDir, $"*.{x}", searchOption))
                .ToArray();

            Directory.CreateDirectory(options.OutDir);

            using (var progress = new ProgressBar())
            {
                progress.SetTotal(files.Length);

                Parallel.ForEach(files, inPath =>
                {
                    string inName = Path.GetFileName(inPath);
                    ContainerType inType = ContainerTypes.Containers[CliArguments.GetFileTypeFromName(inPath)];
                    string relativePath = inPath.Substring(options.InDir.Length).TrimStart('\\');
                    string outName = Path.ChangeExtension(Path.Combine(options.OutDir, relativePath), options.OutTypeName);

                    try
                    {
                        progress.LogMessage(inName);

                        AudioWithConfig inAudio;
                        using (var stream = new FileStream(inPath, FileMode.Open, FileAccess.Read))
                        {
                            inAudio = inType.GetReader().ReadWithConfig(stream);
                        }
                        Configuration configuration = outType.GetConfiguration(options, inAudio.Configuration);

                        using (var stream = new FileStream(outName, FileMode.Create, FileAccess.ReadWrite))
                        {
                            outType.GetWriter().WriteToStream(inAudio.Audio, stream, configuration);
                        }
                    }
                    catch (Exception ex)
                    {
                        progress.LogMessage($"Error converting {inName}");
                        progress.LogMessage(ex.ToString());
                    }

                    progress.ReportAdd(1);
                });
            }

            return true;
        }
    }
}
