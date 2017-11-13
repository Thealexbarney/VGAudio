# ADX Encryption Keys

The following is a list of known ADX encryption keys, along with the software that uses them.

ADX uses a linear congruential generator (LCG) to generate pseudo-random numbers for encryption.
Each sequential output of the LCG is XORed with the scale of each frame.
An ADX encryption key consists of three parameters used to run the LCG. These are called the `seed`, `multiplier` and `increment`.

The code to derive the LCG parameters from a `keystring` or `keycode` can be found [in VGAudio](/src/VGAudio/Codecs/CriAdx/CriAdxKey.cs).

There are two types of ADX encryption: "Type 8" and "Type 9". The encryption keys are listed by type in the tables below.

### Type '8' Encryption
Type 8 encryption keys are derived from a `keystring`. This string is run through a one-way hash function to generate the three LCG parameters.

Because the keystring for type 8 keys is run through a one-way hash function, the keystring cannot be recovered from the LCG parameters alone.

In order to recover a type 8 keystring, LCG parameters are used along with the executable file from the game containing the encrypted ADX files.
The keystring is almost always stored in the executable as plain text. Once the LCG parameters are found.
Every string found in the executable is hashed, and the one that matches the LCG parameters is our keystring.

Because of this, only a handful of keys in the table below have a keystring listed.
Finding the missing keystrings would require obtaining the game's main executable and checking all the strings in it.

### Type '9' Encryption

Type 9 encryption keys are stored as an unsigned 64-bit value called a `keycode`. This value is run through a reversable function to generate the three LCG parameters.

The LCG parameters are derived via a reversable function, so the keycode for type 9 keys can recovered from the LCG parameters.

### Cracking ADX Keys

`VGAudioTools` contains a tool for cracking both type 8 and type 9 encrypted ADX files.
If given the game's executable, it will also try to recover the type 8 keystring.

- Cracking the key of a folder of ADX files:  
`VGAudioTools crackadx <FOLDER_OF_ADX_FILES>`

- Cracking the key of a folder of ADX files and recovering the keycode:  
`VGAudioTools crackadx <FOLDER_OF_ADX_FILES> <executable>`


## Type '8' ADX Keys
|Keystring           |Key (Seed, Multiplier, Increment)|Source|
|--------------------|:-------------------------------:|:-----|
|(C)2005 MOSS LTD. BMW Z4|`0x66F5, 0x58BD, 0x4459`|Raiden III (PS2)|
|3x5k62bg9ptbwy|`0x5DEB, 0x5F27, 0x673F`|Phantasy Star Universe (PC)|
|CS-GGNX+|`0x4F7B, 0x4FDB, 0x5CBF`|Mobile Suit Gundam: Gundam vs. Gundam Next (PSP)|
|GHM|`0x50FB, 0x5803, 0x5701`|Killer7 (GC / PS2)|
|GHMSC|`0x4F3F, 0x472F, 0x562F`|Samurai Champloo: Sidetracked (PS2)|
|karaage|`0x49E1, 0x4A57, 0x553D`|God Hand (PS2)<br/>Okami?|
|mituba|`0x5A17, 0x509F, 0x5BFD`|Amagami (PS2)|
|morio|`0x55B7, 0x6191, 0x5A77`|Sonic and the Black Knight (Wii)|
|ranatus|`0x46D3, 0x5CED, 0x474D`|WarTech: Senko no Ronde (Xbox 360)|
|sakakit4649|`0x440B, 0x6539, 0x5723`|NiGHTS: Journey of Dreams (Wii)|
||`0x40A9, 0x46B1, 0x62AD`|Marriage Royale: Prism Story (PSP)|
||`0x4133, 0x5A01, 0x5723`|Storm Lover Natsu Koi!! (PSP)|
||`0x413B, 0x543B, 0x57D1`|Futakoi Alternative: Koi to Shoujo to Machinegun (PS2)|
||`0x41EF, 0x463D, 0x5507`|Slotter Mania P: Mach Go Go Go III (PSP)|
||`0x4369, 0x486D, 0x5461`|Nichijou: Uchuujin (PSP)|
||`0x440D, 0x4327, 0x4FFF`|Gakuen Utopia Manabi Straight! Kira Kira Happy Festa! (PS2)|
||`0x45AF, 0x5F27, 0x52B1`|Nogizaka Haruka no Himitsu: Cosplay Hajimemashita (PS2)|
||`0x4601, 0x671F, 0x0455`|Nogizaka Haruka no Himitsu: Doujinshi Hajime Mashita (PSP)|
||`0x47E1, 0x60E9, 0x51C1`|Tears to Tiara Anecdotes: The Secret of Avalon (PS3)|
||`0x481D, 0x4F25, 0x5243`|Neon Genesis Evangelion: Girlfriend of Steel 2nd (PS2)|
||`0x4969, 0x5DEB, 0x467F`|Shuffle! On the Stage (PS2)|
||`0x4C01, 0x549D, 0x676F`|Yamasa Digi Portable: Matsuri no Tatsujin (PSP)|
||`0x4C73, 0x4D8D, 0x5827`|Uragiri wa Boku no Namae o Shitteiru (PS2)|
||`0x4D06, 0x663B, 0x7D09`|Lucky Star: Ryouou Gakuen Outousai Portable (PSP)|
||`0x4D65, 0x5EB7, 0x5DFD`|Aoishiro (PS2)|
||`0x4D82, 0x5243, 0x0685`|Lucky Star: Net Idol Meister (PSP)|
||`0x4F7B, 0x5071, 0x4C61`|Shoukan Shoujo: Elemental Girl Calling (PS2)|
||`0x53E9, 0x586D, 0x4EAF`|Rakushou! Pachi-Slot Sengen 6: Rio 2 Cruising Vanadis (PS2)|
||`0x54D1, 0x526D, 0x5E8B`|Ishin Renka: Ryouma Gaiden (PSP)|
||`0x5563, 0x5047, 0x43ED`|Hanayoi Romanesque: Ai to Kanashimi (PS2)|
||`0x55B7, 0x67E5, 0x5387`|La Corda d'Oro (PSP)|
||`0x5803, 0x4555, 0x47BF`|Fragments Blue (PS2)|
||`0x586D, 0x5D65, 0x63EB`|Unknown|
||`0x59ED, 0x4679, 0x46C9`|Soulcalibur IV (PS3 / Xbox 360)|
||`0x5A11, 0x67E5, 0x6751`|Storm Lover Kai!! (PSP)|
||`0x5C33, 0x4133, 0x4CE7`|Suzumiya Haruhi-Chan no Mahjong (PSP)|
||`0x5E75, 0x4A89, 0x4C61`|Sora no Otoshimono: DokiDoki Summer Vacation (PSP)|
||`0x5F5D, 0x552B, 0x5507`|Blood+: One Night Kiss (PS2)|
||`0x5F5D, 0x58BD, 0x55ED`|Soshite Kono Uchuu ni Kirameku Kimi no Shi XXX (PS2)|
||`0x5F65, 0x5B3D, 0x5F65`|Little Anchor (PS2)|
||`0x5FC5, 0x63D9, 0x599F`|Shakugan no Shana (PS2)|
||`0x6157, 0x6809, 0x4045`|Senko no Ronde: Dis-United Order (Xbox 360)|
||`0x62AD, 0x4B13, 0x5957`|Sakura Wars 3: Is Paris Burning? (Dreamcast / PC / PS2)|
||`0x6305, 0x509F, 0x4C01`|Sotsugyou 2nd Generation (PS2)|
||`0x645D, 0x6011, 0x5C29`|Sakura Taisen: Atsuki Chishio Ni (PS2)|
||`0x64AB, 0x5297, 0x632F`|Boku wa Koukuu Kanseikan: Airport Hero Naha (PSP)|
||`0x6731, 0x645D, 0x566B`|Nanatsuiro Drops Pure!! (PS2)|
||`0x6809, 0x5FD5, 0x5BB1`|R-15 Portable (PSP)|

## Type '9' ADX Keys
|Keycode           |Key (Seed, Multiplier, Increment)|Source|
|------------------|:-------------------------------:|:-----|
|12160794|`0x0000, 0x0B99, 0x1E33`|Raramagi (iOS/Android)|
|19910623|`0x0000, 0x12FD, 0x1FBD`|Sonic Runners (iOS/Android)|
|416383518|`0x0003, 0x0D19, 0x043B`|Dragon Ball Z: Dokkan Battle (iOS/Android)|
|683461999|`0x0005, 0x0BCD, 0x1ADD`|Kisou Ryouhei Gunhound EX (PSP)|
|268736153152|`0x07D2, 0x1EC5, 0x0C7F`|Phantasy Star Online 2|
|145552191146490718|`0x5E4B, 0x190D, 0x76BB`|Fallen Princess (iOS/Android)|