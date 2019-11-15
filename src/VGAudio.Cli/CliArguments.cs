using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VGAudio.Codecs.CriAdx;
using VGAudio.Codecs.CriHca;
using VGAudio.Containers.Opus;
using VGAudio.Utilities;

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
                        case "C" when i == 0:
                        case "-CONVERT" when i == 0:
                            options.Job = JobType.Convert;
                            continue;
                        case "B" when i == 0:
                        case "-BATCH" when i == 0:
                            options.Job = JobType.Batch;
                            continue;
                        case "M" when i == 0:
                        case "-METADATA" when i == 0:
                            options.Job = JobType.Metadata;
                            continue;
                        case "H" when i == 0:
                        case "-HELP" when i == 0:
                            PrintUsage();
                            return null;
                        case "-VERSION" when i == 0:
                            Console.WriteLine($"VGAudio v{GetProgramVersion()}");
                            return null;
                        case "I" when options.Job == JobType.Batch:
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after -i switch.");
                                return null;
                            }
                            options.InDir = args[i + 1];
                            i++;
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
                        case "O" when options.Job == JobType.Convert:
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
                        case "O" when options.Job == JobType.Batch:
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after -o switch.");
                                return null;
                            }
                            options.OutDir = args[i + 1];
                            i++;
                            continue;
                        case "R":
                            options.Recurse = true;
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
                            AudioFormat format = GetFormat(args[i + 1]);
                            if (format == AudioFormat.None)
                            {
                                PrintWithUsage("Format must be one of pcm16, pcm8, or GcAdpcm");
                                return null;
                            }

                            options.OutFormat = format;
                            i++;
                            continue;
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
                        case "-OUT-FORMAT":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --out-format.");
                                return null;
                            }

                            options.OutTypeName = args[i + 1];
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
                        case "-BIG-ENDIAN":
                            options.Endianness = Endianness.BigEndian;
                            continue;
                        case "-LITTLE-ENDIAN":
                            options.Endianness = Endianness.LittleEndian;
                            continue;
                        case "-OPUSHEADER":
                            if (i + 1 >= args.Length)
                            {
                                PrintWithUsage("No argument after --OpusHeader");
                                return null;
                            }
                            string headerType = args[i + 1];
                            NxOpusHeaderType nxHeaderType;

                            switch (headerType.ToUpper())
                            {
                                case "STANDARD":
                                    nxHeaderType = NxOpusHeaderType.Standard;
                                    break;
                                case "NAMCO":
                                    nxHeaderType = NxOpusHeaderType.Namco;
                                    break;
                                case "KTSS":
                                    nxHeaderType = NxOpusHeaderType.Ktss;
                                    break;
                                default:
                                    Console.WriteLine("Invalid header type");
                                    return null;
                            }

                            options.NxOpusHeaderType = nxHeaderType;
                            i++;
                            continue;
                        case "-CBR":
                            options.EncodeCbr = true;
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
            if (options.Job == JobType.Batch)
            {
                if (string.IsNullOrWhiteSpace(options.InDir))
                {
                    PrintWithUsage("Input directory must be specified.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(options.OutDir))
                {
                    PrintWithUsage("Output directory must be specified.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(options.OutTypeName))
                {
                    PrintWithUsage("Output file type must be specified.");
                    return false;
                }

                if (!ContainerTypes.Writable.ContainsKey(options.OutTypeName))
                {
                    Console.WriteLine("Output type not available. Available types are:");
                    Console.WriteLine(string.Join(", ", ContainerTypes.WritableList));
                    return false;
                }

                return true;
            }

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
                string ext = options.InFiles[0].Type != FileType.Wave ? ".wav" : ".dsp";
                options.OutFiles.Add(new AudioFile { Path = Path.GetFileNameWithoutExtension(options.InFiles[0].Path) + ext });
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

        public static FileType GetFileTypeFromName(string fileName)
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
            string opusHeaderTypes = string.Join(", ", Enum.GetNames(typeof(NxOpusHeaderType)).Select(x => x));

            Console.WriteLine($"Usage: {GetProgramName()} [mode] [options] infile [-i infile2...] [outfile]");
            Console.WriteLine("\nAvailable Modes:");
            Console.WriteLine("If no mode is specified, a single-file conversion is performed");
            Console.WriteLine("  -c, --convert    Single-file conversion mode");
            Console.WriteLine("  -b, --batch      Batch conversion mode");
            Console.WriteLine("  -m, --metadata   Print file metadata");
            Console.WriteLine("  -h, --help       Display this help and exit");
            Console.WriteLine("      --version    Display version information and exit");

            Console.WriteLine("\nCommon Options:");
            Console.WriteLine("  -i <file>        Specify an input file");
            Console.WriteLine("  -l <start-end>   Set the start and end loop points");
            Console.WriteLine("                   Loop points are given in zero-indexed samples");
            Console.WriteLine("      --no-loop    Sets the audio to not loop");
            Console.WriteLine("  -f               Specify the audio format to use in the output file");

            Console.WriteLine("\nConvert Options:");
            Console.WriteLine("  -i:#,#-# <file>  Specify an input file and the channels to use");
            Console.WriteLine("                   The index for channels is zero-based");
            Console.WriteLine("  -o               Specify the output file");

            Console.WriteLine("\nBatch Options:");
            Console.WriteLine("  -i  <dir>        Specify the input directory");
            Console.WriteLine("  -o  <dir>        Specify the output directory");
            Console.WriteLine("  -r               Recurse subdirectories");
            Console.WriteLine("      --out-format The file type or container to save files as");

            Console.WriteLine("\nBCSTM/BFSTM Options:");
            Console.WriteLine("      --little-endian   Makes the output file little-endian");
            Console.WriteLine("      --big-endian      Makes the output file big-endian");

            Console.WriteLine("\nADX Options:");
            Console.WriteLine("      --adxtype    The ADX encoding type to use");
            Console.WriteLine("      --framesize  ADPCM frame size to use for ADX files");
            Console.WriteLine("      --keystring  String to use for ADX type 8 encryption");
            Console.WriteLine("      --keycode    Number to use for ADX type 9 encryption");
            Console.WriteLine("                   Between 1-18446744073709551615");
            Console.WriteLine("      --filter     Filter to use for fixed-coefficient ADX encoding [0-3]");
            Console.WriteLine("      --version #  ADX header version to write [3,4]");

            Console.WriteLine("\nHCA Options:");
            Console.WriteLine("      --hcaquality     The quality level to use for the HCA file");
            Console.WriteLine("      --bitrate        The bitrate in bits per second of the output HCA file");
            Console.WriteLine("                       --bitrate takes precedence over --hcaquality");
            Console.WriteLine("      --limit-bitrate  This flag sets a limit on how low the bitrate can go");
            Console.WriteLine("                       This limit depends on the properties of the input file");

            Console.WriteLine("\nSwitch Opus Options:");
            Console.WriteLine("      --bitrate        The bitrate in bits per second of the output file");
            Console.WriteLine("      --cbr            Encode the file using a constant bitrate");
            Console.WriteLine("      --opusheader     The type of header to use for the generated Opus file");
            Console.WriteLine("                       Available types: " + opusHeaderTypes);
        }

        private static string GetProgramName() => Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location ?? "");

        private static string GetProgramVersion()
        {
            Version version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
