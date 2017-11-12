# VGAudio
VGAudio is a library for encoding, decoding, and manipulating audio files and formats that are usually found in video games.

## Supported Audio Formats and Containers
### Audio Formats

|Format|Encode|Decode|Notes|
|:-:|:-:|:-:|:-|
|CRI ADX|X|X|4-bit ADPCM codec from CRI|
|CRI HCA|X|X|MDCT-based codec from CRI|
|Nintendo GC-ADPCM|X|X|4-bit ADPCM codec from Nintendo|
|PCM 8-bit|X|X|Signed and unsigned|
|PCM 16-bit|X|X||

### Container Formats

|Container|Encode|Decode|Notes|
|-|:-:|:-:|:-|
|BCSTM|X|X||
|BCSTP|X|X||
|BCWAV|X|||
|BFSTM|X|X||
|BFSTP|X|X||
|BFWAV|X|||
|BRSTM|X|X||
|BRWAV|X|||
|DSP (Nintendo)|X|X||
|GENH|X||GC-ADPCM only|
|HCA|X|X||
|HPS|X|X|HAL Laboratory "HALPST" container for GC-ADPCM audio|
|IDSP (Interleaved Nintendo DSP)|X|X||
|MDSP|X|X|Multi-channel Nintendo DSP|
|WAV|X|X||


The most recent release of the library can be found [on NuGet](https://www.nuget.org/packages/VGAudio/)

The Universal Windows app can be found [in the Windows Store](https://www.microsoft.com/store/apps/9nblggh4s2wn)
