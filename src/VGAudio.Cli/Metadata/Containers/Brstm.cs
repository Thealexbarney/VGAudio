﻿using System.IO;
using VGAudio.Containers;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Brstm : MetadataReader
    {
        public override Common ToCommon(object structure) => Bxstm.ToCommon(structure);
        public override object ReadMetadata(Stream stream) => new BrstmReader().ReadMetadata(stream);
    }
}
