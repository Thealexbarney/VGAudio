namespace VGAudio.Containers.NintendoWare
{
    public class NwVersion
    {
        public uint Version { get; }
        public byte Major { get; }
        public byte Minor { get; }
        public byte Micro { get; }
        public byte Revision { get; }

        public NwVersion(uint version)
        {
            Version = version;
            Major = (byte)((version >> 24) & 0xFF);
            Minor = (byte)((version >> 16) & 0xFF);
            Micro = (byte)((version >> 8) & 0xFF);
            Revision = (byte)(version & 0xFF);
        }

        public NwVersion(byte major = 0, byte minor = 0, byte micro = 0, byte revision = 0)
        {
            Major = major;
            Minor = minor;
            Micro = micro;
            Revision = revision;
            Version = (uint)(major << 24 | minor << 16 | micro << 8 | revision);
        }
    }
}
