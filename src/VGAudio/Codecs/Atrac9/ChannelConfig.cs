namespace VGAudio.Codecs.Atrac9
{
    public class ChannelConfig
    {
        public ChannelConfig(params BlockType[] blockTypes)
        {
            BlockCount = blockTypes.Length;
            BlockTypes = blockTypes;
            foreach (BlockType type in blockTypes)
            {
                ChannelCount += Block.BlockTypeToChannelCount(type);
            }
        }

        public int BlockCount { get; }
        public BlockType[] BlockTypes { get; }
        public int ChannelCount { get; }
    }
}
