using System.Runtime.InteropServices;

namespace VGAudio.TestsLong.GcAdpcm
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    // ReSharper disable once InconsistentNaming
    public unsafe struct ADPCMINFO
    {
        public fixed short coef[16];
        public ushort gain;
        public ushort pred_scale;
        public short yn1;
        public short yn2;

        // loop context
        public ushort loop_pred_scale;
        public short loop_yn1;
        public short loop_yn2;
    }
}
