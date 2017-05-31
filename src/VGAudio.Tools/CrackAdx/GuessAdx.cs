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

namespace VGAudio.Tools.CrackAdx
{
    public class GuessAdx
    {
        public List<AdxFile> Files { get; } = new List<AdxFile>();
        public ConcurrentDictionary<AdxKey, int> TriedKeys { get; } = new ConcurrentDictionary<AdxKey, int>(new AdxKeyComparer());
        public List<AdxKey> Keys { get; } = new List<AdxKey>();
        public int EncryptionType { get; }
        public int[] PossibleValues { get; }
        public int XorMask { get; } = 0x7fff;
        public int ValidationMask { get; }
        public int MaxSeed { get; }


        public GuessAdx(string directory)
        {
            EncryptionType = LoadFiles(directory);

            switch (EncryptionType)
            {
                case 8:
                    PossibleValues = Common.GetPrimes(0x8000);
                    ValidationMask = 0xE000;
                    MaxSeed = 0x8000;
                    break;
                case 9:
                    PossibleValues = Enumerable.Range(0, 0x2000).ToArray();
                    ValidationMask = 0x1000;
                    MaxSeed = 0x2000;
                    break;
            }
        }

        private int LoadFiles(string directory)
        {
            string[] files = Directory.GetFiles(directory, "*.adx");

            int type = 0;
            foreach (string file in files)
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var adx = new AdxReader().ReadMetadata(stream);
                    if (type == 0) type = adx.Flags;
                    if (adx.Flags != type) continue;

                    var audioData = new byte[adx.AudioDataLength];
                    stream.Position = adx.CopyrightOffset + 4;
                    stream.Read(audioData, 0, adx.AudioDataLength);
                    Files.Add(new AdxFile(adx, audioData, file));
                }
            }

            return type;
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
            Console.WriteLine($"{key} Confidence Short - {confidenceSmall:P3} Full - {confidenceFull:P3}");
        }

        public void TryIndex(AdxFile adx, int index)
        {
            int seed = (adx.Scales[adx.StartFrame] ^ index) & MaxSeed - 1;

            foreach (int mult in PossibleValues)
            {
                foreach (var inc in PossibleValues)
                {
                    int xor = seed;
                    bool match = true;
                    for (int i = adx.StartFrame; i < adx.Scales.Length; i++)
                    {
                        ushort scale = adx.Scales[i];
                        if (((scale ^ xor) & ValidationMask) != 0 && scale != 0)
                        {
                            match = false;
                            break;
                        }
                        xor = (xor * mult + inc) & XorMask;
                    }

                    if (match)
                    {
                        var key = GetKey(seed, mult, inc, adx.StartFrame);
                        if (key != null) AddKey(key);
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

            for (int realSeed = 0; realSeed < MaxSeed; realSeed++)
            {
                int xor = realSeed;

                for (int i = 0; i < startFrame; i++)
                {
                    xor = (xor * mult + inc) & XorMask;
                }

                if (xor == seed)
                {
                    return new AdxKey(realSeed, mult, inc);
                }
            }

            // No key found
            return null;
        }

        public bool TryKey(AdxKey key, ushort[] scales)
        {
            int xor = key.Seed;
            foreach (ushort scale in scales)
            {
                if (((scale ^ xor) & ValidationMask) != 0 && scale != 0)
                {
                    return false;
                }
                xor = (xor * key.Mult + key.Inc) & XorMask;
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
        public string Filename { get; }

        public AdxFile(AdxStructure metadata, byte[] audio, string filename)
        {
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
