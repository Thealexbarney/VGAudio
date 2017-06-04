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
        public ConcurrentBag<AdxKey> Keys { get; } = new ConcurrentBag<AdxKey>();
        public int EncryptionType { get; }
        private HashSet<int> PossibleSeeds { get; }
        private int[] PossibleMultipliers { get; }
        private int[] PossibleIncrements { get; }
        private int XorMask { get; } = 0x7fff;
        private int ValidationMask { get; }
        private int MaxSeed { get; }
        private IProgressReport Progress { get; }

        public GuessAdx(string directory, IProgressReport progress = null)
        {
            Progress = progress;
            Progress?.ReportTotal(0x1000);
            EncryptionType = LoadFiles(directory);

            switch (EncryptionType)
            {
                case 8:
                    int[] primes = Common.GetPrimes(0x8000);
                    int start = ~Array.BinarySearch(primes, 0x4000);
                    PossibleMultipliers = new int[0x400];
                    Array.Copy(primes, start, PossibleMultipliers, 0, 0x400);

                    PossibleIncrements = PossibleMultipliers;
                    PossibleSeeds = new HashSet<int>(PossibleMultipliers);
                    ValidationMask = 0xE000;
                    MaxSeed = 0x8000;
                    break;
                case 9:
                    PossibleSeeds = new HashSet<int>(Enumerable.Range(0, 0x2000));
                    PossibleMultipliers = Enumerable.Range(0, 0x2000).Where(x => (x & 3) == 1).ToArray();
                    PossibleIncrements = Enumerable.Range(0, 0x2000).Where(x => (x & 1) == 1).ToArray();
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
                int scale = i;
                Parallel.ForEach(Files, adx =>
                {
                    TryScale(adx, scale);
                });
                Progress?.Report(i);
            }
        }

        public void AddKey(AdxKey key)
        {
            if (!TriedKeys.TryAdd(key, 0)) return;

            Progress?.ReportMessage($"Trying key {PrintKey(key)}");

            if (Files.Any(stream => !KeyIsValid(key, stream.Scales)))
            {
                Progress?.ReportMessage($"Key {PrintKey(key)} is invalid");
                return;
            }

            Progress?.ReportMessage($"Key {PrintKey(key)} could be valid. Calculating confidence...");
            var confidences = Files.AsParallel().Select(file => GetReencodeConfidence(key, file.Filename)).ToList();

            double confidenceSmall = 1 - (double)confidences.Sum(x => x.IdenticalBytesShort) / confidences.Sum(x => x.TotalBytesShort);
            double confidenceFull = 1 - (double)confidences.Sum(x => x.IdenticalBytesFull) / confidences.Sum(x => x.TotalBytesFull);

            Keys.Add(key);
            Progress?.ReportMessage($"{PrintKey(key)} Confidence Short - {confidenceSmall:P3} Full - {confidenceFull:P3}");

            string PrintKey(AdxKey k) => $"Seed - {k.Seed:x4} Multiplier - {k.Mult:x4} Increment - {k.Inc:x4}";
        }

        public void TryScale(AdxFile adx, int index)
        {
            int seed = (adx.Scales[adx.StartFrame] ^ index) & MaxSeed - 1;
            if (adx.StartFrame == 0 && !PossibleSeeds.Contains(seed)) return;

            foreach (int mult in PossibleMultipliers)
            {
                foreach (var inc in PossibleIncrements)
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
                        var key = FindStartingKey(seed, mult, inc, adx.StartFrame);
                        if (key != null) AddKey(key);
                    }
                }
            }
        }

        public AdxKey FindStartingKey(int seed, int mult, int inc, int startFrame)
        {
            if (startFrame == 0)
            {
                return new AdxKey(seed, mult, inc);
            }

            foreach (var realSeed in PossibleSeeds)
            {
                int xor = realSeed;

                for (int i = 0; i < startFrame; i++)
                {
                    xor = (xor * mult + inc) & XorMask;
                }

                if ((xor & MaxSeed - 1) == seed)
                {
                    return new AdxKey(realSeed, mult, inc);
                }
            }

            // No key found
            return null;
        }

        public bool KeyIsValid(AdxKey key, ushort[] scales)
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

        private AdxKeyConfidence GetReencodeConfidence(AdxKey key, string filename)
        {
            CriAdxFormat adx;
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                adx = (CriAdxFormat)new AdxReader().ReadFormat(stream);
            }

            CriAdxEncryption.Decrypt(adx.Channels.Select(x => x.Audio).ToArray(), key, adx.FrameSize);
            Pcm16Format pcm = adx.ToPcm16();
            CriAdxFormat adx2 = new CriAdxFormat().EncodeFromPcm16(pcm);

            var confidence = new AdxKeyConfidence();

            int toCompareFull = adx.Channels[0].Audio.Length;
            int toCompareShort = Math.Min(adx.FrameSize * 100, toCompareFull);

            for (int i = 0; i < adx.ChannelCount; i++)
            {
                confidence.TotalBytesFull += toCompareFull;
                confidence.TotalBytesShort += toCompareShort;
                confidence.IdenticalBytesFull += Common.DiffArrays(adx.Channels[i].Audio, adx2.Channels[i].Audio, toCompareFull);
                confidence.IdenticalBytesShort += Common.DiffArrays(adx.Channels[i].Audio, adx2.Channels[i].Audio, toCompareShort);
            }

            return confidence;
        }

        private class AdxKeyConfidence
        {
            public int TotalBytesFull { get; set; }
            public int IdenticalBytesFull { get; set; }
            public int TotalBytesShort { get; set; }
            public int IdenticalBytesShort { get; set; }
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

            for (int i = 0; i < audio.Length; i++)
            {
                if (audio[i] != 0)
                {
                    StartFrame = i / FrameSize;
                    break;
                }
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
