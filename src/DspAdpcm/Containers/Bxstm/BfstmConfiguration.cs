namespace DspAdpcm.Containers.Bxstm
{
    public class BfstmConfiguration : BxstmConfiguration, IConfiguration
    {
        public bool IncludeUnalignedLoopPoints { get; set; }
    }
}