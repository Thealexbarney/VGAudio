using System.Collections.Generic;
using DspAdpcm.Utilities;

namespace DspAdpcm.Formats.GcAdpcm
{
    internal class GcAdpcmSeekTable
    {
        private Dictionary<int, SeekTable> SeekTables { get; } = new Dictionary<int, SeekTable>();
        private GcAdpcmChannel Adpcm { get; }

        public GcAdpcmSeekTable(GcAdpcmChannel adpcmParent)
        {
            Adpcm = adpcmParent;
        }

        public void AddSeekTable(short[] table, int samplesPerEntry)
            => SeekTables[samplesPerEntry] = new SeekTable(table, false);

        public short[] GetSeekTable(int samplesPerEntry, bool ensureSelfCalculated)
        {
            SeekTable table;

            if (SeekTables.TryGetValue(samplesPerEntry, out table) && !(ensureSelfCalculated && !table.IsSelfCalculated))
            {
                return table.Table;
            }

            CalculateSeekTable(samplesPerEntry);
            return SeekTables[samplesPerEntry].Table;
        }

        private void CalculateSeekTable(int samplesPerEntry)
        {
            var audio = Adpcm.GetPcmAudio(true);
            int EntryCount = Adpcm.SampleCount.DivideByRoundUp(samplesPerEntry);
            short[] table = new short[EntryCount * 2];

            for (int i = 0; i < EntryCount; i++)
            {
                table[i * 2] = audio[i * samplesPerEntry + 1];
                table[i * 2 + 1] = audio[i * samplesPerEntry];
            }

            SeekTables[samplesPerEntry] = new SeekTable(table, true);
        }

        private class SeekTable
        {
            public SeekTable(short[] table, bool isSelfCalculated)
            {
                Table = table;
                IsSelfCalculated = isSelfCalculated;
            }

            public readonly short[] Table;
            public readonly bool IsSelfCalculated;
        }
    }
}
