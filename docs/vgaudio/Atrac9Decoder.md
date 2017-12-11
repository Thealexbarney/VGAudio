# Atrac9 Decoder

To decode an ATRAC9 stream, a 4-byte value called the Configuration Data, and the ATRAC9 stream are needed.

### Initialization
Once you've extracted these from the file containing the ATRAC9 data, create and initialize the decoder:  
`Atrac9Decoder decoder = new Atrac9Decoder().Initialize(configData);`

### Audio Buffers
Each time `Decode` is called it requires two buffers to be passed to it, one for the input ATRAC9 data, and one for the output PCM data.
We'll call these `atrac9Buffer` and `pcmBuffer` respectively.

These buffers must be large enough to hold one superframe of audio data. 
Those values can be retrieved from an initialized decoder.

`byte[] atrac9Buffer` must be at least `decoder.Config.SuperframeBytes` bytes long.  
`short[][] pcmBuffer` must have dimensions of at least `[decoder.Config.ChannelCount][decoder.Config.SuperframeSamples]`

### Decoding
The decoder will decode one superframe of the ATRAC9 stream at a time. The superframes in a stream all have a constant size.

With one superframe of ATRAC9 data in `atrac9Buffer`, call `Decode`:
`decoder.Decode(atrac9Buffer, pcmBuffer);`  
Repeat for each superframe.

### Example

`byte[][] atrac9Data` is an array of all the superframes in the ATRAC9 stream.

```
// Initialize the decoder
var decoder = new Atrac9Decoder();
decoder.Initialize(configData);

// Create a buffer for the output PCM
var pcmBuffer = new short[decoder.Config.ChannelCount][];
for (int i = 0; i < pcmBuffer.Length; i++)
{
    pcmBuffer[i] = new short[decoder.Config.SuperframeSamples];
}

// Decode each superframe
for (int i = 0; i < atrac9Data.Length; i++)
{
    decoder.Decode(atrac9Data[i], pcmBuffer);

    // Use the decoded audio in pcmBuffer however you want
}

```