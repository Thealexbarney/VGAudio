using System.Collections.Generic;

namespace VGAudio.Codecs.CriHca
{
    public static partial class CriHcaEncryption
    {
        public static List<CriHcaKey> Keys { get; } = new List<CriHcaKey>
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
            new CriHcaKey(45719322),

            // Idolm@ster Million Live (iOS/Android)
            new CriHcaKey(765765765765765),

            // Black Knight and White Devil (iOS/Android)
            new CriHcaKey(3003875739822025258),

            // Puella Magi Madoka Magica Side Story: Magia Record (iOS/Android)
            new CriHcaKey(20536401),

            // The Tower of Princess (iOS/Android)
            new CriHcaKey(9101518402445063),

            // Fallen Princess (iOS/Android)
            new CriHcaKey(145552191146490718),

            // Diss World (iOS/Android)
            new CriHcaKey(9001712656335836006),

            // イケメンヴァンパイア 偉人たちと恋の誘惑 (iOS/Android)
            new CriHcaKey(45152594117267709),

            // Super Robot Wars X-Ω (iOS/Android)
            new CriHcaKey(165521992944278),

            // BanG Dream! Girls Band Party! (iOS/Android)
            new CriHcaKey(8910)

        };
    }
}
