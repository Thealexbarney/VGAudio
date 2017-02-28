#if NET20 || NET35
using System;

// ReSharper disable once CheckNamespace
namespace VGAudio
{
    internal static class EnumFlag
    {
        public static bool HasFlag(this Enum variable, Enum value)
        {
            if (variable == null)
                return false;

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!Enum.IsDefined(variable.GetType(), value))
            {
                throw new ArgumentException(
                    $"Enumeration type mismatch.  The flag is of type '{value.GetType()}', was expecting '{variable.GetType()}'.");
            }

            ulong num = Convert.ToUInt64(value);
            return ((Convert.ToUInt64(variable) & num) == num);
        }
    }
}
#endif