using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using VGAudio.Utilities;

namespace VGAudio.Tools.Misc
{
    public class ArrayPacker
    {
        private List<PackedItem> Items { get; } = new List<PackedItem>();
        public void Add(Array array) => Items.Add(PackItem(array));

        public byte[] Pack()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)0);
                writer.Write((ushort)Items.Count);

                foreach (PackedItem item in Items)
                {
                    writer.Write(item.Header);
                }

                ushort start = (ushort)stream.Position;
                stream.Position = 2;
                writer.Write(start);
                stream.Position = start;

                foreach (var item in Items)
                {
                    writer.Write(item.PackedArray);
                }

                return stream.ToArray();
            }
        }

        private static PackedItem PackItem(Array array)
        {
            Arrays.GetJaggedArrayInfo(array.GetType(), out Type baseType, out int rank);

            var item = new PackedItem
            {
                Array = array,
                ElementType = baseType,
                Rank = rank,
                ElementSize = Marshal.SizeOf(baseType),
                Dimensions = GetJaggedArrayDimensions(array, rank)
            };

            item.LevelSizes = GetLevelSizes(item);
            item.PackedArray = new byte[item.LevelSizes[0]];
            item.Header = GenerateItemHeader(item);

            PackItemInternal(item.Array, 0, item.Dimensions, item.LevelSizes, item.PackedArray, 0);
            return item;
        }

        private static void PackItemInternal(Array array, int depth, int[] dimensions, int[] levelSizes, byte[] packed, int index)
        {
            if (depth >= dimensions.Length - 1)
            {
                Buffer.BlockCopy(array, 0, packed, index, levelSizes[depth]);
                return;
            }
            int size = levelSizes[depth + 1];
            for (int i = 0; i < dimensions[depth]; i++)
            {
                Array subArray = (Array)array.GetValue(i);
                PackItemInternal(subArray, depth + 1, dimensions, levelSizes, packed, index + i * size);
            }
        }

        private static byte[] GenerateItemHeader(PackedItem item)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int typeId = Array.IndexOf(TypeLookup, item.ElementType);
                byte type = Helpers.CombineNibbles(typeId, item.Rank);

                writer.Write(type);
                foreach (var dimension in item.Dimensions)
                {
                    writer.Write((ushort)dimension);
                }

                return stream.ToArray();
            }
        }

        private static int[] GetLevelSizes(PackedItem item)
        {
            var levelSizes = new int[item.Rank];
            for (int i = 0; i < item.Dimensions.Length; i++)
            {
                levelSizes[i] = item.ElementSize;
                for (int j = item.Rank - 1; j >= i; j--)
                {
                    levelSizes[i] *= item.Dimensions[j];
                }
            }
            return levelSizes;
        }

        private static int[] GetJaggedArrayDimensions(Array array, int rank)
        {
            var dimensions = new int[rank];
            return GetJaggedArrayDimensionsInternal(array, 0, dimensions) ? dimensions : null;
        }

        private static bool GetJaggedArrayDimensionsInternal(Array array, int depth, int[] dimensions)
        {
            int rank = dimensions.Length;
            int length = array.Length;

            if (dimensions[depth] > 0 && length != dimensions[depth])
            {
                return false;
            }

            dimensions[depth] = length;

            if (depth == rank - 1)
            {
                return true;
            }

            for (int i = 0; i < length; i++)
            {
                if (!(array.GetValue(i) is Array subArray) || subArray.Length == 0)
                {
                    return false;
                }
                if (!GetJaggedArrayDimensionsInternal(subArray, depth + 1, dimensions))
                {
                    return false;
                }
            }

            return true;
        }

        private static readonly Type[] TypeLookup =
        {
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double)
        };

        private class PackedItem
        {
            public Array Array { get; set; }
            public byte[] PackedArray { get; set; }
            public byte[] Header { get; set; }
            public int[] Dimensions { get; set; }
            public int[] LevelSizes { get; set; }
            public int Rank { get; set; }
            public int ElementSize { get; set; }
            public Type ElementType { get; set; }
        }
    }
}
