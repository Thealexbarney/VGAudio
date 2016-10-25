using System;
using System.IO;
using System.Reflection;

namespace DspAdpcm.Cli
{
    internal class CliArguments
    {
        public static Options Parse(string[] args)
        {
            var options = new Options();

            for (int i = 0; i < args.Length; i++)
            {
                if (string.IsNullOrEmpty(args[i])) continue;

                if (args[i][0] == '-' || args[i][0] == '/')
                {
                    switch (args[i].Substring(1).ToUpper())
                    {
                        case "I":
                            if (options.InFilePath != null)
                            {
                                PrintWithUsage("Can't set multiple inputs.");
                                return null;
                            }
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after -i switch.");
                                return null;
                            }
                            options.InFilePath = args[i + 1];
                            i++;
                            continue;
                        case "O":
                            if (options.OutFilePath != null)
                            {
                                PrintWithUsage("Can't set multiple outputs.");
                                return null;
                            }
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after -o switch.");
                                return null;
                            }
                            options.OutFilePath = args[i + 1];
                            i++;
                            continue;
                        case "L":
                            if (options.NoLoop)
                            {
                                PrintWithUsage("Can't set loop points while using --no-loop.");
                                return null;
                            }

                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after -l switch.");
                                return null;
                            }

                            string[] loopPoints = args[i + 1].Split('-');
                            if (loopPoints.Length != 2)
                            {
                                PrintWithUsage("-l switch requires two loop points in the format <start>-<end>.");
                                return null;
                            }

                            int loopStart, loopEnd;
                            if (!(int.TryParse(loopPoints[0], out loopStart) && int.TryParse(loopPoints[1], out loopEnd)))
                            {
                                PrintWithUsage("Error parsing loop points.");
                                return null;
                            }

                            options.Loop = true;
                            options.LoopStart = loopStart;
                            options.LoopEnd = loopEnd;
                            i++;
                            continue;
                        case "-NO-LOOP":
                            if (options.Loop)
                            {
                                PrintWithUsage("Can't set loop points while using --no-loop.");
                                return null;
                            }

                            options.NoLoop = true;
                            continue;
                        case "-HELP":
                        case "H":
                            PrintUsage();
                            return null;
                        case "-VERSION":
                            Console.WriteLine($"DspAdpcm v{GetProgramVersion()}");
                            return null;
                    }
                }

                if (options.InFilePath == null)
                {
                    options.InFilePath = args[i];
                    continue;
                }
                if (options.OutFilePath == null)
                {
                    options.OutFilePath = args[i];
                    continue;
                }

                PrintWithUsage($"Unknown parameter: {args[i]}");
                return null;
            }

            if (!ValidateFileNameAndType(options)) return null;

            return options;
        }

        private static bool ValidateFileNameAndType(Options options)
        {
            if (string.IsNullOrEmpty(options.InFilePath))
            {
                PrintWithUsage("Input file must be specified.");
                return false;
            }

            if (options.InFileType == FileType.NotSet)
            {
                options.InFileType = GetFileTypeFromName(options.InFilePath);
            }

            if (options.InFileType == FileType.NotSet)
            {
                PrintWithUsage("Can't determine input file type from extension.");
                return false;
            }

            if (string.IsNullOrEmpty(options.OutFilePath))
            {
                options.OutFilePath = Path.GetFileNameWithoutExtension(options.InFilePath) + ".dsp";
            }

            if (options.OutFileType == FileType.NotSet)
            {
                options.OutFileType = GetFileTypeFromName(options.OutFilePath);
            }

            if (options.OutFileType == FileType.NotSet)
            {
                PrintWithUsage("Can't determine output file type from extension.");
                return false;
            }

            return true;
        }

        private static FileType GetFileTypeFromName(string fileName)
        {
            switch (Path.GetExtension(fileName)?.ToLower())
            {
                case ".wav":
                case ".wave":
                    return FileType.Wave;
                case ".dsp":
                    return FileType.Dsp;
                case ".idsp":
                    return FileType.Idsp;
                case ".brstm":
                    return FileType.Brstm;
                case ".bcstm":
                    return FileType.Bcstm;
                case ".bfstm":
                    return FileType.Bfstm;
                case ".genh":
                    return FileType.Genh;
                default:
                    return FileType.NotSet;
            }
        }

        private static void PrintWithUsage(string toPrint)
        {
            Console.WriteLine(toPrint);
            PrintUsage();
        }

        private static void PrintUsage()
        {
            Console.WriteLine($"Usage: {GetProgramName()} [options] infile [outfile]\n");
            Console.WriteLine("  -i             Specify an input file");
            Console.WriteLine("  -o             Specify an output file");
            Console.WriteLine("  -l <start-end> Set the start and end loop points");
            Console.WriteLine("                 Loop points are given in zero-based samples");
            Console.WriteLine("      --no-loop  Sets the audio to not loop");
            Console.WriteLine("  -h, --help     Display this help and exit");
            Console.WriteLine("      --version  Display version information and exit");
        }

        private static string GetProgramName() => Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location ?? "");

        private static string GetProgramVersion()
        {
            Version version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
