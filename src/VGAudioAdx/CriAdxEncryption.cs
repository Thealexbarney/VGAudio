using System;
using System.Linq.Expressions;
using VGAudio.Utilities;

// ReSharper disable once CheckNamespace
namespace VGAudio.Codecs
{
    public static class CriAdxEncryption
    {
        public static void Decrypt(byte[][] adpcm, AdxKey key, int frameSize)
        {
            for (int i = 0; i < adpcm.Length; i++)
            {
                DecryptChannel(adpcm[i], key, frameSize, i, adpcm.Length);
            }
        }

        public static void DecryptChannel(byte[] adpcm, AdxKey key, int frameSize, int channelNum, int channelCount)
        {
            int xor = key.Seed;
            int frameCount = adpcm.Length.DivideByRoundUp(frameSize);

            for (int i = 0; i < channelNum; i++)
            {
                xor = (xor * key.Mult + key.Inc) & 0x7fff;
            }

            for (int i = 0; i < frameCount; i++)
            {
                int pos = i * frameSize;
                if (adpcm[pos] != 0 || adpcm[pos + 1] != 0)
                {
                    adpcm[pos] ^= (byte)(xor >> 8);
                    adpcm[pos] &= 0x1f;
                    adpcm[pos + 1] ^= (byte)xor;
                }

                for (int c = 0; c < channelCount; c++)
                {
                    xor = (xor * key.Mult + key.Inc) & 0x7fff;
                }
            }
        }
    }

    public class AdxKey
    {
        public AdxKey(int seed, int mult, int inc)
        {
            Seed = seed;
            Mult = mult;
            Inc = inc;
        }

        public AdxKey(ulong keyCode)
        {
            keyCode--;
            Seed = (int)(keyCode >> 27 & 0x7fff);
            Mult = (int)(keyCode >> 12 & 0x7ffc | 1);
            Inc = (int)(keyCode << 1 & 0x7fff | 1);
        }

        public AdxKey(string keyString)
        {
            if (string.IsNullOrEmpty(keyString)) return;

            Seed = Primes[0x100];
            Mult = Primes[0x200];
            Inc = Primes[0x300];

            foreach (char c in keyString)
            {
                Seed = Primes[Seed * Primes[c + 0x80] % 0x400];
                Mult = Primes[Mult * Primes[c + 0x80] % 0x400];
                Inc = Primes[Inc * Primes[c + 0x80] % 0x400];
            }
        }

        public int Seed { get; }
        public int Mult { get; }
        public int Inc { get; }
        private static int[] Primes { get; } = BuildPrimesTable();

        public ulong KeyCode
        {
            get
            {
                ulong seed = (ulong)Seed << 27;
                ulong mult = (ulong)(Mult & 0xfffc) << 12;
                ulong inc = (ulong)Inc >> 1;
                return (seed | mult | inc) + 1;
            }
        }

        private static int[] BuildPrimesTable()
        {
            int[] primes = Helpers.GetPrimes(0x8000);
            int start = ~Array.BinarySearch(primes, 0x4000);
            var trimmedPrimes = new int[0x400];
            Array.Copy(primes, start, trimmedPrimes, 0, 0x400);
            return trimmedPrimes;
        }
    }
}
