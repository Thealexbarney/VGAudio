using System;

namespace VGAudio.Utilities.Riff
{
    public static class MediaSubtypes
    {
        public static Guid MediaSubtypePcm { get; } = new Guid("00000001-0000-0010-8000-00AA00389B71");
        public static Guid MediaSubtypeAtrac9 { get; } = new Guid("47E142D2-36BA-4d8d-88FC-61654F8C836C");
    }
}
