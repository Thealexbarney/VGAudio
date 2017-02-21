using System;
using System.Collections.Generic;
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
                    switch (args[i].Split(':')[0].Substring(1).ToUpper())
                    {
                        case "I":
                            List<int> range = null;
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after -i switch.");
                                return null;
                            }
                            if (args[i].Length > 2 && args[i][2] == ':')
                            {
                                range = ParseIntRange(args[i].Substring(3));
                            }
                            options.InFiles.Add(new AudioFile { Path = args[i + 1], Channels = range });
                            i++;
                            continue;
                        case "O":
                            if (options.OutFiles.Count > 0)
                            {
                                PrintWithUsage("Can't set multiple outputs.");
                                return null;
                            }
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after -o switch.");
                                return null;
                            }
                            options.OutFiles.Add(new AudioFile { Path = args[i + 1] });
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

                if (options.InFiles.Count == 0)
                {
                    options.InFiles.Add(new AudioFile { Path = args[i] });
                    continue;
                }
                if (options.OutFiles.Count == 0)
                {
                    options.OutFiles.Add(new AudioFile { Path = args[i] });
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
            if (options.InFiles.Count == 0)
            {
                PrintWithUsage("Input file must be specified.");
                return false;
            }

            foreach (AudioFile file in options.InFiles)
            {
                if (file.Type != FileType.NotSet) continue;

                FileType inferredType = GetFileTypeFromName(file.Path);

                if (inferredType == FileType.NotSet)
                {
                    PrintWithUsage("Can't infer input file type from extension.");
                    return false;
                }

                file.Type = inferredType;
            }

            if (options.OutFiles.Count == 0)
            {
                var a = new AudioFile { Path = Path.GetFileNameWithoutExtension(options.InFiles[0].Path) + ".dsp" };
            }

            foreach (AudioFile file in options.OutFiles)
            {
                if (file.Type != FileType.NotSet) continue;

                FileType inferredType = GetFileTypeFromName(file.Path);

                if (inferredType == FileType.NotSet)
                {
                    PrintWithUsage("Can't infer output file type from extension.");
                    return false;
                }

                file.Type = inferredType;
            }

            return true;
        }

        private static FileType GetFileTypeFromName(string fileName)
        {
            FileType fileType;
            string extension = Path.GetExtension(fileName)?.TrimStart('.').ToLower() ?? "";
            ContainerTypes.Extensions.TryGetValue(extension, out fileType);
            return fileType;
        }

        private static List<int> ParseIntRange(string input)
        {
            var range = new List<int>();

            foreach (string s in input.Split(','))
            {
                int num;
                if (int.TryParse(s, out num))
                {
                    range.Add(num);
                    continue;
                }

                string[] subs = s.Split('-');
                int start, end;

                if (subs.Length > 1 &&
                    int.TryParse(subs[0], out start) &&
                    int.TryParse(subs[1], out end) &&
                    end >= start)
                {
                    for (int i = start; i <= end; i++)
                    {
                        range.Add(i);
                    }
                }
            }

            return range;
        }

        private static void PrintWithUsage(string toPrint)
        {
            Console.WriteLine(toPrint);
            PrintUsage();
        }

        private static void PrintUsage()
        {
            Console.WriteLine($"Usage: {GetProgramName()} [options] infile [-i infile2...] [outfile]\n");
            Console.WriteLine("  -i             Specify an input file");
            Console.WriteLine("  -i:#,#-#...    Specify an input file and the channels to use");
            Console.WriteLine("                 The index for channels is zero-based");
            Console.WriteLine("  -o             Specify the output file");
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
