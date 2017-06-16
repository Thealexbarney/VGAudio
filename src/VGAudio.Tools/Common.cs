using System.Collections.Generic;
using System.Linq;
using VGAudio.Containers;
using VGAudio.Containers.Adx;

namespace VGAudio.Tools
{
    internal static class Common
    {
        public static readonly Dictionary<FileType, FileTypeInfo> FileTypes = new[]
        {
            new FileTypeInfo(FileType.Wave, "*.wav", () => new WaveReader(), () => new WaveWriter()),
            new FileTypeInfo(FileType.Dsp, "*.dsp", () => new DspReader(), () => new DspWriter()),
            new FileTypeInfo(FileType.Idsp, "*.idsp", () => new IdspReader(), () => new IdspWriter()),
            new FileTypeInfo(FileType.Brstm, "*.brstm", () => new BrstmReader(), () => new BrstmWriter()),
            new FileTypeInfo(FileType.Bcstm, "*.bcstm", () => new BcstmReader(), () => new BcstmWriter()),
            new FileTypeInfo(FileType.Bfstm, "*.bfstm", () => new BfstmReader(), () => new BfstmWriter()),
            new FileTypeInfo(FileType.Hps, "*.hps", () => new HpsReader(), () => new HpsWriter()),
            new FileTypeInfo(FileType.Adx, "*.adx", () => new AdxReader(), () => new AdxWriter()),
            new FileTypeInfo(FileType.Genh, "*.genh", () => new GenhReader(), () => null)
        }.ToDictionary(x => x.Type, x => x);

        public static int[] GetPrimes(int maxPrime)
        {
            var sieve = new int[maxPrime];

            for (int i = 2; i < maxPrime; i++)
            {
                for (int j = i * i; j < maxPrime; j += i)
                {
                    sieve[j] = 1;
                }
            }

            var primes = new List<int>();
            for (int i = 0; i < maxPrime; i++)
            {
                if (sieve[i] == 0)
                {
                    primes.Add(i);
                }
            }

            return primes.ToArray();
        }

        internal static int DiffArrays(byte[] a1, byte[] a2, int bytesToCompare = -1)
        {
            if (a1 == null || a2 == null) return -1;
            if (a1 == a2) return -1;
            if (a1.Length != a2.Length) return -2;
            if (bytesToCompare < 0 || bytesToCompare > a1.Length)
                bytesToCompare = a1.Length;
            int byteCount = 0;

            for (int i = 0; i < bytesToCompare; i++)
            {
                if (a1[i] != a2[i])
                {
                    byteCount++;
                }
            }

            return byteCount;
        }
    }
}
