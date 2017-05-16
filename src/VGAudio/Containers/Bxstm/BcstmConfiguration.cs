namespace VGAudio.Containers.Bxstm
{
    public class BcstmConfiguration : BxstmConfiguration
    {
        /// <summary>
        /// If <c>true</c>, include track information in the BCSTM
        /// header. Default is <c>true</c>.
        /// </summary>
        public bool IncludeTrackInformation { get; set; } = true;
        /// <summary>
        /// If <c>true</c>, include an extra chunk in the header
        /// after the stream info and before the track offset table.
        /// The purpose of this chunk is unknown.
        /// Default is <c>false</c>.
        /// </summary>
        public bool InfoPart1Extra { get; set; }
    }
}