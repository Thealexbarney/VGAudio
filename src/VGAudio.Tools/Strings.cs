using System.Collections.Generic;
using System.Text;

namespace VGAudio.Tools
{
    internal static class Strings
    {
        private const int MinLength = 4;

        public static string[] Search(byte[] text)
        {
            var strings = new List<string>();
            int length = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (IsPrintable(text[i]))
                {
                    length++;
                }
                else
                {
                    if (length >= MinLength)
                    {
                        strings.Add(Encoding.ASCII.GetString(text, i - length, length));
                    }
                    length = 0;
                }
            }

            return strings.ToArray();
        }

        private static bool IsPrintable(byte c)
        {
            return c >= 0x20 && c < 0x7F;
        }
    }
}
