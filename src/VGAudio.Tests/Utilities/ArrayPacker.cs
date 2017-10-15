using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using VGAudio.Utilities;

namespace VGAudio.Tests.Utilities
{
    public class ArrayPacker
    {
        private const byte StandardMode = 0;
        private const byte EvenNdMode = 1;
        private const byte Uneven2DMode = 2;
        private static readonly bool StoreSmallestType = true;
        public List<byte[]> Items { get; } = new List<byte[]>();
        public void Add(Array array, Type baseType = null) => Items.Add(PackArray(array, Items.Count, baseType));

        public byte[] Pack()
        {
            return Pack(Items);
        }

        public static byte[] Pack(IList<byte[]> items)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)items.Count);

                foreach (byte[] item in items)
                {
                    writer.Write(item);
                }

                return stream.ToArray();
            }
        }

        public static byte[] PackCompress(IList<byte[]> items)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                byte[] packed = Pack(items);
                writer.Write((byte)1);
                writer.Write(packed.Length);
                writer.Write(Deflate(packed));
                return stream.ToArray();
            }
        }

        private static byte[] PackArray(Array array, int id, Type outType)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Arrays.GetJaggedArrayInfo(array.GetType(), out Type arrayType, out int rank);
                outType = outType ?? arrayType;
                int outTypeId = Array.IndexOf(TypeLookup, outType);

                writer.Write((byte)id);
                writer.Write(Helpers.CombineNibbles(outTypeId, rank));
                writer.Write(PackSubArray(array));

                return stream.ToArray();
            }
        }

        private static byte[] PackSubArray(Array array)
        {
            byte[] packed = null;

            foreach (Func<Array, byte[]> mode in Modes)
            {
                byte[] packedMode = mode(array);
                if (packed == null || packedMode != null && packedMode.Length < packed.Length)
                {
                    packed = packedMode;
                }
            }

            return packed;
        }

        private static readonly Func<Array, byte[]>[] Modes =
        {
            PackArrayStandardMode,
            PackArrayNdEvenMode,
            PackArray2DUnevenMode
        };

        private static byte[] PackArrayStandardMode(Array array)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Arrays.GetJaggedArrayInfo(array.GetType(), out Type outType, out int rank);
                Type storedType = rank == 1 ? FindSmallestType(array) : outType;
                int storedTypeId = rank == 1 ? Array.IndexOf(TypeLookup, storedType) : 0;

                writer.Write(Helpers.CombineNibbles(StandardMode, storedTypeId));
                writer.Write((ushort)array.Length);

                if (rank == 1)
                {
                    array = CastArray(array, storedType);

                    writer.Write(ToBytes(array));
                    return stream.ToArray();
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (array.GetValue(i) is Array subArray)
                    {
                        writer.Write(PackSubArray(subArray));
                    }
                    else
                    {
                        writer.Write(byte.MaxValue);
                    }
                }

                return stream.ToArray();
            }
        }

        private static byte[] PackArrayNdEvenMode(Array array)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Arrays.GetJaggedArrayInfo(array.GetType(), out Type outType, out int rank);
                int[] dimensions = GetJaggedArrayDimensions(array, rank);

                if (dimensions == null)
                {
                    return null;
                }

                int baseTypeId = Array.IndexOf(TypeLookup, outType);
                writer.Write(Helpers.CombineNibbles(EvenNdMode, baseTypeId));

                foreach (int dimension in dimensions)
                {
                    writer.Write((ushort)dimension);
                }

                FlattenArray(array, writer);

                return stream.ToArray();
            }
        }

        private static byte[] PackArray2DUnevenMode(Array array)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Arrays.GetJaggedArrayInfo(array.GetType(), out Type baseType, out int rank);

                if (rank != 2)
                {
                    return null;
                }

                int typeId = Array.IndexOf(TypeLookup, baseType);
                writer.Write(Helpers.CombineNibbles(Uneven2DMode, typeId));
                writer.Write((ushort)array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    if (array.GetValue(i) is Array subArray)
                    {
                        writer.Write((ushort)subArray.Length);
                    }
                    else
                    {
                        writer.Write(ushort.MaxValue);
                    }
                }

                FlattenArray(array, writer);

                return stream.ToArray();
            }
        }

        private static Array CastArray(Array inArray, Type outType)
        {
            Arrays.GetJaggedArrayInfo(inArray.GetType(), out Type inType, out int rank);

            if (rank != 1)
            {
                throw new ArgumentException();
            }

            if (inType == outType)
            {
                return inArray;
            }

            Array outArray = Array.CreateInstance(outType, inArray.Length);

            for (int i = 0; i < inArray.Length; i++)
            {
                object inValue = inArray.GetValue(i);
                object outValue = Convert.ChangeType(inValue, outType);
                outArray.SetValue(outValue, i);
            }
            return outArray;
        }

        private static Type FindSmallestType(Array array)
        {
            if (!StoreSmallestType) return array.GetType().GetElementType();

            // There's gotta be a better way to do this
            long min;
            long max;

            switch (array)
            {
                case short[] arrayCast:
                    min = arrayCast.Min();
                    max = arrayCast.Max();
                    break;
                case ushort[] arrayCast:
                    min = arrayCast.Min();
                    max = arrayCast.Max();
                    break;
                case int[] arrayCast:
                    min = arrayCast.Min();
                    max = arrayCast.Max();
                    break;
                case uint[] arrayCast:
                    min = arrayCast.Min();
                    max = arrayCast.Max();
                    break;
                case long[] arrayCast:
                    min = arrayCast.Min();
                    max = arrayCast.Max();
                    break;
                case ulong[] arrayCast:
                    ulong minLocal = arrayCast.Min();
                    ulong maxLocal = arrayCast.Max();
                    if (minLocal > long.MaxValue || maxLocal > long.MaxValue)
                    {
                        return typeof(ulong);
                    }
                    min = (long)minLocal;
                    max = (long)maxLocal;
                    break;
                default:
                    return array.GetType().GetElementType();
            }

            if (min >= sbyte.MinValue && max <= sbyte.MaxValue) return typeof(sbyte);
            if (min >= byte.MinValue && max <= byte.MaxValue) return typeof(byte);
            if (min >= short.MinValue && max <= short.MaxValue) return typeof(short);
            if (min >= ushort.MinValue && max <= ushort.MaxValue) return typeof(ushort);
            if (min >= int.MinValue && max <= int.MaxValue) return typeof(int);
            if (min >= uint.MinValue && max <= uint.MaxValue) return typeof(uint);
            return typeof(long);
        }

        private static void FlattenArray(Array array, BinaryWriter writer)
        {
            if (array == null) return;
            Arrays.GetJaggedArrayInfo(array.GetType(), out Type _, out int rank);

            if (rank == 1)
            {
                writer.Write(ToBytes(array));
                return;
            }

            for (int i = 0; i < array.Length; i++)
            {
                FlattenArray(array.GetValue(i) as Array, writer);
            }
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

        private static byte[] ToBytes(Array array)
        {
            Arrays.GetJaggedArrayInfo(array.GetType(), out Type baseType, out int rank);
            int lengthBytes = Marshal.SizeOf(baseType) * array.Length;
            if (rank != 1)
            {
                throw new ArgumentException("Array must be an array of primitives");
            }

            var bytes = new byte[lengthBytes];
            Buffer.BlockCopy(array, 0, bytes, 0, lengthBytes);
            return bytes;
        }

        public static byte[] Deflate(byte[] data)
        {
            using (var stream = new MemoryStream())
            using (var deflate = new DeflateStream(stream, CompressionLevel.Optimal))
            {
                deflate.Write(data, 0, data.Length);
                return stream.ToArray();
            }
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
    }
}
