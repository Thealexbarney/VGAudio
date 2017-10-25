using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using VGAudio.Codecs.CriAdx;
using VGAudio.Codecs.CriHca;

namespace VGAudio.Cli
{
    internal static class CliArguments
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
                        case "-VERSION" when args.Length == 1:
                            Console.WriteLine($"VGAudio v{GetProgramVersion()}");
                            return null;
                        case "M":
                            options.Job = JobType.Metadata;
                            continue;
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

                            if (!(int.TryParse(loopPoints[0], out int loopStart) && int.TryParse(loopPoints[1], out int loopEnd)))
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
                        case "-LOOP-ALIGN":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --loop-align.");
                                return null;
                            }
                            if (!int.TryParse(args[i + 1], out int align))
                            {
                                PrintWithUsage("Error parsing loop alignment.");
                                return null;
                            }

                            options.LoopAlignment = align;
                            i++;
                            continue;
                        case "F":
                            if (options.OutFormat != AudioFormat.None)
                            {
                                PrintWithUsage("Can't set multiple formats.");
                                return null;
                            }
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after -f switch.");
                                return null;
                            }
                            var format = GetFormat(args[i + 1]);
                            if (format == AudioFormat.None)
                            {
                                PrintWithUsage("Format must be one of pcm16, pcm8, or GcAdpcm");
                                return null;
                            }

                            options.OutFormat = format;
                            i++;
                            continue;
                        case "-HELP":
                        case "H":
                            PrintUsage();
                            return null;
                        case "-VERSION":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --version.");
                                return null;
                            }
                            if (!int.TryParse(args[i + 1], out int version))
                            {
                                PrintWithUsage("Error parsing version.");
                                return null;
                            }

                            options.Version = version;
                            i++;
                            continue;
                        case "-FRAMESIZE":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --FrameSize.");
                                return null;
                            }
                            if (!int.TryParse(args[i + 1], out int framesize))
                            {
                                PrintWithUsage("Error parsing frame size.");
                                return null;
                            }

                            options.FrameSize = framesize;
                            i++;
                            continue;
                        case "-FILTER":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --filter.");
                                return null;
                            }
                            if (!int.TryParse(args[i + 1], out int filter))
                            {
                                PrintWithUsage("Error parsing filter value.");
                                return null;
                            }

                            options.Filter = filter;
                            i++;
                            continue;
                        case "-ADXTYPE":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --AdxType.");
                                return null;
                            }
                            string type = args[i + 1];
                            CriAdxType adxType;

                            switch (type.ToUpper())
                            {
                                case "LINEAR":
                                    adxType = CriAdxType.Linear;
                                    break;
                                case "FIXED":
                                    adxType = CriAdxType.Fixed;
                                    break;
                                case "EXP":
                                case "EXPONENTIAL":
                                    adxType = CriAdxType.Exponential;
                                    break;
                                default:
                                    Console.WriteLine("Valid ADX types are Linear, Fixed, or Exp(onential)");
                                    return null;
                            }

                            options.AdxType = adxType;
                            i++;
                            continue;
                        case "-KEYSTRING":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --keystring.");
                                return null;
                            }

                            options.KeyString = args[i + 1];
                            i++;
                            continue;
                        case "-KEYCODE":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --keycode.");
                                return null;
                            }
                            if (!ulong.TryParse(args[i + 1], out ulong keycode))
                            {
                                PrintWithUsage("Error parsing key code.");
                                return null;
                            }

                            options.KeyCode = keycode;
                            i++;
                            continue;
                        case "-HCAQUALITY":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --hcaquality.");
                                return null;
                            }

                            string quality = args[i + 1];
                            CriHcaQuality hcaQuality;

                            switch (quality.ToUpper())
                            {
                                case "HIGHEST":
                                    hcaQuality = CriHcaQuality.Highest;
                                    break;
                                case "HIGH":
                                    hcaQuality = CriHcaQuality.High;
                                    break;
                                case "MIDDLE":
                                    hcaQuality = CriHcaQuality.Middle;
                                    break;
                                case "LOW":
                                    hcaQuality = CriHcaQuality.Low;
                                    break;
                                case "LOWEST":
                                    hcaQuality = CriHcaQuality.Lowest;
                                    break;
                                default:
                                    Console.WriteLine("Valid qualities are Highest, High, Middle, Low, or Lowest.");
                                    return null;
                            }

                            options.HcaQuality = hcaQuality;
                            i++;
                            continue;
                        case "-BITRATE":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --bitrate.");
                                return null;
                            }
                            if (!int.TryParse(args[i + 1], out int bitrate))
                            {
                                PrintWithUsage("Error parsing bitrate.");
                                return null;
                            }

                            options.Bitrate = bitrate;
                            i++;
                            continue;
                        case "-LIMIT-BITRATE":
                            options.LimitBitrate = true;
                            continue;
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
                options.OutFiles.Add(new AudioFile { Path = Path.GetFileNameWithoutExtension(options.InFiles[0].Path) + ".dsp" });
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
            string extension = Path.GetExtension(fileName)?.TrimStart('.').ToLower() ?? "";
            ContainerTypes.Extensions.TryGetValue(extension, out FileType fileType);
            return fileType;
        }

        private static List<int> ParseIntRange(string input)
        {
            var range = new List<int>();

            foreach (string s in input.Split(','))
            {
                if (int.TryParse(s, out int num))
                {
                    range.Add(num);
                    continue;
                }

                string[] subs = s.Split('-');

                if (subs.Length > 1 &&
                    int.TryParse(subs[0], out int start) &&
                    int.TryParse(subs[1], out int end) &&
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

        private static AudioFormat GetFormat(string format)
        {
            switch (format.ToLower())
            {
                case "pcm16": return AudioFormat.Pcm16;
                case "pcm8": return AudioFormat.Pcm8;
                case "gcadpcm": return AudioFormat.GcAdpcm;
                default: return AudioFormat.None;
            }
        }

        private static void PrintWithUsage(string toPrint)
        {
            Console.WriteLine(toPrint);
            PrintUsage();
        }

        private static void PrintUsage()
        {
            Console.WriteLine($"Usage: {GetProgramName()} [options] infile [-i infile2...] [outfile]\n");
            Console.WriteLine("  -i <file>        Specify an input file");
            Console.WriteLine("  -i:#,#-# <file>  Specify an input file and the channels to use");
            Console.WriteLine("                   The index for channels is zero-based");
            Console.WriteLine("  -o               Specify the output file");
            Console.WriteLine("  -m               Print file metadata");
            Console.WriteLine("  -l <start-end>   Set the start and end loop points");
            Console.WriteLine("                   Loop points are given in zero-based samples");
            Console.WriteLine("      --no-loop    Sets the audio to not loop");
            Console.WriteLine("  -f               Specify the audio format to use in the output file");
            Console.WriteLine("  -h, --help       Display this help and exit");
            Console.WriteLine("      --version    Display version information and exit");

            Console.WriteLine("\nADX Options:");
            Console.WriteLine("      --adxtype    The ADX encoding type to use");
            Console.WriteLine("      --framesize  ADPCM frame size to use for ADX files");
            Console.WriteLine("      --keystring  String to use for ADX type 8 encryption");
            Console.WriteLine("      --keycode    Number to use for ADX type 9 encryption");
            Console.WriteLine("                   Between 1-18446744073709551615");
            Console.WriteLine("      --filter     Filter to use for fixed coefficient ADX encoding [0-3]");
            Console.WriteLine("      --version #  ADX header version to write [3,4]");

            Console.WriteLine("\nHCA Options:");
            Console.WriteLine("      --hcaquality     The quality level to use for the HCA file");
            Console.WriteLine("      --bitrate        The bitrate in bps of the output HCA file");
            Console.WriteLine("                       --bitrate takes precedence over --hcaquality");
            Console.WriteLine("      --limit-bitrate  This flag sets a limit on how low the bitrate can go");
            Console.WriteLine("                       This limit depends on the properties of the input file");
        }

        private static string GetProgramName() => Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location ?? "");

        private static string GetProgramVersion()
        {
            Version version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
