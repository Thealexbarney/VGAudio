using VGAudio.Utilities;

namespace VGAudio.Formats.GcAdpcm
{
    internal class GcAdpcmSeekTable
    {
        public int SamplesPerEntry { get; }
        public short[] SeekTable { get; }
        public bool IsSelfCalculated { get; }

        public GcAdpcmSeekTable(short[] seekTable, int samplesPerEntry, bool isSelfCalculated)
        {
            SeekTable = seekTable;
            SamplesPerEntry = samplesPerEntry;
            IsSelfCalculated = isSelfCalculated;
        }

        public GcAdpcmSeekTable(short[] pcm, int samplesPerEntry)
        {
            SeekTable = CreateSeekTable(pcm, samplesPerEntry);
            SamplesPerEntry = samplesPerEntry;
            IsSelfCalculated = true;
        }

        private static short[] CreateSeekTable(short[] pcm, int samplesPerEntry)
        {
            int entryCount = pcm.Length.DivideByRoundUp(samplesPerEntry);
            var seekTable = new short[entryCount * 2];

            //The first entry should always be 0
            for (int i = 1; i < entryCount; i++)
            {
                seekTable[i * 2] = pcm[i * samplesPerEntry - 1];
                seekTable[i * 2 + 1] = pcm[i * samplesPerEntry - 2];
            }

            return seekTable;
        }
    }
}
