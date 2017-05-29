using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VGAudio.Codecs;
using VGAudio.Containers;
using VGAudio.Containers.Adx;
using VGAudio.Formats;
using VGAudio.Formats.Adx;

namespace VGAudio.TestsLong.CrackAdx
{
    public class GuessAdx
    {
        public List<AdxFile> Files { get; } = new List<AdxFile>();
        public List<ushort> StartScales { get; } = new List<ushort>();
        public ConcurrentDictionary<AdxKey, int> TriedKeys { get; } = new ConcurrentDictionary<AdxKey, int>(new AdxKeyComparer());
        public List<AdxKey> Keys { get; } = new List<AdxKey>();
        public int[] Primes { get; }

        public GuessAdx(string directory)
        {
            LoadFiles(directory);
            Primes = Common.GetPrimes(0x8000);
        }

        public void LoadFiles(string directory)
        {
            string[] files = Directory.GetFiles(directory, "*.adx");

            foreach (string file in files)
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var adx = new AdxReader().ReadMetadata(stream);
                    var audioDataLength = adx.SampleCount / CriAdxHelpers.NibbleCountToSampleCount(adx.FrameSize * 2, adx.FrameSize) * adx.FrameSize * adx.ChannelCount;
                    var audioData = new byte[audioDataLength];
                    stream.Position = adx.CopyrightOffset + 4;
                    stream.Read(audioData, 0, audioDataLength);
                    Files.Add(new AdxFile(adx, audioData, file));
                }
            }

            StartScales.AddRange(Files.Where(x => x.StartFrame == 0).Select(x => x.Scales[0]).OrderBy(x => x));
        }

        public void Run()
        {
            for (int i = 0; i < 0x1000; i++)
            {
                int index = i;
                Parallel.ForEach(Files, adx =>
                {
                    TryIndex(adx, index);
                });
                Console.WriteLine(i);
            }
        }

        public void AddKey(AdxKey key)
        {
            if (!TriedKeys.TryAdd(key, 0)) return;

            Console.WriteLine($"Trying key {key}");

            if (Files.Any(stream => !TryKey(key, stream.Scales)))
            {
                Console.WriteLine($"Key {key} is invalid");
                return;
            }

            Console.WriteLine($"Key {key} is valid. Calculating confidence...");
            var diffs = Files.AsParallel().SelectMany(file => Reencode(key, file.Filename));

            var confidenceSmall = 1 - diffs.Average(x => x.Item1);
            var confidenceFull = 1 - diffs.Average(x => x.Item2);

            Keys.Add(key);
            Console.WriteLine($"{key} Confidence Short - {confidenceSmall:P3} Full - {confidenceFull:P5}");
        }

        public void TryIndex(AdxFile adx, int index)
        {
            int seed = adx.Scales[adx.StartFrame] ^ index;

            foreach (int mult in Primes)
            {
                foreach (var inc in Primes)
                {
                    int xor = seed;
                    bool match = true;
                    for (int i = adx.StartFrame; i < adx.Scales.Length; i++)
                    {
                        ushort scale = adx.Scales[i];
                        if (((scale ^ xor) & 0xE000) != 0 && scale != 0)
                        {
                            match = false;
                            break;
                        }
                        xor = (xor * mult + inc) & 0x7fff;
                    }

                    if (match)
                    {
                        AddKey(GetKey(seed, mult, inc, adx.StartFrame));
                    }
                }
            }
        }

        public AdxKey GetKey(int seed, int mult, int inc, int startFrame)
        {
            if (startFrame == 0)
            {
                return new AdxKey(seed, mult, inc);
            }

            for (int realSeed = 0; realSeed < 0x8000; realSeed++)
            {
                int xor = realSeed;

                for (int i = 0; i < startFrame; i++)
                {
                    xor = (xor * mult + inc) & 0x7fff;
                }

                if (xor == seed)
                {
                    return new AdxKey(realSeed, mult, inc);
                }
            }

            // No key found
            return new AdxKey(seed, mult, inc);
        }

        public bool TryKey(AdxKey key, ushort[] scales)
        {
            int xor = key.Seed;
            foreach (ushort scale in scales)
            {
                if (((scale ^ xor) & 0xe000) != 0 && scale != 0)
                {
                    return false;
                }
                xor = (xor * key.Mult + key.Inc) & 0x7fff;
            }
            return true;
        }

        public List<Tuple<double, double>> Reencode(AdxKey key, string filename)
        {
            CriAdxFormat adx;
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                adx = (CriAdxFormat)new AdxReader().ReadFormat(stream);
            }

            CriAdxCodec.Decrypt(adx.Channels, key, adx.FrameSize);
            Pcm16Format pcm = adx.ToPcm16();
            CriAdxFormat adx2 = new CriAdxFormat().EncodeFromPcm16(pcm);

            var diffs = new List<Tuple<double, double>>();

            int toCompareFull = adx.Channels[0].Length;
            int toCompareSmall = Math.Min(adx.FrameSize * 100, toCompareFull);

            for (int i = 0; i < adx.ChannelCount; i++)
            {
                var smallDiff = (double)Common.DiffArrays(adx.Channels[i], adx2.Channels[i], toCompareSmall) / toCompareSmall;
                var fullDiff = (double)Common.DiffArrays(adx.Channels[i], adx2.Channels[i], toCompareFull) / toCompareFull;
                diffs.Add(new Tuple<double, double>(smallDiff, fullDiff));
            }

            return diffs;
        }
    }

    public class AdxFile
    {
        public ushort[] Scales { get; }
        public int FrameSize { get; }
        public int FrameCount { get; }
        public int StartFrame { get; }
        public AdxStructure Metadata { get; }
        public string Filename { get; }

        public AdxFile(AdxStructure metadata, byte[] audio, string filename)
        {
            Metadata = metadata;
            Filename = filename;
            FrameSize = metadata.FrameSize;
            FrameCount = audio.Length / FrameSize;

            Scales = new ushort[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                int pos = i * FrameSize;
                Scales[i] = (ushort)(audio[pos] << 8 | audio[pos + 1]);
            }

            for (StartFrame = 0; StartFrame < FrameCount; StartFrame++)
            {
                if (Scales[StartFrame] != 0)
                    break;
            }
        }
    }

    public class AdxKeyConfidence
    {
        public double Small { get; set; }
        public double Medium { get; set; }
        public double All { get; set; }
    }

    public sealed class AdxKeyComparer : IEqualityComparer<AdxKey>
    {
        public bool Equals(AdxKey x, AdxKey y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Seed == y.Seed && x.Mult == y.Mult && x.Inc == y.Inc;
        }

        public int GetHashCode(AdxKey obj)
        {
            unchecked
            {
                int hashCode = obj.Seed;
                hashCode = (hashCode * 397) ^ obj.Mult;
                hashCode = (hashCode * 397) ^ obj.Inc;
                return hashCode;
            }
        }
    }
}
