using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.TestsLong.GcAdpcm
{
    public class DspToolDll : IDspTool
    {
        public DspToolType DllType { get; }
        private readonly IntPtr _pDll;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DspCorrelateCoefsDelegate(short[] src, uint samples, short[] coefsOut);
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
        
        public DspToolDll(DspToolType type, string path)
        {
            DllType = type;
            string dllPath = Path.Combine(path, DllInfo[type].Filename);
            _pDll = Native.LoadLibrary(dllPath);
            if (_pDll == IntPtr.Zero)
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

        public short[] DspCorrelateCoefs(short[] pcm)
        {
            var coefs = new short[16];
            DspCorrelateCoefsDll(pcm, (uint)pcm.Length, coefs);
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
            DspToolInfo dll = DllInfo[DllType];

            IntPtr pDecode = Native.GetProcAddress(_pDll, "decode");
            IntPtr pEncode = Native.GetProcAddress(_pDll, "encode");
            IntPtr pDspCorrelateCoefs = _pDll + dll.CorrelateCoefsAddress;
            IntPtr pDspEncodeFrame = _pDll + dll.EncodeFrameAddress;

            if (DllType == DspToolType.OpenSource)
            {
                pDspCorrelateCoefs = Native.GetProcAddress(_pDll, "correlateCoefs");
                pDspEncodeFrame = Native.GetProcAddress(_pDll, "encodeFrame");
            }

            Decode = (DecodeDelegate)Marshal.GetDelegateForFunctionPointer(pDecode, typeof(DecodeDelegate));
            Encode = (EncodeDelegate)Marshal.GetDelegateForFunctionPointer(pEncode, typeof(EncodeDelegate));
            DspCorrelateCoefsDll = (DspCorrelateCoefsDelegate)Marshal.GetDelegateForFunctionPointer(pDspCorrelateCoefs, typeof(DspCorrelateCoefsDelegate));
            DspEncodeFrameDll = (DspEncodeFrameDelegate)Marshal.GetDelegateForFunctionPointer(pDspEncodeFrame, typeof(DspEncodeFrameDelegate));
        }

        private struct DspToolInfo
        {
            public readonly string Filename;
            public readonly int EncodeFrameAddress;
            public readonly int CorrelateCoefsAddress;

            public DspToolInfo(string filename, int encodeFrameAddress, int correlateCoefsAddress)
            {
                Filename = filename;
                EncodeFrameAddress = encodeFrameAddress;
                CorrelateCoefsAddress = correlateCoefsAddress;
            }
        }

        private static readonly Dictionary<DspToolType, DspToolInfo> DllInfo = new Dictionary<DspToolType, DspToolInfo>
        {
            [DspToolType.Revolution] = new DspToolInfo("dsptoolrevolution.dll", 0x19d0, 0x3ce0),
            [DspToolType.Cafe32] = new DspToolInfo("dsptoolcafe32.dll", 0x1970, 0x3400),
            [DspToolType.Cafe64] = new DspToolInfo("dsptoolcafe64.dll", 0x1b90, 0x3900),
            [DspToolType.Cafe64Debug] = new DspToolInfo("dsptoolcafe64debug.dll", 0x2490, 0x53c0),
            [DspToolType.OpenSource] = new DspToolInfo("dsptoolopen.dll", 0, 0)
        };
    }

    public enum DspToolType
    {
        Revolution,
        Cafe32,
        Cafe64,
        Cafe64Debug,
        OpenSource
    }
}
