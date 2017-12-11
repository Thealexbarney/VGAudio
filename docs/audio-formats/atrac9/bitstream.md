# ATRAC9 Bitstream

An ATRAC9 stream is composed of various nested data structures.
The hierarchy of these main structures is as follows:  
`ATRAC9 stream` -> `Superframe` -> `Frame` -> `Block` -> `Channel`

## Superframe
In practice, superframes within a stream are of fixed size, although technically there's nothing stopping a stream from having variable-sized superframes.

A superframe is the smallest decodable unit in an ATRAC9 stream, and can contain either 1 or 4 frames of varying lengths.

## Frame
A frame is a collection of 1-5 sequentially-stored blocks, and has no header or structure beyond that.
The count and type of the blocks is determined by the channel configuration of the ATRAC9 stream.

The length of a frame must be a multiple of 8 bits (1 byte).
This length is not stored in the bitstream, and can only be determined by unpacking the entire frame.

## Block

A block contains one frame of encoded audio data for one substream.
Every block contains parameters for the entire block, and spectrum data for each channel.

## Block Types

There are 3 different block types, mono, stereo, and low-frequency effects (LFE).

Mono and stereo blocks are packed in the same way, with stereo blocks having a few more fields than mono blocks.

LFE blocks are packed slightly differently because they only contain 2 quantization units of spectral data.

## Block Structure

In the tables below, for all 1-bit, boolean fields, `0` is false and `1` is true unless otherwise specified.

The basic structure of standard mono and stereo blocks:

- Block header
- Band parameters
- Gradient parameters
- Intensity stereo parameters
- Extension parameters
- For each channel:
  - Scale factors
  - Quantized spectrum
  - Fine quantized spectrum

## Block Header

All block types share the following header:

|Name|Bits|Description|
|:-:|:-:|:-:|
|first_in_superframe|1|Is this the first frame in the superframe?|
|reuse_band_params|1|Should the band parameters from the previous frame be reused?<br/>The first frame in a superframe must have this bit set.|

## Band Parameters

This section is stored if `reuse_band_params` is set.

### Minimum Band Count

Each stream has a minimum band count `min_band_count` depending on the sampling rate. This value is added to each of the band parameters.

|Type|Minimum<br/>Bands|
|:-:|:-:|
|High-frequency|1|
|Low-frequency|3|

### Bitstream

|Name|Condition|Bits|Description|
|:-:|:-:|:-:|:-:|
|band_count||4|The number of encoded bands|
|stereo_band|Stereo block|4|The first band to use intensity stereo coding.<br/>The number of bands using intensity stereo coding is given by `band_count - stereo_band`|
|band_extension_enabled||1|If `true` this block uses band extension|
|extension_band|`band_extension_enabled`|4|The total number of bands after applying band extension|

## Gradient Parameters

|Name|Condition|Bits|Description|
|:-:|:-:|:-:|:-:|
|mode||2|The mode used when creating the gradient. (0-3)|
|start_SF||6|The scale factor where the gradient slope begins|
|end_SF|`gradient_mode == 0`|6|The scale factor where the gradient slope ends.<br/>Add `1` to this value when read|
|min_value||5|The low value of the gradient|
|max_value|`gradient_mode == 0`|5|The high value of the gradient|
|boundary||4|Each value below this scale factor is increased by `1`|

## Stereo Parameters

This section is only stored for stereo blocks.

|Name|Condition|Bits|Description|
|:-:|:-:|:-:|:-:|
|master_channel||1|The main channel for stereo encoding.<br/>0 - Left; 1 - Right|
|has_stereo_signs||1|Set if the block signs for use with intensity stereo|
|intensity_stereo_signs|`has_stereo_signs`|1 x the number of stereo quantization units|Contains one value per quantization unit.<br/> 0 - Positive; 1 - Negative|

## Extension Parameters

When `band_extension_enabled == false`

|Name|Condition|Bits|Description|
|:-:|:-:|:-:|:-:|
|has_extension_data||1|Set if extension data is stored|
|extension_mode|`has_extension_data`|2|The extension mode|
|extension_data_length|`has_extension_data`|5|The length in bits of the extension data|
|extension_data|`has_extension_data`|`extension_data_length`|The extension data|

When `band_extension_enabled == true`

|Name|Condition|Bits|Description|
|:-:|:-:|:-:|:-:|
|ext_mode_right|Stereo block|2|The extension mode for the right channel|
|dummy|Mono block|1|Unused bit|
|has_extension_data||1|Set if extension data is stored|
|ext_mode_left|`has_extension_data`|2|The extension mode for the left channel|
|extension_data_length|`has_extension_data`|5|The total length in bits of the extension data for all channels combined|
|extension_data_left|`has_extension_data`|Varies|The extension data for the left channel|
|extension_data_right|`has_extension_data`; Stereo block|Varies|The extension data for the right channel|

## Channel

There is only one type of channel. After the block parameters, all the scale factors and spectral coefficients for the 1st channel are stored.
If the block is a stereo block, the data for the 2nd channel comes after that.

## Scale Factors

[See Here](scale-factors.md)