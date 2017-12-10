# AT9 Container
ATRAC9 audio is usually stored in a WAV container with an extension of `.at9`.

## Subchunks

AT9 files support 4 subchunks

### fmt Chunk

|Field|Length|Contents|
|:-:|:-:|:-:|
|ckID|4|Chunk ID: "fmt "|
|ckSize|4|Chunk size|
|wFormatTag|2|Format code: WAVE_FORMAT_EXTENSIBLE (0xFFFE)|
|nChannels|2|Number of channels|
|nSamplesPerSec|4|Sampling rate|
|nAvgBytesPerSec|4|Byte rate|
|nBlockAlign|2|Data block size (bytes)|
|wBitsPerSample|2|Always `0`|
|cbSize|2|Extension size|
|wSamplesPerBlock|2|Samples per data block|
|dwChannelMask|4|Channel mapping|
|SubFormat|16|SubFormat GUID: `47E142D2-36BA-4d8d-88FC-61654F8C836C`|
|At9Version|4|`2` if Band Extension is used; `1` otherwise.|
|ConfigData|4|Stream configuration data|
|Reserved|4|Usually `0`|

### fact Chunk

|Field|Length|Contents|
|:-:|:-:|:-:|
|ckID|4|Chunk ID: "fact"|
|ckSize|4|Chunk size|
|dwSampleLength|4|Number of samples (per channel)|
|EncoderDelay|4|Encoder delay in samples|
|EncoderDelay2|4|Always the same as the previous field|

### data Chunk

AT9 uses the standard `data` chunk with no extensions.

The data chunk is split into equal-sized blocks called "superframes".
Each of these superframes is `nBlockAlign` bytes long, as specified in the `fmt ` chunk.

### smpl Chunk

AT9 uses the standard `smpl` chunk with no extensions.

## Config Data

The config data is composed of 32 bits and contains basic information about the ATRAC9 audio stream. 
Fields are stored MSB-first.

|Field|Length (Bits)|Contents|
|:-:|:-:|:-:|
|Header|8|Must be `0xFE`|
|SampleRateIndex|4|Sampling rate|
|ChannelConfigIndex|3|Specifies the channel mapping|
|ValidationBit|1|Must be `0`|
|FrameSize|11|The size of one frame in bytes. Increase value by `1` when reading.|
|SuperFrameIndex|2|Frames per superframe = `1 << SuperFrameIndex`<br/> Valid values are `0` and `2`|
|Unused|3||

### Sampling Rates

Sampling rate indexes 8-15 are currently not used, and are encoded slightly differently than indexes 0-7.

In this documentation, streams using indexes 0-7 will be called low-frequency streams,
and streams using indexes 8-15 will be called high-frequency streams.

|Index|Sampling Rate (Hz)|
|:-:|:-:|
|0|11025|
|1|12000|
|2|16000|
|3|22050|
|4|24000|
|5|32000|
|6|44100|
|7|48000|
|8|44100|
|9|48000|
|10|64000|
|11|88200|
|12|96000|
|13|128000|
|14|176400|
|15|192000|

### Channel Mappings

The ATRAC9 audio steam can contain up to 5 substreams depending on the channel configuration.

The possible substream types are Stereo, Mono, or Low-frequency effects (LFE).

|Index|Description|Block 0|Block 1|Block 2|Block 3|Block 4|
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|0|Mono|Mono|||||
|1|Dual Mono|Mono|Mono||||
|2|Stereo|Stereo|||||
|3|5.1 ch|Stereo|Mono|LFE|Stereo||
|4|7.1 ch|Stereo|Mono|LFE|Stereo|Stereo|
|5|4 ch|Stereo|Stereo||||