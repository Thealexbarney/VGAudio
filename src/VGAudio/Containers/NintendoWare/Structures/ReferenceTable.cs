using System.Collections.Generic;
using System.IO;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class ReferenceTable
    {
        public int Count { get; }
        public List<Reference> References { get; } = new List<Reference>();

        public ReferenceTable(BinaryReader reader, int baseOffset = 0)
        {
            Count = reader.ReadInt32();

            for (int i = 0; i < Count; i++)
            {
                References.Add(new Reference(reader, baseOffset));
            }
        }
    }
}
