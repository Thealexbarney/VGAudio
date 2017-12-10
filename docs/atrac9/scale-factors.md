# Scale Factors

There are multiple modes available for encoding the scale factors.
There is one scale factor for each quantization unit in a block. This number is given by `extension_unit` in the block's header.

The bitstream is as follows:

|Name|Bits|Description|
|:-:|:-:|:-:|
|coding_mode|2|The method used to store the scale factors|
|scale_factors|varies|The encoded scale factors, stored according to `coding_mode`|

The method used to encode the scale factors depends on the channel number and `coding_mode`.
Each of these 4 methods are described later.

|Channel #|Mode|Method Used|
|:-:|:-:|:-:|
|0|0|B|
|0|1|A|
|0|2|C using the previous frame|
|0|3|D using the previous frame|
|1|0|B|
|1|1|C using the main channel|
|1|2|D using the main channel|
|1|3|C using the previous frame|

## Encoding Techniques

ATRAC9 uses several lossless encoding techniques to encode the scale factors.

### Huffman Coding

ATRAC9 uses standard prefix codes to store scale factors, allowing more common values to be encoded using fewer bits.

There are multiple codebooks used based on the bit length and signedness of the encoded values.
In all these codebooks, values closer to 0 are encoded with fewer bits.

### Delta Coding

Because the differences between scale factors can be small, those differences are often stored instead of the full values.
An initial value is stored, followed by the deltas for the remaining values.

For example, take the following values: `10, 9, 11, 10, 12, 14, 15, 17`

Storing these values directly requires 5 bits per value. `Ceiling(Log2(17)) = 5`  
Instead, we can encode an initial value `10` and the subsequent deltas: `-1, 2, -1, 2, 2, 1, 2`
 
Storing these deltas requires 3 bits per value (Possible range of -4 to 3), saving some storage space.
If further encoded with Huffman coding, the storage will be reduced even more because most of the delta values are close to 0.

To save space when using delta coding, the maximum value will wrap around to the minimum value, and vice versa when adding the delta.
For example, using a bit length of 3 and adding a delta of 5 to the value 6 results in a value of 3. `(5 + 6) mod 8 == 3` 

### Offset Coding

If the values to be encoded have a small range, they can more efficiently be encoded as offsets to a base value.

Take the example used above again: `10, 9, 11, 10, 12, 14, 15, 17`  
We'll choose the minimum value `9` as the base, and then find the offsets of each value: `1, 0, 2, 1, 3, 5, 6, 8`

Alternatively, the values can be stored as offsets to another sequence of values.
ATRAC9 has 8 pre-defined base sequences used for encoding scale factors.

## Coding Methods

### Method A: Directly coded offset

Scale factors are offset encoded, and then stored directly with a constant bit length.

|Name|Condition|Bits|Description|
|:-:|:-:|:-:|:-:|
|bit_length||2|The bit length of the encoded values. Add 2 to the read value.|
|base_value|`bit_length < 5`|5|The base value. `0` if `bit_length == 5`|
|offset_values||`bit_length * extension_unit`|The offset from `base_value` for each quantization unit.|

### Method B: Huffman coded delta of offset with weights

|Name|Bits|Description|
|:-:|:-:|:-:|
|weight_index|3|The index of the sequence of 31 weights to be used. One for each quantization unit.|
|base_value|5|The base value.|
|bit_length|2|The bit length of the encoded delta values. Add 3 to the read value.|
|initial_value|`bit_length`|The initial value for the delta coding.|
|delta_values|varies|Hufman encoded unsigned deltas.|

To encode:

1. Add the weight values to the scale factors of their corresponding quantization units.
2. Subtract `base_value` from each scale factor.
3. Delta encode the scale factors.
4. Directly write `initial_value` using `bit_length` bits.
5. Using the codebook for the `bit_length`, Huffman encode the delta values.

### Method C: Huffman coded distance to baseline

This method encodes a set of scale factors by using another set of scale factors as a baseline.

If the baseline has fewer scale factors than the set being coded, the remaining values are directly stored as 5-bit integers.

|Name|Condition|Bits|Description|
|:-:|:-:|:-:|:-:|
|bit_length||2|The bit length of the encoded values. Add 2 to the read value.|
|distance_values||varies|Hufman encoded signed distances. The codebooks for signed scale factor values are used.<br/>Wraps around in both the positive and negative direction.|
|direct_values|The block being read has more scale factors than the baseline.|5 each|The remaining scale factors.|

### Method D: Huffman coded delta of offset with baseline

This method is similar to Method A, but uses another set of scale factors as a baseline instead of pre-defined weights.

If the baseline has fewer scale factors than the set being coded, the remaining values are directly stored as 5-bit integers.

|Name|Condition|Bits|Description|
|:-:|:-:|:-:|:-:|
|base_value||5|The base value. Stored with offset binary representation.|
|bit_length||2|The bit length of the encoded values. Add 1 to the read value.|
|initial_value||`bit_length`|The initial value for the delta coding.|
|delta_values||varies|Hufman encoded unsigned deltas.|
|direct_values|The block being read has more scale factors than the baseline.|5 each|The remaining scale factors.|

To decode:

1. Read the Huffman encoded values and do delta decoding with them to get the offsets.
2. Add the `base_value` to each offset.
3. Add the vector of baseline scale factors to the vector being decoded.
4. Read any leftover values from the bitstream.