﻿using System.IO;
using VGAudio.Containers.Bxstm;
using VGAudio.Formats;

namespace VGAudio.Containers
{
    public class BfstmReader : AudioReader<BfstmReader, BfstmStructure, BfstmConfiguration>
    {
        protected override BfstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            return (BfstmStructure)new BCFstmReader().ReadFile(stream, readAudioData);
        }

        protected override IAudioFormat ToAudioStream(BfstmStructure structure)
        {
            return BCFstmReader.ToAudioStream(structure);
        }

        protected override BfstmConfiguration GetConfiguration(BfstmStructure structure)
        {
            return new BfstmConfiguration
            {
                SamplesPerInterleave = structure.SamplesPerInterleave,
                SamplesPerSeekTableEntry = structure.SamplesPerSeekTableEntry,
                IncludeUnalignedLoopPoints = structure.Version == 4
            };
        }
    }
}