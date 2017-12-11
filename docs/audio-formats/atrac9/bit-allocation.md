# Bit Allocation

ATRAC9 uses a block's `gradient` and `scale factors` to determine the bit allocation for the spectral data.
Because the bit allocation is not explicitly stored, the algorithm used must be exactly the same in both the encoder and decoder, with no room for rounding error.

Bit allocation is controlled by a quantization unit's `precision` and `fine_precision`.
The `precision`, ranging from 1-15, is equal to the number of bits that will be used to quantize each of the spectral coefficients in a given `quantization unit` (QU).

If a calculated `precision` is greater than `15`, the excess bits above `15` are assigned to the `fine_precision`.
The `fine_precision` is equal to the number of bits that will be used to encode a second value that further increases quantization accuracy.

## Gradient

The gradient is a curve that determines how many bits will be allocated to a spectral coefficient based on the index of its QU.

This curve is 31 units long, 1 for each possible QU.
It is a mapping from a QU index to a value that is used to determine bit allocation. 
The lower this value, the more bits a QU receives.

Each block contains parameters for creating the gradient used to decode that block. These parameters are:

- `mode` - The mode used when applying the gradient. 4 modes possible (0-3).
- `start_unit` - The QU at which the gradient begins increasing.
- `end_unit` - The QU at which the gradient finishes increasing. Always `31` for gradient modes 1-3.
- `min_value` - The minimum value of the curve.
- `max_value` - The maximum value of the curve. Always `31` for gradient modes 1-3.
- `boundary` - Each value below this scale factor is increased by `1`.

### Base Gradient

There is one pre-defined 48-value curve that's used as a base for all generated gradients:  
`1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 4, 4, 5, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 16, 18, 19, 20, 21, 22, 23, 24, 25, 26, 26, 27, 27, 28, 28, 28, 29, 29, 29, 29, 30, 30, 30, 30`

This base gradient is used to create scaled-down versions of itself for all lengths of 1-48.

The function for the scaled curves is as follows, where `x` is the index in the scaled curve,
`l` is the length of the scaled curve, and `base` is the base gradient.  
`f(x, l) = base[floor(x * 48 / l)]`

### Generation

1. Set all scale factors below `start_unit` to `min_value`.
2. Set all scale factors at or above `end_unit` to `max_value`.
3. Use the scaled base gradient of length `max_value - min_value` to transition the QUs from `start_unit` to `end_unit` from `min_value` to `max_value`:
   - From values `0` to `max_value - min_value`:  
     `gradient[start_unit + i] = min_value + 1 + scaled_base[i] * ((max_value - min_value - 1) / 31)`

## Frequency Masking

ATRAC9 uses partial frequency masking to allocate more bits to QUs with significantly more power than their immediate neighbors.
To do this, a value is assigned to each QU indicating its prominence over its neighbor QUs based on its scalefactor.
These values are used as a mask when performing bit allocation.

To calculate a QU's prominence, we look at it and its two neighboring QUs. (The first and last QUs will only have 1 neighbor).
If a QU's scale factor is less than 2 values greater than its neighbor's scale factor, it has a prominence of `0`.
Otherwise, the prominence will be its distance above the neighboring scale factor, up to a maximum of `5`.

This is repeated for each of a QU's neighbors, and the results are added together to obtain the QU's prominence.
The maximum prominence a QU can have is `10`.


## Computing Precisions

The scale factors, gradient, and mask are used to calculate the precision for each QU.

1. Calculate each QU's `precision` from an equation determined by the gradient's `mode`:

    #### Gradient Mode 0
    `precision = scale_factor - gradient`

    #### Gradient Mode 1
    `precision = (scale_factor - gradient + mask) * (4 / 8)`

    #### Gradient Mode 2
    `precision = (scale_factor - gradient + mask) * (3 / 8)`

    #### Gradient Mode 3
    `precision = (scale_factor - gradient + mask) * (2 / 8)`

2. Set the value of any precisions below `1` to `1`.
3. Increase the `precision` of every QU with an index below `boundary` by `1`.
4. If a `precision` is above `15`, set that QU's `fine_precision` to `precision - 15`, and set the `precision` to `15`.