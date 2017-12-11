using System;

namespace VGAudio.Tools.Atrac9
{
    public class Result
    {
        public string Filename { get; set; }

        public Exception Exception { get; set; }

        public bool Equal { get; set; }
        public bool Invalid { get; set; }

        public int Channel { get; set; }
        public int Frame { get; set; }
        public int FrameSample { get; set; }
        public int Sample { get; set; }
    }
}