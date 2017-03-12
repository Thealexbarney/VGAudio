﻿using System.IO;
using VGAudio.Containers;
using VGAudio.Containers.Genh;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Genh : MetadataReader
    {
        public override Common ToCommon(object structure)
        {
            var genh = structure as GenhStructure;
            if (genh == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = genh.SampleCount,
                SampleRate = genh.SampleRate,
                ChannelCount = genh.ChannelCount,
                Format = AudioFormat.GcAdpcm,
                Looping = genh.Looping,
                LoopStart = genh.LoopStart,
                LoopEnd = genh.LoopEnd
            };
        }

        public override object ReadMetadata(Stream stream) => new GenhReader().ReadMetadata(stream);
    }
}
