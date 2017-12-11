# ATRAC9 Spectrum
- The spectrum for ATRAC9 ranges from 0 to 24000 Hz, with 256 frequency bins of 93.75 Hz each.  
- This spectrum is divided into 18 bands, each containing 8, 16 or 32 bins.  
- Each band is further divided into 1, 2 or 4 quantization units.
- There are a total of 30 quantization units, each containing 2, 4, 8 or 16 bins.

## Band to Quantization Unit Relationship
`Frequency` - The frequency at the top end of the band.  
`Frequency Bins` - The number of frequency bins contained in each quantization unit.  
`Total Frequency Bins` - The total number of frequency bins contained in this quantization unit and below.

|Band|Frequency<br/>(Hz)|Quant<br/>Unit|Frequency<br/>Bins|Total<br/>Frequency<br/>Bins|
|:-:|:-:|:-:|:-:|:-:|
|0|750|0|2|2|
|||1|2|4|
|||2|2|6|
|||3|2|8|
|1|1500|4|2|10|
|||5|2|12|
|||6|2|14|
|||7|2|16|
|2|2250|8|4|20|
|||9|4|24|
|3|3000|10|4|28|
|||11|4|32|
|4|3750|12|8|40|
|5|4500|13|8|48|
|6|5250|14|8|56|
|7|6000|15|8|64|
|8|7500|16|8|72|
|||17|8|80|
|9|9000|18|8|88|
|||19|8|96|
|10|10500|20|16|112|
|11|12000|21|16|128|
|12|13500|22|16|144|
|13|15000|23|16|160|
|14|16500|24|16|176|
|15|18000|25|16|192|
|16|21000|26|16|208|
|||27|16|224|
|17|24000|28|16|240|
|||29|16|256|