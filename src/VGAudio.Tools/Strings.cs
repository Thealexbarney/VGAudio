using System.Collections.Generic;
using System.Text;

namespace VGAudio.Tools
{
    internal static class Strings
    {
        public static string[] Search(byte[] text, int minLength = 4)
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
                    if (length >= minLength)
                    {
                        for (int subLength = length; subLength > minLength; subLength--)
                        {
                            strings.Add(Encoding.ASCII.GetString(text, i - subLength, subLength));
                        }
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
