using System;

namespace VGAudio.Utilities
{
    public static class Arrays
    {
        public static T[] Generate<T>(int count, Func<int, T> elementGenerator)
        {
            var table = new T[count];
            for (int i = 0; i < count; i++)
            {
                table[i] = elementGenerator(i);
            }
            return table;
        }

        public static void GetJaggedArrayInfo(Type type, out Type baseType, out int rank)
        {
            rank = -1;
            baseType = type;

            while (type != null)
            {
                baseType = type;
                type = type.GetElementType();
                rank++;
            }
        }

        public static Type MakeJaggedArrayType(Type elementType, int rank)
        {
            Type type = elementType;
            for (int i = 0; i < rank; i++)
            {
                type = type.MakeArrayType();
            }
            return type;
        }
    }
}
