using System;
using System.Diagnostics;
using VGAudio.Utilities;

namespace VGAudio.Codecs.CriAdx
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + ", nq}")]
    public class CriAdxKey
    {
        public CriAdxKey(int seed, int mult, int inc)
        {
            Seed = seed;
            Mult = mult;
            Inc = inc;
        }

        public CriAdxKey(ulong keyCode)
        {
            keyCode--;
            Seed = (int)(keyCode >> 27 & 0x7fff);
            Mult = (int)(keyCode >> 12 & 0x7ffc | 1);
            Inc = (int)(keyCode << 1 & 0x7fff | 1);
        }

        public CriAdxKey(string keyString)
        {
            if (string.IsNullOrEmpty(keyString)) return;
            KeyString = keyString;

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
        public string KeyString { get; }
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

        private string DebuggerDisplay => KeyString == null ? $"KeyCode = {KeyCode}" : $"KeyString = \"{KeyString}\"";
    }
}