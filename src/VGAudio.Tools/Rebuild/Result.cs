using System.Diagnostics;

namespace VGAudio.Tools.Rebuild
{
    [DebuggerDisplay("Diff: {ByteCount}, File: {System.IO.Path.GetFileName(Filename)}")]
    internal class Result
    {
        public string Filename { get; }
        public int ByteCount { get; }
        public string Error { get; }

        public Result(string filename, int byteCount, string error = null)
        {
            Filename = filename;
            ByteCount = byteCount;
            Error = error;
        }
    }
}
