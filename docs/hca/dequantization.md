# Dequantization

Dequantization of the spectral coefficients has 2 steps:

1. Normalize the coefficients to a range of -1 to 1.
2. Scale the coefficients using their respective scale factors.

## Normalization

Each quantized coefficient falls within a certain range of values determined by the coefficient's resolution as outlined [here](spectrum-decoding.md).

To normalize those values into a range of -1 to 1, each coefficient is multiplied by a scale from the equation `2 / levels_in_resolution`, Where `levels_in_resolution` is the number of values that can be represented by that resolution.

This table shows the scale for each resolution, and the maximum value with that resolution after quantization.

| Resolution |Levels | Max<br/>Quantized<br/>Value  | Scale | Max<br/>Normalized<br/>Value|
|:----------:|:-----:|:-----:|:--------------|:------------|
| 1          | 3     | 1     | 0.6666667     | 0.6666667   |
| 2          | 5     | 2     | 0.4           | 0.8         |
| 3          | 7     | 3     | 0.2857143     | 0.8571429   |
| 4          | 9     | 4     | 0.222222224   | 0.8888889   |
| 5          | 11    | 5     | 0.181818187   | 0.909090936 |
| 6          | 13    | 6     | 0.15384616    | 0.923077    |
| 7          | 15    | 7     | 0.13333334    | 0.9333334   |
| 8          | 31    | 15    | 0.06451613    | 0.9677419   |
| 9          | 63    | 31    | 0.0317460336  | 0.984127045 |
| 10         | 127   | 63    | 0.0157480314  | 0.992126    |
| 11         | 255   | 127   | 0.007843138   | 0.9960785   |
| 12         | 511   | 255   | 0.0039138943  | 0.99804306  |
| 13         | 1023  | 511   | 0.00195503421 | 0.9990225   |
| 14         | 2047  | 1023  | 0.0009770396  | 0.9995115   |
| 15         | 4095  | 2047  | 0.0004884005  | 0.999755859 |

## Scaling

After the coefficient values are normalized, they are scaled according to their individual scale factors, which can range from 0 to 63.

This scale comes from the equation `sqrt(128) * (2^(53/128))^(scale_factor - 63)`.  <!-- \sqrt{128}(2^{\frac{53}{128}})^{x-63} -->

The following table lists every scale factor along with the scale to apply on a coefficient.

| Scale Factor | Scale        |
|:------------:|:-------------|
| 0            | 1.588383E-07 |
| 1            | 2.116414E-07 |
| 2            | 2.819978E-07 |
| 3            | 3.757431E-07 |
| 4            | 5.006523E-07 |
| 5            | 6.670855E-07 |
| 6            | 8.888464E-07 |
| 7            | 1.184328E-06 |
| 8            | 1.578037E-06 |
| 9            | 2.102628E-06 |
| 10           | 2.80161E-06  |
| 11           | 3.732956E-06 |
| 12           | 4.973912E-06 |
| 13           | 6.627403E-06 |
| 14           | 8.830567E-06 |
| 15           | 1.176613E-05 |
| 16           | 1.567758E-05 |
| 17           | 2.088932E-05 |
| 18           | 2.783361E-05 |
| 19           | 3.708641E-05 |
| 20           | 4.941514E-05 |
| 21           | 6.584233E-05 |
| 22           | 8.773047E-05 |
| 23           | 0.0001168949 |
| 24           | 0.0001557546 |
| 25           | 0.0002075325 |
| 26           | 0.0002765231 |
| 27           | 0.0003684484 |
| 28           | 0.0004909326 |
| 29           | 0.0006541346 |
| 30           | 0.0008715902 |
| 31           | 0.001161335  |
| 32           | 0.001547401  |
| 33           | 0.002061807  |
| 34           | 0.002747219  |
| 35           | 0.003660484  |
| 36           | 0.004877347  |
| 37           | 0.006498737  |
| 38           | 0.008659128  |
| 39           | 0.0115377    |
| 40           | 0.01537321   |
| 41           | 0.02048377   |
| 42           | 0.02729324   |
| 43           | 0.0363664    |
| 44           | 0.04845578   |
| 45           | 0.06456406   |
| 46           | 0.08602725   |
| 47           | 0.1146255    |
| 48           | 0.1527307    |
| 49           | 0.2035034    |
| 50           | 0.2711546    |
| 51           | 0.3612952    |
| 52           | 0.4814015    |
| 53           | 0.641435     |
| 54           | 0.8546689    |
| 55           | 1.138789     |
| 56           | 1.517359     |
| 57           | 2.021779     |
| 58           | 2.693884     |
| 59           | 3.589418     |
| 60           | 4.782658     |
| 61           | 6.372569     |
| 62           | 8.491017     |
| 63           | 11.31371     |
