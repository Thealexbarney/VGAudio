using System;
using System.IO;
using VGAudio.Containers;
using VGAudio.Containers.NintendoWare;
using VGAudio.Utilities;

namespace VGAudio.Tools.Rebuild
{
    internal class Rebuilder
    {
        private Func<IAudioReader> Reader { get; }
        private Func<IAudioWriter> Writer { get; }
        private string Path { get; }
        private string Filter { get; }
        public string[] Files { get; }
        public Result[] Results { get; }

        public Rebuilder(string path, string filter, Func<IAudioReader> reader, Func<IAudioWriter> writer)
        {
            Path = path;
            Filter = filter;
            Reader = reader;
            Writer = writer;
            Files = Directory.GetFiles(Path, Filter, SearchOption.AllDirectories);
            Results = new Result[Files.Length];
        }

        public Result[] Run()
        {
            Parallel.For(0, Files.Length, i =>
            {
                try
                {
                    byte[] inFile = File.ReadAllBytes(Files[i]);
                    AudioWithConfig audio = Reader().ReadWithConfig(inFile);

                    audio.Configuration.TrimFile = false;
                    if (audio.Configuration is BxstmConfiguration a)
                    {
                        a.RecalculateLoopContext = false;
                        a.RecalculateSeekTable = false;
                    }

                    int bytesDiff = 0;
                    if (Writer != null)
                    {
                        byte[] outFile = Writer().GetFile(audio.AudioFormat, audio.Configuration);
                        bytesDiff = Common.DiffArrays(inFile, outFile);
                    }
                    else
                    {
                        audio.AudioFormat.ToPcm16();
                    }
                    Results[i] = new Result(Files[i], bytesDiff);
                }
                catch (Exception ex)
                {
                    Results[i] = new Result(Files[i], -4, ex.Message);
                }
            });
            return Results;
        }
    }
}
