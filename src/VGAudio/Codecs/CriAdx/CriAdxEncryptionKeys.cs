using System.Collections.Generic;

namespace VGAudio.Codecs.CriAdx
{
    public static partial class CriAdxEncryption
    {
        public static readonly List<CriAdxKey> Keys8 = new List<CriAdxKey>
        {
            // Clover Studio (GOD HAND, Okami) 
            // Found in the game's executable
            new CriAdxKey("karaage"),

            // Grasshopper Manufacture 0 (Blood+)
            // This is estimated
            new CriAdxKey(0x5f5d, 0x58bd, 0x55ed),

            // Grasshopper Manufacture 1 (Killer7)
            // Found in the game's executable
            new CriAdxKey("GHM"),

            // Grasshopper Manufacture 2 (Samurai Champloo)
            // Found in the game's executable
            new CriAdxKey("GHMSC"),

            // Moss Ltd (Raiden III)
            // Found in the game's executable
            new CriAdxKey("(C)2005 MOSS LTD. BMW Z4"),

            // Sonic Team 0 (Phantasy Star Universe)
            // Found in the game's executable
            new CriAdxKey("3x5k62bg9ptbwy"),

            // G.rev 0 (Senko no Ronde)
            // Found in the game's executable
            new CriAdxKey("ranatus"),

            // Sonic Team 1 (NiGHTS: Journey of Dreams)
            // Found in the game's executable
            new CriAdxKey("sakakit4649"),

            // From guessadx (unique?), unknown source
            new CriAdxKey(0x586d, 0x5d65, 0x63eb),

            // Navel (Shuffle! On the Stage)
            // 2nd key from guessadx
            new CriAdxKey(0x4969, 0x5deb, 0x467f),

            // Success (Aoishiro)
            // 1st key from guessadx
            new CriAdxKey(0x4d65, 0x5eb7, 0x5dfd),

            // Sonic Team 2 (Sonic and the Black Knight)
            // Found in the game's executable
            new CriAdxKey("morio"),

            // Enterbrain (Amagami)
            // Found in the game's executable
            new CriAdxKey("mituba"),

            // Yamasa (Yamasa Digi Portable: Matsuri no Tatsujin)
            // Confirmed unique with guessadx
            new CriAdxKey(0x4c01, 0x549d, 0x676f),

            // Kadokawa Shoten (Fragments Blue)
            // Confirmed unique with guessadx
            new CriAdxKey(0x5803, 0x4555, 0x47bf),

            // Namco (Soulcalibur IV)
            // Confirmed unique with guessadx
            new CriAdxKey(0x59ed, 0x4679, 0x46c9),

            // G.rev 1 (Senko no Ronde DUO)
            // From guessadx
            new CriAdxKey(0x6157, 0x6809, 0x4045),

            // ASCII Media Works 0 (Nogizaka Haruka no Himitsu: Cosplay Hajimemashita)
            // 2nd from guessadx, other was {0x45ad, 0x5f27, 0x10fd}
            new CriAdxKey(0x45af, 0x5f27, 0x52b1),

            // D3 Publisher 0 (Little Anchor)
            // Confirmed unique with guessadx
            new CriAdxKey(0x5f65, 0x5b3d, 0x5f65),

            // Marvelous 0 (Hanayoi Romanesque: Ai to Kanashimi)
            // 2nd from guessadx, other was {0x5562, 0x5047, 0x1433}
            new CriAdxKey(0x5563, 0x5047, 0x43ed),

            // Capcom (Mobile Suit Gundam: Gundam vs. Gundam NEXT PLUS)
            // Found in the game's executable
            new CriAdxKey("CS-GGNX+"),

            // Developer: Bridge NetShop
            // Publisher: Kadokawa Shoten (Shoukan Shoujo: Elemental Girl Calling)
            // Confirmed unique with guessadx
            new CriAdxKey(0x4f7b, 0x5071, 0x4c61),

            // Developer: Net Corporation
            // Publisher: Tecmo (Rakushou! Pachi-Slot Sengen 6: Rio 2 Cruising Vanadis)
            // Confirmed unique with guessadx
            new CriAdxKey(0x53e9, 0x586d, 0x4eaf),

            // Developer: Aquaplus
            // Tears to Tiara Gaiden Avalon no Kagi (PS3)
            // Confirmed unique with guessadx
            new CriAdxKey(0x47e1, 0x60e9, 0x51c1),

            // Developer: Broccoli
            // Neon Genesis Evangelion: Koutetsu no Girlfriend 2nd (PS2)
            // Confirmed unique with guessadx
            new CriAdxKey(0x481d, 0x4f25, 0x5243),

            // Developer: Marvelous
            // Futakoi Alternative (PS2)
            // Confirmed unique with guessadx
            new CriAdxKey(0x413b, 0x543b, 0x57d1),

            // Developer: Marvelous
            // Gakuen Utopia - Manabi Straight! KiraKira Happy Festa! (PS2)
            // 2nd from guessadx, other was {0x440b, 0x4327, 0x564b}
            new CriAdxKey(0x440d, 0x4327, 0x4fff),

            // Developer: Datam Polystar
            // Soshite Kono Uchuu ni Kirameku Kimi no Shi XXX (PS2)
            // Confirmed unique with guessadx
            new CriAdxKey(0x5f5d, 0x552b, 0x5507),

            // Developer: Sega
            // Sakura Taisen: Atsuki Chishio Ni (PS2)
            // Confirmed unique with guessadx
            new CriAdxKey(0x645d, 0x6011, 0x5c29),

            // Developer: Sega
            // Sakura Taisen 3 ~Paris wa Moeteiru ka~ (PS2)
            // Confirmed unique with guessadx
            new CriAdxKey(0x62ad, 0x4b13, 0x5957),

            // Developer: Jinx
            // Sotsugyou 2nd Generation (PS2)
            // 1st from guessadx, other was {0x6307, 0x509f, 0x2ac5}
            new CriAdxKey(0x6305, 0x509f, 0x4c01),

            // La Corda d'Oro (2005)(-)(Koei)[PSP]
            // Confirmed unique with guessadx
            new CriAdxKey(0x55b7, 0x67e5, 0x5387),

            // Nanatsuiro * Drops Pure!! (2007)(Media Works)[PS2]
            // Confirmed unique with guessadx
            new CriAdxKey(0x6731, 0x645d, 0x566b),

            // Shakugan no Shana (2006)(Vridge)(Media Works)[PS2]
            // Confirmed unique with guessadx
            new CriAdxKey(0x5fc5, 0x63d9, 0x599f),

            // Uragiri wa Boku no Namae o Shitteiru (2010)(Kadokawa Shoten)[PS2]
            // Confirmed unique with guessadx
            new CriAdxKey(0x4c73, 0x4d8d, 0x5827),

            // StormLover Kai!! (2012)(D3 Publisher)[PSP]
            // Confirmed unique with guessadx
            new CriAdxKey(0x5a11, 0x67e5, 0x6751),

            // Sora no Otoshimono - DokiDoki Summer Vacation (2010)(Kadokawa Shoten)[PSP]
            // Confirmed unique with guessadx
            new CriAdxKey(0x5e75, 0x4a89, 0x4c61),

            // Boku wa Koukuu Kanseikan - Airport Hero Naha (2006)(Sonic Powered)(Electronic Arts)[PSP]
            // Confirmed unique with guessadx
            new CriAdxKey(0x64ab, 0x5297, 0x632f),

            // Lucky Star - Net Idol Meister (2009)(Kadokawa Shoten)[PSP]
            // Confirmed unique with guessadx
            new CriAdxKey(0x4d82, 0x5243, 0x685),

            // Ishin Renka: Ryouma Gaiden (2010-11-25)(-)(D3 Publisher)[PSP]
            new CriAdxKey(0x54d1, 0x526d, 0x5e8b),

            // Lucky Star - Ryouou Gakuen Outousai Portable (2010-12-22)(-)(Kadokawa Shoten)[PSP]
            new CriAdxKey(0x4d06, 0x663b, 0x7d09),

            // Marriage Royale - Prism Story (2010-04-28)(-)(ASCII Media Works)[PSP]
            new CriAdxKey(0x40a9, 0x46b1, 0x62ad),

            // Nogizaka Haruka no Himitsu - Doujinshi Hajime Mashita (2010-10-28)(-)(ASCII Media Works)[PSP]
            new CriAdxKey(0x4601, 0x671f, 0x0455),

            // Slotter Mania P - Mach Go Go Go III (2011-01-06)(-)(Dorart)[PSP]
            new CriAdxKey(0x41ef, 0x463d, 0x5507),

            // Nichijou - Uchuujin (2011-07-28)(-)(Kadokawa Shoten)[PSP]
            new CriAdxKey(0x4369, 0x486d, 0x5461),

            // R-15 Portable (2011-10-27)(-)(Kadokawa Shoten)[PSP]
            new CriAdxKey(0x6809, 0x5fd5, 0x5bb1),

            // Suzumiya Haruhi-chan no Mahjong (2011-07-07)(-)(Kadokawa Shoten)[PSP]
            new CriAdxKey(0x5c33, 0x4133, 0x4ce7),

            // Storm Lover Natsu Koi!! (2011-08-04)(Vridge)(D3 Publisher)
            new CriAdxKey(0x4133, 0x5a01, 0x5723)
        };

        public static readonly List<CriAdxKey> Keys9 = new List<CriAdxKey>
        {
            // Phantasy Star Online 2
            // Guessed with degod
            new CriAdxKey(0x07d2, 0x1ec5, 0x0c7f),

            // Dragon Ball Z: Dokkan Battle
            // Verified by VGAudio
            new CriAdxKey(416383518),

            // Kisou Ryouhei Gunhound EX (2013-01-31)(Dracue)[PSP]
            // Verified by VGAudio
            new CriAdxKey(683461999),

            // Raramagi [Android]
            // Verified by VGAudio
            new CriAdxKey(12160794),

            // Sonic runners [Android]
            // Verified by VGAudio
            new CriAdxKey(19910623),

            // Fallen Princess (iOS/Android)
            new CriAdxKey(145552191146490718)
        };
    }
}