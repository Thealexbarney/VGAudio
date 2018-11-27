using System.IO;
using System.Linq;
using VGAudio.Formats;
using VGAudio.Formats.Pcm16;
using VGAudio.Formats.Pcm8;
using VGAudio.Utilities;
using VGAudio.Utilities.Riff;

namespace VGAudio.Containers.Wave
{
    public class WaveReader : AudioReader<WaveReader, WaveStructure, WaveConfiguration>
    {
        protected override WaveStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            var structure = new WaveStructure();
            var parser = new RiffParser { ReadDataChunk = readAudioData };
            parser.ParseRiff(stream);

            ValidateWaveFile(parser);

            var fmt = parser.GetSubChunk<WaveFmtChunk>("fmt ");
            var data = parser.GetSubChunk<WaveDataChunk>("data");
            var smpl = parser.GetSubChunk<WaveSmplChunk>("smpl");

            int bytesPerSample = fmt.BitsPerSample.DivideByRoundUp(8);
            structure.RiffSubChunks = parser.GetAllSubChunks();
            structure.SampleCount = data.SubChunkSize / bytesPerSample / fmt.ChannelCount;
            structure.SampleRate = fmt.SampleRate;
            structure.BitsPerSample = fmt.BitsPerSample;
            structure.ChannelCount = fmt.ChannelCount;

            if (smpl?.Loops?.FirstOrDefault() != null)
            {
                structure.LoopStart = smpl.Loops[0].Start;
                structure.LoopEnd = smpl.Loops[0].End;
                structure.Looping = structure.LoopEnd > structure.LoopStart;
            }

            if (!readAudioData) return structure;

            switch (fmt.BitsPerSample)
            {
                case 16:
                    structure.AudioData16 = data.Data.InterleavedByteToShort(fmt.ChannelCount);
                    break;
                case 8:
                    structure.AudioData8 = data.Data.DeInterleave(bytesPerSample, fmt.ChannelCount);
                    break;
            }
            return structure;
        }

        protected override IAudioFormat ToAudioStream(WaveStructure structure)
        {
            switch (structure.BitsPerSample)
            {
                case 16:
                    return new Pcm16FormatBuilder(structure.AudioData16, structure.SampleRate)
                        .WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                        .Build();
                case 8:
                    return new Pcm8FormatBuilder(structure.AudioData8, structure.SampleRate)
                        .WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                        .Build();
                default:
                    return null;
            }
        }

        private static void ValidateWaveFile(RiffParser parser)
        {
            if (parser.RiffChunk.Type != "WAVE")
            {
                throw new InvalidDataException("Not a valid WAVE file");
            }

            WaveFmtChunk fmt = parser.GetSubChunk<WaveFmtChunk>("fmt ") ?? throw new InvalidDataException("File must have a valid fmt chunk");
            if (parser.GetSubChunk<WaveDataChunk>("data") == null) throw new InvalidDataException("File must have a valid data chunk");

            int bytesPerSample = fmt.BitsPerSample.DivideByRoundUp(8);

            if (fmt.FormatTag != WaveFormatTags.WaveFormatPcm && fmt.FormatTag != WaveFormatTags.WaveFormatExtensible)
                throw new InvalidDataException($"Must contain PCM data. Has unsupported format {fmt.FormatTag}");

            if (fmt.BitsPerSample != 16 && fmt.BitsPerSample != 8)
                throw new InvalidDataException($"Must have 8 or 16 bits per sample, not {fmt.BitsPerSample} bits per sample");

            if (fmt.ChannelCount == 0) throw new InvalidDataException("Channel count must not be zero");

            if (fmt.BlockAlign != bytesPerSample * fmt.ChannelCount)
                throw new InvalidDataException("File has invalid block alignment");

            if (fmt.Ext != null && fmt.Ext.SubFormat != MediaSubtypes.MediaSubtypePcm)
                throw new InvalidDataException($"Must contain PCM data. Has unsupported SubFormat {fmt.Ext.SubFormat}");
        }
    }
}
