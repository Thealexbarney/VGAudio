### Huffman Code Sets

There are 2 sets of codebooks, each with a slightly different bit distribution.
The set that is used can be chosen per quantization unit (QU), and depends on the scale factors.

#### Codebook Set A

These codebooks have a more even bit distribution. The longest codes in a codebook might be about twice the length of the shortest ones.
This set is the default, and is used unless otherwise specified.

#### Codebook Set B

These codebooks have a wider bit distribution. The longest codes in a codebook might be 3-5x the length of the shortest ones.
This set is only used for quantization units above 8, and has a set of rules to decide when it is used.

In these rules, `avg` is the floor of the average of the first 12 scale factors.
Codebook set B is used when at least one of these four conditions are met.

For quantization units above 8:
- The scale factor is 3 or more values above either of its two neighboring scale factors.
- The combined distance of a scale factor above its neighboring scale factors adds up to 3 or more.

For quantization units 12 - 19:
- The scale factor is 2 or more values above either of its two neighboring scale factors, and is greater than or equal to `avg`.

For quantization units above 20:
- The scale factor is 2 or more values above either of its two neighboring scale factors, and is greater than or equal to `avg - 1`.
