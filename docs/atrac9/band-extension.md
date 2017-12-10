# ATRAC9 Band Extension
Band Extension is a feature of ATRAC9 that reconstructs high-frequency bands using the data from the low-frequency bands and a small set of values.

The number of stored bands must be from 5 and 10 inclusive when using Band Extension, which means the number of stored quantization units must be between 13 and 20 inclusive.

The high-frequency quantization units are split into 3 different groups (A, B, C) according to the following table:

|Quant<br/>Units|0..12|13|14|15|16|17|18|19|20|21|22|23
|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
|13|-|A|A|A|B|B|B|B|B|C|-|-|
|14|-|-|A|A|A|A|B|B|B|B|-|-|
|15|-|-|-|A|A|A|A|A|B|B|-|-|
|16|-|-|-|-|A|A|A|A|A|B|-|-|
|18|-|-|-|-|-|-|A|A|A|A|A|B|
|20|-|-|-|-|-|-|-|-|A|A|A|A|

## Initializing High-frequency Bands

The spectral coefficients of each group are populated by copying the coefficients from the low-frequency bands. Each group is filled with a reverse copy of the coefficients before it.

For example, say we have the following coefficients and groups:

|Group|-|-|-|-|-|A|A|B|B|B|B|B|C|C|
|-----|-|-|-|-|-|-|-|-|-|-|-|-|-|-|
|Coeff|0|1|2|3|4| | | | | | | | | |

Group A would be populated as follows:

|Group|-|-|-|-|-|A|A|B|B|B|B|B|C|C|
|-----|-|-|-|-|-|-|-|-|-|-|-|-|-|-|
|Coeff|0|1|2|3|4|4|3| | | | | | | |

Followed by Group B:

|Group|-|-|-|-|-|A|A|B|B|B|B|B|C|C|
|-----|-|-|-|-|-|-|-|-|-|-|-|-|-|-|
|Coeff|0|1|2|3|4|4|3|3|4|4|3|2| | |

And finally Group C:

|Group|-|-|-|-|-|A|A|B|B|B|B|B|C|C|
|-----|-|-|-|-|-|-|-|-|-|-|-|-|-|-|
|Coeff|0|1|2|3|4|4|3|3|4|4|3|2|2|3|

## Band Extension Modes
After the high-frequency bands are initialized, each spectral coefficient in those bands is scaled according to the Band Extension mode and encoded values.

There are 5 possible Band Extension modes, which are set on a frame-by-frame basis. Mode 4 is used if and only if the frame has fewer than 8 stored bands. Each mode encodes a certain number of values that are used when applying Band Extension.

### Mode 0

The number of values encoded by this mode depends on how many quantization units are stored for the channel. Up to 4 values are encoded. The bit-length of each value is listed below.

|Units<br/>Encoded|Value 1<br/>Bits|Value 2<br/>Bits|Value 3<br/>Bits|Value 4<br/>Bits|
|:-:|:-:|:-:|:-:|:-:|
|16|5|4|3|3|
|18|4|4|3|4|
|20|4|5|--|--|

### Mode 1

This mode does not encode any values into the bitstream. Each spectral coefficient is set to a random value between 1 and -1, and is then scaled according to the scale factor of the quantization unit it's in.

### Mode 2
This mode encodes 2 or 1 6-bit numbers which correspond to indexes in the same table. These values from the tables are `Group_A_Scale` and `Group_B_Scale`.

Every spectral coefficient in each group is then multiplied by its respective scale.

### Mode 3
This mode encodes 2 4-bit numbers which correspond to indexes in two different tables. The value from the first table is the `initial` value of the scale, and the second is the
`rate`.

The scale for the n^th^ spectral coefficient is modeled by the exponential growth/decay equation `scale = initial * 2^(rate * n)`, where `n` is 1-indexed. The Band Extension groups have no effect in this scaling mode.

### Mode 4
This mode encodes 1 3-bit number which corresponds to an index in a table. This table contains a curve with a range from 0 to 1. The value `x` from the table is used as follows:
````
Group_A_Scale = x * 0.7079468;
Group_B_Scale = x * 0.5011902;
Group_C_Scale = x * 0.3548279;
````

Every spectral coefficient in each group is then multiplied by its respective scale.