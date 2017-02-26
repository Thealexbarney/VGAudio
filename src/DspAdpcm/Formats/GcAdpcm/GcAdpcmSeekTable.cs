using System;
using System.Collections.Generic;
using DspAdpcm.Utilities;

#if NET20
using DspAdpcm.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

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

        public void ClearSeekTableCache() => SeekTables.Clear();

        public short[] GetSeekTable(int samplesPerEntry, bool ensureSelfCalculated)
        {
            SeekTable table;

            if (SeekTables.TryGetValue(samplesPerEntry, out table) && !(ensureSelfCalculated && !table.IsSelfCalculated))
            {
                return table.Table;
            }

            CreateSeekTable(samplesPerEntry);
            return SeekTables[samplesPerEntry].Table;
        }

        public Tuple<int, short[]> GetTableForSeeking()
        {
            KeyValuePair<int, SeekTable> seekTable = SeekTables.OrderBy(x => x.Key).FirstOrDefault(x => x.Value.IsSelfCalculated);
            return seekTable.Value == null ? null : new Tuple<int, short[]>(seekTable.Key, seekTable.Value.Table);
        }

        private void CreateSeekTable(int samplesPerEntry)
        {
            short[] audio = Adpcm.GetPcmAudio(true);
            int entryCount = Adpcm.SampleCount.DivideByRoundUp(samplesPerEntry);
            var table = new short[entryCount * 2];

            for (int i = 0; i < entryCount; i++)
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
