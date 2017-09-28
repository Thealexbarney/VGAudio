# HCA Spectrum Decoding

The quantized spectral data is stored using various prefix codes. The codebook used depends on the precision that was needed to encode each value.
The entire spectrum is composed of 128 bands, but high-frequency bands may be discarded, encoding only the remaining bands in order to lower the bitrate.

Each band contains a single quantitized spectral coefficient, and has its own `Resolution` which determines the possible range of the encoded value, as listed in the table below. Because each band has its own resolution, the prefix codebook used can change at each value.

## Quantization Resolution Table

| Resolution | Min<br/>Value | Max<br/>Value | Levels |
|:----------:|:-------------:|:-------------:|:------:|
| 0          |  0            | 0             | 1      |
| 1          | -1            | 1             | 3      |
| 2          | -2            | 2             | 5      |
| 3          | -3            | 3             | 7      |
| 4          | -4            | 4             | 9      |
| 5          | -5            | 5             | 11     |
| 6          | -6            | 6             | 13     |
| 7          | -7            | 7             | 15     |
| 8          | -15           | 15            | 31     |
| 9          | -31           | 31            | 63     |
| 10         | -63           | 63            | 127    |
| 11         | -127          | 127           | 255    |
| 12         | -255          | 255           | 511    |
| 13         | -511          | 511           | 1023   |
| 14         | -1023         | 1023          | 2047   |
| 15         | -2047         | 2047          | 4095   |


## Prefix Codebooks

### Resolution 1

|Value|Code|
|:---:|:--:|
| 0   |0   |
| 1   |10  |
|-1   |11  |

### Resolution 2

|Value|Code|
|:---:|:--:|
| 0   |00  |
| 1   |01  |
|-1   |10  |
| 2   |110 |
|-2   |111 |

### Resolution 3

|Value|Code|
|:---:|:--:|
| 0   |00  |
| 1   |010 |
|-1   |011 |
| 2   |100 |
|-2   |101 |
| 3   |110 |
|-3   |111 |

### Resolution 4

|Value|Code|
|:---:|:--:|
| 0   |000 |
| 1   |001 |
|-1   |010 |
| 2   |011 |
|-2   |100 |
| 3   |101 |
|-3   |110 |
| 4   |1110|
|-4   |1111|

### Resolution 5

|Value|Code|
|:---:|:--:|
| 0   |000 |
| 1   |001 |
|-1   |010 |
| 2   |011 |
|-2   |100 |
| 3   |1010|
|-3   |1011|
| 4   |1100|
|-4   |1101|
| 5   |1110|
|-5   |1111|

### Resolution 6

|Value|Code|
|:---:|:--:|
| 0   |000 |
| 1   |001 |
|-1   |010 |
| 2   |0110|
|-2   |0111|
| 3   |1000|
|-3   |1001|
| 4   |1010|
|-4   |1011|
| 5   |1100|
|-5   |1101|
| 6   |1110|
|-6   |1111|

### Resolution 7

|Value|Code|
|:---:|:--:|
| 0   |000 |
| 1   |0010|
|-1   |0011|
| 2   |0100|
|-2   |0101|
| 3   |0110|
|-3   |0111|
| 4   |1000|
|-4   |1001|
| 5   |1010|
|-5   |1011|
| 6   |1100|
|-6   |1101|
| 7   |1110|
|-7   |1111|

### Resolutions 8-15

These values are stored in sign-magnitude form, with the low bit representing the sign.

The number of bits used to store each value is determined by `Resolution - 3`. The value `0` uses one fewer bit than the rest of the values because it does not have a sign.

An example using Resolution 8 is given below. `x` represents the value, and `s` represents the sign.

|Value|Code |
|:---:|:---:|
|0    |0000 |
|X    |xxxxs|