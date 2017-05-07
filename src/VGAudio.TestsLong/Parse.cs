using System;

namespace VGAudio.TestsLong
{
    internal static class Parse
    {
        public static FileType ParseFileType(string type)
        {
            if (Enum.TryParse(type, true, out FileType parsedType)) return parsedType;

            Console.WriteLine($"{type} is not a valid file type");
            Console.WriteLine("Valid file types are:");
            Console.WriteLine("Wave, Dsp, Idsp, Brstm, Bcstm, Bfstm");
            return FileType.NotSet;
        }
    }
}
