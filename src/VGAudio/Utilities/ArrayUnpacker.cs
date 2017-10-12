using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace VGAudio.Utilities
{
    public static class ArrayUnpacker
    {
        public static Array[] UnpackArrays(byte[] packedArrays)
        {
            packedArrays = TryDecompress(packedArrays);

            using (var stream = new MemoryStream(packedArrays))
            using (var reader = new BinaryReader(stream))
            {
                int compressed = reader.ReadByte();
                int version = reader.ReadByte();
                if(compressed != 0 || version != 0) throw new InvalidDataException();

                int position = reader.ReadUInt16();
                int count = reader.ReadUInt16();
                var arrays = new Array[count];

                for (int i = 0; i < count; i++)
                {
                    byte packedType = reader.ReadByte();
                    Type elementType = TypeLookup[Helpers.GetHighNibble(packedType)];
                    byte rank = Helpers.GetLowNibble(packedType);
                    int elementSize = Marshal.SizeOf(elementType);
                    Type type = Arrays.MakeJaggedArrayType(elementType, rank);
                    int byteCount = elementSize;

                    var dimensions = new int[rank];
                    var levelSizes = new int[rank];

                    for (int d = 0; d < dimensions.Length; d++)
                    {
                        dimensions[d] = reader.ReadUInt16();
                        byteCount *= dimensions[d];
                    }

                    for (int d = 0; d < dimensions.Length; d++)
                    {
                        levelSizes[d] = elementSize;
                        for (int j = rank - 1; j >= d; j--)
                        {
                            levelSizes[d] *= dimensions[j];
                        }
                    }

                    Array array = UnpackInternal(type.GetElementType(), elementSize, packedArrays, position, 0, dimensions);
                    position += byteCount;
                    arrays[i] = array;
                }

                return arrays;
            }
        }

        private static byte[] TryDecompress(byte[] data)
        {
            bool compressed = data[0] == 1;
            if (compressed)
            {
                int decompressedLength = BitConverter.ToInt32(data, 1);
                data = Inflate(data, 5, decompressedLength);
            }
            return data;
        }

        private static byte[] Inflate(byte[] compressed, int startIndex, int length)
        {
            var inflatedBytes = new byte[length];
            using (var stream = new MemoryStream(compressed))
            {
                stream.Position = startIndex;
                using (var deflate = new DeflateStream(stream, CompressionMode.Decompress))
                {
                    deflate.Read(inflatedBytes, 0, length);
                }
            }

            return inflatedBytes;
        }

        private static Array UnpackInternal(Type type, int elementSize, byte[] data, int dataIndex, int depth, params int[] dimensions)
        {
            if (depth >= dimensions.Length) return null;
            Array array = Array.CreateInstance(type, dimensions[depth]);

            Type elementType = type.GetElementType();
            if (elementType == null)
            {
                int elementCount = dimensions[depth];
                int byteCount = elementCount * elementSize;
                Buffer.BlockCopy(data, dataIndex, array, 0, byteCount);
                return array;
            }

            int length = elementSize;
            for (int i = depth + 1; i < dimensions.Length; i++)
            {
                length *= dimensions[i];
            }

            for (int i = 0; i < dimensions[depth]; i++)
            {
                array.SetValue(UnpackInternal(elementType, elementSize, data, dataIndex + i * length, depth + 1, dimensions), i);
            }

            return array;
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
