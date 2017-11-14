using System.Collections.Generic;
using System.Linq;

namespace VGAudio.Codecs.CriAdx
{
    public static partial class CriAdxEncryption
    {
        private static readonly string[] KeyStrings =
        {
            "(C)2005 MOSS LTD. BMW Z4",
            "3x5k62bg9ptbwy",
            "CS-GGNX+",
            "GHM",
            "GHMSC",
            "karaage",
            "mituba",
            "morio",
            "ranatus",
            "sakakit4649"
        };

        private static readonly ulong[] KeyCodes =
        {
            12160794,
            19910623,
            416383518,
            683461999,
            268736153152,
            145552191146490718,
            4867249871962584729
        };

        private static readonly CriAdxKey[] LcgParameters =
        {
            new CriAdxKey(0x40a9, 0x46b1, 0x62ad),
            new CriAdxKey(0x4133, 0x5a01, 0x5723),
            new CriAdxKey(0x413b, 0x543b, 0x57d1),
            new CriAdxKey(0x41ef, 0x463d, 0x5507),
            new CriAdxKey(0x4369, 0x486d, 0x5461),
            new CriAdxKey(0x440d, 0x4327, 0x4fff),
            new CriAdxKey(0x45af, 0x5f27, 0x52b1),
            new CriAdxKey(0x4601, 0x671f, 0x0455),
            new CriAdxKey(0x47e1, 0x60e9, 0x51c1),
            new CriAdxKey(0x481d, 0x4f25, 0x5243),
            new CriAdxKey(0x4969, 0x5deb, 0x467f),
            new CriAdxKey(0x4c01, 0x549d, 0x676f),
            new CriAdxKey(0x4c73, 0x4d8d, 0x5827),
            new CriAdxKey(0x4d06, 0x663b, 0x7d09),
            new CriAdxKey(0x4d65, 0x5eb7, 0x5dfd),
            new CriAdxKey(0x4d82, 0x5243, 0x0685),
            new CriAdxKey(0x4f7b, 0x5071, 0x4c61),
            new CriAdxKey(0x53e9, 0x586d, 0x4eaf),
            new CriAdxKey(0x54d1, 0x526d, 0x5e8b),
            new CriAdxKey(0x5563, 0x5047, 0x43ed),
            new CriAdxKey(0x55b7, 0x67e5, 0x5387),
            new CriAdxKey(0x5803, 0x4555, 0x47bf),
            new CriAdxKey(0x586d, 0x5d65, 0x63eb),
            new CriAdxKey(0x59ed, 0x4679, 0x46c9),
            new CriAdxKey(0x5a11, 0x67e5, 0x6751),
            new CriAdxKey(0x5c33, 0x4133, 0x4ce7),
            new CriAdxKey(0x5e75, 0x4a89, 0x4c61),
            new CriAdxKey(0x5f5d, 0x552b, 0x5507),
            new CriAdxKey(0x5f5d, 0x58bd, 0x55ed),
            new CriAdxKey(0x5f65, 0x5b3d, 0x5f65),
            new CriAdxKey(0x5fc5, 0x63d9, 0x599f),
            new CriAdxKey(0x6157, 0x6809, 0x4045),
            new CriAdxKey(0x62ad, 0x4b13, 0x5957),
            new CriAdxKey(0x6305, 0x509f, 0x4c01),
            new CriAdxKey(0x645d, 0x6011, 0x5c29),
            new CriAdxKey(0x64ab, 0x5297, 0x632f),
            new CriAdxKey(0x6731, 0x645d, 0x566b),
            new CriAdxKey(0x6809, 0x5fd5, 0x5bb1)
        };

        public static readonly List<CriAdxKey> Keys8 = KeyStrings.Select(key => new CriAdxKey(key)).Concat(LcgParameters).ToList();
        public static readonly List<CriAdxKey> Keys9 = KeyCodes.Select(key => new CriAdxKey(key)).ToList();
    }
}