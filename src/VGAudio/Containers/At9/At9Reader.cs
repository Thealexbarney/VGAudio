using System.IO;
using System.Linq;
using VGAudio.Codecs.Atrac9;
using VGAudio.Formats;
using VGAudio.Formats.Atrac9;
using VGAudio.Utilities.Riff;

namespace VGAudio.Containers.At9
{
    public class At9Reader : AudioReader<At9Reader, At9Structure, At9Configuration>
    {
        protected override At9Structure ReadFile(Stream stream, bool readAudioData = true)
        {
            var structure = new At9Structure();
            var parser = new RiffParser { ReadDataChunk = readAudioData };
            parser.RegisterSubChunk("fact", At9FactChunk.ParseAt9);
            parser.RegisterSubChunk("data", At9DataChunk.ParseAt9);
            parser.FormatExtensibleParser = At9WaveExtensible.ParseAt9;
            parser.ParseRiff(stream);

            ValidateAt9File(parser);

            var fmt = parser.GetSubChunk<WaveFmtChunk>("fmt ");
            var ext = (At9WaveExtensible)fmt.Ext;
            var fact = parser.GetSubChunk<At9FactChunk>("fact");
            var data = parser.GetSubChunk<At9DataChunk>("data");
            var smpl = parser.GetSubChunk<WaveSmplChunk>("smpl");

            structure.Config = new Atrac9Config(ext.ConfigData);
            structure.SampleCount = fact.SampleCount;
            structure.EncoderDelay = fact.EncoderDelaySamples;
            structure.Version = ext.VersionInfo;
            structure.AudioData = data.AudioData;
            structure.SuperframeCount = data.FrameCount;

            if (smpl?.Loops?.FirstOrDefault() != null)
            {
                structure.LoopStart = smpl.Loops[0].Start - structure.EncoderDelay;
                structure.LoopEnd = smpl.Loops[0].End - structure.EncoderDelay;
                structure.Looping = structure.LoopEnd > structure.LoopStart;
            }

            return structure;
        }

        protected override IAudioFormat ToAudioStream(At9Structure structure) =>
            new Atrac9FormatBuilder(structure.AudioData, structure.Config, structure.SampleCount, structure.EncoderDelay)
                .WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                .Build();

        private static void ValidateAt9File(RiffParser parser)
        {
            if (parser.RiffChunk.Type != "WAVE")
            {
                throw new InvalidDataException("Not a valid WAVE file");
            }

            WaveFmtChunk fmt = parser.GetSubChunk<WaveFmtChunk>("fmt ") ?? throw new InvalidDataException("File must have a valid fmt chunk");
            At9WaveExtensible ext = fmt.Ext as At9WaveExtensible ?? throw new InvalidDataException("File must have a format chunk extension");
            if (parser.GetSubChunk<At9FactChunk>("fact") == null) throw new InvalidDataException("File must have a valid fact chunk");
            if (parser.GetSubChunk<At9DataChunk>("data") == null) throw new InvalidDataException("File must have a valid data chunk");

            if (fmt.ChannelCount == 0) throw new InvalidDataException("Channel count must not be zero");

            if (ext.SubFormat != MediaSubtypes.MediaSubtypeAtrac9)
                throw new InvalidDataException($"Must contain ATRAC9 data. Has unsupported SubFormat {ext.SubFormat}");
        }
    }
}
