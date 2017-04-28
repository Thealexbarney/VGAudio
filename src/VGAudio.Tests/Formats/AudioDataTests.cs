using System;
using System.Collections.Generic;
using VGAudio.Formats;
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
            var expected = new List<Type> { typeof(Pcm16Format) };
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
    }
}
