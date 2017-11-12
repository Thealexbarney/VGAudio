# VGAudio
VGAudio is a library for encoding, decoding, and manipulating audio files and formats that are usually found in video games.

The most recent release of the library can be found [on NuGet](https://www.nuget.org/packages/VGAudio/)

The Universal Windows app can be found [in the Windows Store](https://www.microsoft.com/store/apps/9nblggh4s2wn)

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
|BCSTP||X||
|BCWAV||X||
|BFSTM|X|X||
|BFSTP||X||
|BFWAV||X||
|BRSTM|X|X||
|BRWAV||X||
|DSP (Nintendo)|X|X||
|GENH||X|GC-ADPCM only|
|HCA|X|X||
|HPS|X|X|HAL Laboratory "HALPST" container for GC-ADPCM audio|
|IDSP (Interleaved Nintendo DSP)|X|X||
|MDSP|X|X|Multi-channel Nintendo DSP|
|WAV|X|X||

## VGAudioCli

The tool `VGAudioCli` provides a command-line interface (CLI) to convert audio files supported by the `VGAudio` library.

### Examples
#### Basic Usage
- Convert to another file type. The file-types are inferred from the file extensions:  
`VGAudioCli input.wav output.dsp`

- Files can be listed in any order:  
`VGAudioCli -o output.dsp -i input.wav`

- Loop a file from samples 30000 to 200000:  
`VGAudioCli input.wav output.dsp -l 30000-200000`

- Show file metadata:  
`VGAudioCli -m input.wav`

- Specify output audio format if the output container supports multiple audio formats.  
Save as a BRSTM file with PCM 16-bit audio:  
`VGAudioCli input.wav output.brstm -f pcm16`

#### Splitting and Combining Files

- Output only channels 0,1,2, and 5 from the input file:  
`VGAudioCli -i:0-2,5 input.wav output.dsp`

- Combine two mono files into a stereo file. (Input files must have the same length)    
`VGAudioCli -i input_left.wav -i input_right.wav output_stereo.dsp`

- Take channels 0 and 3 from `input1.wav`, channel 1 from `input2.dsp`, and all the channels in `input3.adx` and save them as `output.hca`:  
`VGAudioCli -i:0,3 input1.wav -i:1 input2.dsp -i input3.adx output.hca`

### Batch Conversions

Batch conversion allows an entire folder of files to be converted at a time. These conversions are done in parallel, and will be faster the more CPU cores you have.

Batch mode is used by inputting `-b` or `--batch` as the first command-line argument. Unlike single-file conversions, you must explicitly specify the output file type in batch conversions.

#### Examples 

- Convert all the files in the `source` folder to HCA files, and save them in the `dest` folder:  
`VGAudio -b -i source -o dest -f hca`

- Convert all the files in the `source` folder and all its subfolders to HCA files, and save them in the `dest` folder:  
`VGAudio -b -r -i source -o dest -f hca`

- Other options can also be specified during batch conversion.  
Remove the loop points from any converted files:  
`VGAudio -b -i source -o dest -f hca --no-loop`

- Codec-specific options can be specified as well:  
`VGAudio -b -i source -o dest -f hca --hcaquality low`

### Lossless conversions

Depending on which format is used, when the input and output files are the same audio format, the conversion is often lossless.
For example, both the DSP and BRSTM containers contain GC-ADPCM audio, so converting a DSP file to a BRSTM file would be a lossless conversion.
The encoded audio data is read from the DSP file and repackaged into a BRSTM file without any modification.

The channels from some formats like ADX and GC-ADPCM can be losslessly split and combined.
For example, a stereo ADX file can be split into two mono ADX files without any loss of quality.
Conversely, two stereo ADX files can be combined into a single 4-channel ADX file.

Because no audio encoding is done, these types of conversions are very quick compared to normal conversions.
