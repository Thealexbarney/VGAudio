using System.IO;

namespace VGAudio.Containers.Bxstm.Structures
{
    public class Reference
    {
        public ReferenceType Type { get; }
        public int Offset { get; }
        public int BaseOffset { get; }
        public int AbsoluteOffset => BaseOffset + Offset;
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