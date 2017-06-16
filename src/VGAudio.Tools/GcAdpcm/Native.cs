using System;
using System.Runtime.InteropServices;

namespace VGAudio.Tools.GcAdpcm
{
    public static class Native
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string dllName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);
    }

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
