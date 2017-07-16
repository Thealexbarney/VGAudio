using System.Collections.Generic;

namespace VGAudio.Codecs.CriHca
{
    public static partial class CriHcaEncryption
    {
        public static readonly List<CriHcaKey> Keys = new List<CriHcaKey>
        {
            // HCA Decoder default
            new CriHcaKey(9621963164387704),

            // Phantasy Star Online 2
            new CriHcaKey(24002584467202475),

            // Ro-Kyu-Bu! 2? (PSP)
            new CriHcaKey(2012082716),

            // Jojo All Star Battle (PS3)
            new CriHcaKey(19700307),

            // Idolm@ster Cinderella Stage (iOS/Android)
            new CriHcaKey(59751358413602),

            // Grimoire (iOS/Android)
            new CriHcaKey(5027916581011272),

            // Idol Connect (iOS/Android)
            new CriHcaKey(2424),

            // Ro-Kyu-Bu! (Vita)
            // Wax Cube
            new CriHcaKey(1234253142),

            // Kamen Rider Battle Rush (iOS/Android)
            new CriHcaKey(29423500797988784),

            // SD Gundam Strikers
            new CriHcaKey(30260840980773),

            // Sonic Runners
            new CriHcaKey(19910623),

            // Old Phantasy Star Online 2
            new CriHcaKey(61891147883431481),

            // FGO
            new CriHcaKey(12345),

            //Raramagi (Mobile)
            new CriHcaKey(45719322)
        };
    }
}
