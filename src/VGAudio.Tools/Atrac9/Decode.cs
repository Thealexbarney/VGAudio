using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VGAudio.Containers.At9;

namespace VGAudio.Tools.Atrac9
{
    public class Decode
    {
        private string[] Files { get; }
        private IAt9Tool DecoderA { get; }
        private IAt9Tool DecoderB { get; }

        public Decode(string[] files, IAt9Tool decoderA, IAt9Tool decoderB)
        {
            Files = files;
            DecoderA = decoderA;
            DecoderB = decoderB;
        }

        public ParallelQuery<Result> Run()
        {
            return Files.AsParallel().Select(path =>
            {
                try
                {
                    return CompareDecoding(path);
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.Any(x => x.GetType() == typeof(FileNotFoundException)))
                    {
                        return new Result { Filename = path, Channel = -1, Invalid = true };
                    }
                    return new Result { Filename = path, Channel = -1, Exception = ex.InnerExceptions[0] };
                }
                catch (Exception ex)
                {
                    return new Result { Filename = path, Channel = -1, Exception = ex };
                }
            });
        }

        public Result CompareDecoding(string path)
        {
            short[][] pcmA = null;
            short[][] pcmB = null;

            Parallel.Invoke(
                () => pcmA = DecoderA.Decode(path),
                () => pcmB = DecoderB.Decode(path)
            );

            int frameSize = GetFrameSize(path);

            for (int i = 0; i < pcmA.Length; i++)
            {
                int equal = ArraysEqual(pcmA[i], pcmB[i]);
                if (equal != -1)
                {
                    return new Result
                    {
                        Equal = false,
                        Filename = path,
                        Channel = i,
                        Sample = equal,
                        Frame = equal / frameSize,
                        FrameSample = equal % frameSize
                    };
                }
            }

            return new Result { Equal = true, Filename = path };
        }

        private static int GetFrameSize(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                At9Structure meta = new At9Reader().ReadMetadata(stream);
                return meta.Config.FrameSamples;
            }
        }

        private static int ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (a1 == null || a2 == null) return -2;
            if (a1 == a2) return -1;
            if (a1.Length != a2.Length) return -3;

            for (int i = 0; i < a1.Length; i++)
            {
                if (!a1[i].Equals(a2[i]))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
