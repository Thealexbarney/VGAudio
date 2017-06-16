using System;
using System.Collections.Generic;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Formats.Pcm16;
using Xunit;

namespace VGAudio.Tests.Formats
{
    public class AudioDataTests
    {
        [Fact]
        public void CreatingAudioDataWithAdpcm()
        {
            GcAdpcmFormat adpcm = GenerateAudio.GenerateAdpcmSineWave(200, 2, 48000);
            var audio = new AudioData(adpcm);
            Assert.Same(adpcm, audio.GetFormat<GcAdpcmFormat>());
        }

        [Fact]
        public void CreatingAudioDataWithPcm()
        {
            Pcm16Format pcm = GenerateAudio.GeneratePcmSineWave(200, 2, 48000);
            var audio = new AudioData(pcm);
            Assert.Same(pcm, audio.GetFormat<Pcm16Format>());
        }

        [Fact]
        public void CreatingAudioDataWithInterface()
        {
            IAudioFormat adpcm = GenerateAudio.GenerateAdpcmSineWave(200, 2, 48000);
            var audio = new AudioData(adpcm);
            Assert.Same(adpcm, audio.GetFormat<GcAdpcmFormat>());
        }

        [Fact]
        public void GettingSecondFormatFromFirst()
        {
            Pcm16Format pcm = GenerateAudio.GeneratePcmSineWave(200, 2, 48000);
            var audio = new AudioData(pcm);
            var adpcm = audio.GetFormat<GcAdpcmFormat>();

            Assert.Same(adpcm, audio.GetFormat<GcAdpcmFormat>());
        }

        [Fact]
        public void ListAvailableFormats()
        {
            var expected = new List<Type> { typeof(Pcm16Format) };
            Pcm16Format pcm = GenerateAudio.GeneratePcmSineWave(100, 1, 48000);
            var audio = new AudioData(pcm);
            Assert.Equal(expected, audio.ListAvailableFormats());

            expected.Add(typeof(GcAdpcmFormat));
            audio.GetFormat<GcAdpcmFormat>();
            Assert.Equal(expected, audio.ListAvailableFormats());
        }

        [Fact]
        public void GetAllFormats()
        {
            Pcm16Format pcm = GenerateAudio.GeneratePcmSineWave(100, 1, 48000);
            var audio = new AudioData(pcm);
            Assert.Collection(audio.GetAllFormats(), x => Assert.True(x is Pcm16Format));

            audio.GetFormat<GcAdpcmFormat>();
            Assert.Collection(audio.GetAllFormats(), x => Assert.True(x is Pcm16Format), x => Assert.True(x is GcAdpcmFormat));
        }

        [Fact]
        public void AddSameFormats()
        {
            Pcm16Format pcm = GenerateAudio.GeneratePcmSineWave(100, 1, 48000);
            Pcm16Format pcm2 = GenerateAudio.GeneratePcmSineWave(100, 1, 48000);
            var audio = new AudioData(pcm);
            var audio2 = new AudioData(pcm2);

            var combined = AudioData.Combine(audio, audio2);
            var pcmCombined = combined.GetFormat<Pcm16Format>();
            Assert.Same(pcm.Channels[0], pcmCombined.Channels[0]);
            Assert.Same(pcm2.Channels[0], pcmCombined.Channels[1]);
            Assert.Equal(2, pcmCombined.ChannelCount);
        }

        [Fact]
        public void AddDifferentFormats()
        {
            Pcm16Format pcm = GenerateAudio.GeneratePcmSineWave(100, 1, 48000);
            var adpcm = GenerateAudio.GenerateAdpcmSineWave(100, 1, 48000);
            var audio = new AudioData(pcm);
            var audio2 = new AudioData(adpcm);

            var combined = AudioData.Combine(audio, audio2);
            Assert.Collection(combined.GetAllFormats(), x => Assert.True(x is Pcm16Format));
            var pcmCombined = combined.GetFormat<Pcm16Format>();
            Assert.Same(pcm.Channels[0], pcmCombined.Channels[0]);
            Assert.Equal(2, pcmCombined.ChannelCount);
        }

        [Fact]
        public void AddSameFormatsAfterEncodingChoosesEncodedFormat()
        {
            Pcm16Format pcm = GenerateAudio.GeneratePcmSineWave(100, 1, 48000);
            Pcm16Format pcm2 = GenerateAudio.GeneratePcmSineWave(100, 1, 48000);
            var audio = new AudioData(pcm);
            var audio2 = new AudioData(pcm2);

            var adpcm = audio.GetFormat<GcAdpcmFormat>();
            var adpcm2 = audio2.GetFormat<GcAdpcmFormat>();

            var combined = AudioData.Combine(audio, audio2);
            Assert.Collection(combined.GetAllFormats(), x => Assert.True(x is GcAdpcmFormat));
            var pcmCombined = combined.GetFormat<Pcm16Format>();
            var adpcmCombined = combined.GetFormat<GcAdpcmFormat>();

            Assert.Same(adpcm.Channels[0].GetAdpcmAudio(), adpcmCombined.Channels[0].GetAdpcmAudio());
            Assert.Same(adpcm2.Channels[0].GetAdpcmAudio(), adpcmCombined.Channels[1].GetAdpcmAudio());
            Assert.NotSame(pcm.Channels[0], pcmCombined.Channels[0]);
            Assert.NotSame(pcm2.Channels[0], pcmCombined.Channels[1]);
            Assert.Equal(2, adpcmCombined.ChannelCount);
        }

        [Fact]
        public void AddDifferentFormatsMultipleInputs()
        {
            Pcm16Format pcmFormat = GenerateAudio.GeneratePcmSineWave(100, 4, 48000);
            var pcm0 = pcmFormat.GetChannels(0);
            var pcm1 = pcmFormat.GetChannels(1, 2);
            var pcm2 = pcmFormat.GetChannels(3);
            var audio0 = new AudioData(pcm0);
            var audio1 = new AudioData(pcm1);
            var audio2 = new AudioData(pcm2);

            audio0.GetFormat<GcAdpcmFormat>();
            audio2.GetFormat<GcAdpcmFormat>();

            var combined = AudioData.Combine(audio0, audio1, audio2);
            Assert.Collection(combined.GetAllFormats(), x => Assert.True(x is Pcm16Format));
            var pcmCombined = combined.GetFormat<Pcm16Format>();
            Assert.Equal(4, pcmCombined.ChannelCount);
            Assert.Same(pcm0.Channels[0], pcmCombined.Channels[0]);
            Assert.Same(pcm1.Channels[0], pcmCombined.Channels[1]);
            Assert.Same(pcm1.Channels[1], pcmCombined.Channels[2]);
            Assert.Same(pcm2.Channels[0], pcmCombined.Channels[3]);
        }

        [Fact]
        public void CombineThrowsWhenArrayIsNull()
        {
            Exception ex = Record.Exception(() => AudioData.Combine(null));
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public void CombineThrowsWhenElementIsNull()
        {
            var audio = new AudioData(GenerateAudio.GeneratePcmSineWave(100, 1, 48000));
            Exception ex = Record.Exception(() => AudioData.Combine(audio, null));
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public void CombineThrowsWhenAudioCantBeCombined()
        {
            var audio = new AudioData(GenerateAudio.GeneratePcmSineWave(100, 1, 48000));
            var audio2 = new AudioData(GenerateAudio.GeneratePcmSineWave(200, 1, 48000));
            Exception ex = Record.Exception(() => AudioData.Combine(audio, audio2));
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public void LoopingLoopsAllFormats()
        {
            var audio = new AudioData(GenerateAudio.GeneratePcmSineWave(100, 1, 48000));
            var adpcm = audio.GetFormat<GcAdpcmFormat>();
            audio.SetLoop(true, 10, 100);
            var loopedAdpcm = audio.GetFormat<GcAdpcmFormat>();
            var loopedPcm = audio.GetFormat<Pcm16Format>();

            Assert.False(adpcm.Looping);
            Assert.Equal(0, adpcm.LoopStart);
            Assert.Equal(0, adpcm.LoopEnd);
            Assert.True(loopedAdpcm.Looping);
            Assert.Equal(10, loopedAdpcm.LoopStart);
            Assert.Equal(100, loopedAdpcm.LoopEnd);
            Assert.True(loopedPcm.Looping);
            Assert.Equal(10, loopedPcm.LoopStart);
            Assert.Equal(100, loopedPcm.LoopEnd);
        }

        [Fact]
        public void LoopingBoolOnlyLoopsAllFormats()
        {
            var audio = new AudioData(GenerateAudio.GeneratePcmSineWave(100, 1, 48000));
            audio.GetFormat<GcAdpcmFormat>();
            audio.SetLoop(true);
            var loopedAdpcm = audio.GetFormat<GcAdpcmFormat>();
            var loopedPcm = audio.GetFormat<Pcm16Format>();

            Assert.True(loopedAdpcm.Looping);
            Assert.Equal(0, loopedAdpcm.LoopStart);
            Assert.Equal(100, loopedAdpcm.LoopEnd);
            Assert.True(loopedPcm.Looping);
            Assert.Equal(0, loopedPcm.LoopStart);
            Assert.Equal(100, loopedPcm.LoopEnd);

            audio.SetLoop(false);
            var unloopedAdpcm = audio.GetFormat<GcAdpcmFormat>();
            var unloopedPcm = audio.GetFormat<Pcm16Format>();

            Assert.False(unloopedAdpcm.Looping);
            Assert.Equal(0, unloopedAdpcm.LoopStart);
            Assert.Equal(0, unloopedAdpcm.LoopEnd);
            Assert.False(unloopedPcm.Looping);
            Assert.Equal(0, unloopedPcm.LoopStart);
            Assert.Equal(0, unloopedPcm.LoopEnd);
        }
    }
}
