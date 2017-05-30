using System.Diagnostics;

namespace VGAudio.Tools.Rebuild
{
    [DebuggerDisplay("Diff: {ByteCount}, File: {System.IO.Path.GetFileName(Filename)}")]
    internal class Result
    {
        public string Filename { get; }
        public int ByteCount { get; }

        public Result(string filename, int byteCount)
        {
            Filename = filename;
            ByteCount = byteCount;
        }
    }
}
