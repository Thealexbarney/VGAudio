﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DspAdpcm.Encode.Adpcm
{
    public interface IAdpcmStream
    {
        IList<IAdpcmChannel> Channels { get; }
        int NumSamples { get; }
        int SampleRate { get; }

        int LoopStart { get; }
        int LoopEnd { get; }
        bool Looping { get; }

        void SetLoop(int loopStart, int loopEnd);
    }

    public interface IAdpcmChannel
    {
        IEnumerable<byte> AudioData { get; }
        int NumSamples { get; }
        short[] Coefs { get; set; }
        short Hist1 { get; }
        short Hist2 { get; }

        short LoopPredScale { get; }
        short LoopHist1 { get; }
        short LoopHist2 { get; }
        void SetLoopContext(short loopPredScale, short loopHist1, short loopHist2);
    }
}
