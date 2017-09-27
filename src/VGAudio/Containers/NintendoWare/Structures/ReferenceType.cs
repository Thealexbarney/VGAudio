namespace VGAudio.Containers.NintendoWare.Structures
{
    public enum ReferenceType
    {
        ByteTable = 0x0100,
        ReferenceTable = 0x0101,
        GcAdpcmInfo = 0x0300,
        SampleData = 0x1F00,
        StreamInfoBlock = 0x4000,
        StreamSeekBlock = 0x4001,
        StreamDataBlock = 0x4002,
        StreamRegionBlock = 0x4003,
        StreamPrefetchDataBlock = 0x4004,
        StreamInfo = 0x4100,
        TrackInfo = 0x4101,
        ChannelInfo = 0x4102,
        WaveInfoBlock = 0x7000,
        WaveDataBlock = 0x7001,
        WaveChannelInfo = 0x7100
    }
}