﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using DspAdpcm.Adpcm.Formats.Structures;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Adpcm.Formats
{
    public class Genh
    {
        public AdpcmStream AudioStream { get; set; }

        public Genh(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            GenhStructure genh = ReadGenhFile(stream);
            AudioStream = GetAdpcmStream(genh);
        }

        public Genh(byte[] file)
        {
            using (var stream = new MemoryStream(file))
            {
                GenhStructure genh = ReadGenhFile(stream);
                AudioStream = GetAdpcmStream(genh);
            }
        }

        public static GenhStructure ReadMetadata(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return ReadGenhFile(stream, false);
        }

        private static GenhStructure ReadGenhFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                var structure = new GenhStructure();

                ParseHeader(reader, structure);

                if (readAudioData)
                {
                    reader.BaseStream.Position = structure.AudioDataOffset;
                    ParseData(reader, structure);
                }

                return structure;
            }
        }

        private static AdpcmStream GetAdpcmStream(GenhStructure structure)
        {
            var audioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
            if (structure.Looping)
            {
                audioStream.SetLoop(structure.LoopStart, structure.LoopEnd);
            }

            for (int c = 0; c < structure.NumChannels; c++)
            {
                var channel = new AdpcmChannel(structure.NumSamples, structure.AudioData[c])
                {
                    Coefs = structure.Channels[c].Coefs,
                };
                audioStream.Channels.Add(channel);
            }

            return audioStream;
        }

        private static void ParseHeader(BinaryReader reader, GenhStructure structure)
        {
            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "GENH")
            {
                throw new InvalidDataException("File has no GENH header");
            }

            structure.NumChannels = reader.ReadInt32();
            structure.Interleave = reader.ReadInt32();
            structure.SampleRate = reader.ReadInt32();
            structure.LoopStart = reader.ReadInt32();
            structure.LoopEnd = reader.ReadInt32();
            structure.Codec = reader.ReadInt32();
            structure.AudioDataOffset = reader.ReadInt32();
            structure.HeaderSize = reader.ReadInt32();
            structure.Coefs[0] = reader.ReadInt32();
            structure.Coefs[1] = reader.ReadInt32();
            structure.InterleaveType = reader.ReadInt32();
            structure.CoefType = (GenhCoefType)reader.ReadInt32();
            structure.CoefsSplit[0] = reader.ReadInt32();
            structure.CoefsSplit[1] = reader.ReadInt32();

            if (structure.NumChannels < 1)
            {
                throw new InvalidDataException("File must have at least one channel.");
            }

            if (structure.NumChannels > 2)
            {
                throw new InvalidDataException("GENH does not support more than 2 channels with NGC DSP files.");
            }

            if (structure.HeaderSize > structure.AudioDataOffset)
            {
                throw new InvalidDataException("Audio data must come after the GENH header.");
            }

            for (int i = 0; i < structure.NumChannels; i++)
            {
                using (BinaryReader coefReader = structure.CoefType.HasFlag(GenhCoefType.LittleEndian)
                    ? new BinaryReader(reader.BaseStream, Encoding.UTF8, true)
                    : new BinaryReaderBE(reader.BaseStream, Encoding.UTF8, true))
                {
                    reader.BaseStream.Position = structure.Coefs[i];
                    var channel = new AdpcmChannelInfo
                    {
                        Coefs = Enumerable.Range(0, 16).Select(x => coefReader.ReadInt16()).ToArray(),
                    };

                    structure.Channels.Add(channel);
                }
            }
        }

        private static void ParseData(BinaryReader reader, GenhStructure structure)
        {
            int dataLength = GetBytesForAdpcmSamples(structure.NumSamples) * structure.NumChannels;
            structure.AudioData = reader.BaseStream.DeInterleave(dataLength, structure.Interleave, structure.NumChannels);
        }
    }
}