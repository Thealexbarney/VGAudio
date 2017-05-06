using System;
using System.IO;
using System.Runtime.InteropServices;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.TestsLong.GcAdpcm
{
    public class DspToolCafe32 : IDspTool
    {
        private const string DllName = "dsptoolcafe32.dll";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DspCorrelateCoefsDelegate(short[] src, uint samples, ref ADPCMINFO cxt);
        public DspCorrelateCoefsDelegate DspCorrelateCoefsDll;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DspEncodeFrameDelegate(short[] src, byte[] dest, short[] coefs, byte one);
        public DspEncodeFrameDelegate DspEncodeFrameDll;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DecodeDelegate(byte[] src, short[] dst, ref ADPCMINFO cxt, uint samples);
        public DecodeDelegate Decode;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void EncodeDelegate(short[] src, byte[] dst, ref ADPCMINFO cxt, uint samples);
        public EncodeDelegate Encode;

        private IntPtr pDll;

        public DspToolCafe32(string path)
        {
            string dllPath = Path.Combine(path, DllName);
            pDll = Native.LoadLibrary(dllPath);
            if (pDll == IntPtr.Zero)
            {
                throw new DllNotFoundException($"Can't open {dllPath}");
            }
            GetDelegates();
        }

        public unsafe GcAdpcmChannel EncodeChannel(short[] pcm)
        {
            int sampleCount = pcm.Length;
            byte[] adpcm = new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)];
            var info = new ADPCMINFO();

            Encode(pcm, adpcm, ref info, (uint)sampleCount);

            short[] coefs = new short[16];
            for (int i = 0; i < 16; i++)
            {
                coefs[i] = info.coef[i];
            }

            return new GcAdpcmChannel(adpcm, coefs, sampleCount);
        }

        public unsafe short[] DspCorrelateCoefs(short[] pcm)
        {
            var info = new ADPCMINFO();

            DspCorrelateCoefsDll(pcm, (uint)pcm.Length, ref info);

            short[] coefs = new short[16];
            for (int i = 0; i < 16; i++)
            {
                coefs[i] = info.coef[i];
            }

            return coefs;
        }

        public void DspEncodeFrame(short[] pcmInOut, int sampleCount, byte[] adpcmOut, short[] coefsIn)
        {
            DspEncodeFrameDll(pcmInOut, adpcmOut, coefsIn, 1);
        }

        public short[] DecodeChannel(GcAdpcmChannel channel)
        {
            return DecodeAdpcm(channel.GetAdpcmAudio(), channel.Coefs, channel.SampleCount);
        }

        public unsafe short[] DecodeAdpcm(byte[] adpcm, short[] coefs, int sampleCount)
        {
            var info = new ADPCMINFO();
            short[] pcm = new short[sampleCount];
            for (int i = 0; i < 16; i++)
            {
                info.coef[i] = coefs[i];
            }

            Decode(adpcm, pcm, ref info, (uint)sampleCount);
            return pcm;
        }

        public void GetDelegates()
        {
            IntPtr pDecode = Native.GetProcAddress(pDll, "decode");
            Decode = (DecodeDelegate)Marshal.GetDelegateForFunctionPointer(pDecode, typeof(DecodeDelegate));

            IntPtr pEncode = Native.GetProcAddress(pDll, "encode");
            Encode = (EncodeDelegate)Marshal.GetDelegateForFunctionPointer(pEncode, typeof(EncodeDelegate));

            IntPtr pDspCorrelateCoefs = pDll + 0x3400;
            DspCorrelateCoefsDll = (DspCorrelateCoefsDelegate)Marshal.GetDelegateForFunctionPointer(pDspCorrelateCoefs, typeof(DspCorrelateCoefsDelegate));

            IntPtr pDspEncodeFrame = pDll + 0x1970;
            DspEncodeFrameDll = (DspEncodeFrameDelegate)Marshal.GetDelegateForFunctionPointer(pDspEncodeFrame, typeof(DspEncodeFrameDelegate));
        }
    }
}
