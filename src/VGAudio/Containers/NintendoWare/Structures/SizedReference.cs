using System.IO;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class SizedReference : Reference
    {
        public int Size { get; }

        public SizedReference(BinaryReader reader, int baseOffset = 0) : base(reader, baseOffset)
        {
            Size = reader.ReadInt32();
        }
    }
}