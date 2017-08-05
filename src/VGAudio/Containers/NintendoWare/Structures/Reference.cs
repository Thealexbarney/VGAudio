using System.IO;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class Reference
    {
        public ReferenceType Type { get; }
        public int Offset { get; }
        public int BaseOffset { get; }
        public int AbsoluteOffset => BaseOffset + Offset;

        /// <summary>
        /// The reference type. Used in NW4R file formats. 0 = Address, 1 = Offset
        /// </summary>
        public byte ReferenceType => (byte)((int)Type >> 8);
        /// <summary>
        /// The data type of the referenced data. Used in NW4R file formats.
        /// Meaning can change between different structures.
        /// </summary>
        public byte DataType => (byte)Type;

        public Reference() { }
        public Reference(BinaryReader reader, int baseOffset = 0)
        {
            Type = (ReferenceType)reader.ReadInt16();
            reader.BaseStream.Position += 2;
            Offset = reader.ReadInt32();
            BaseOffset = baseOffset;
        }

        public bool IsType(ReferenceType type) => Type == type && Offset > 0;
    }
}