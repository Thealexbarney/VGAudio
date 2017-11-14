using System.Collections.Generic;
using System.Linq;

namespace VGAudio.Codecs.CriHca
{
    public static partial class CriHcaEncryption
    {
        /* 
         * Important! 
         * If you add new keys to this list, make sure to update the
         * /docs/hca/encryption-keys.md file in this repository.
         */
        private static readonly ulong[] KeyCodes =
        {
            1224,
            2424,
            8910,
            12345,
            1905818,
            9516284,
            19700307,
            19840202,
            19910623,
            20536401,
            45719322,
            241352432,
            243812156,
            1234253142,
            2012062010,
            2012082716,
            49160768297,
            5047159794308,
            30260840980773,
            59751358413602,
            165521992944278,
            621561580448882,
            765765765765765,
            5027916581011272,
            6929101074247145,
            9101518402445063,
            29423500797988784,
            45152594117267709,
            61891147883431481,
            145552191146490718,
            3003875739822025258,
            4867249871962584729,
            4892292804961027794,
            9001712656335836006,
            9117927877783581796,
            14723751768204501419,
            14925543929015470456,
            15806334760965177344,
            18279639311550860193,
            18446744073709551615
        };

        /// <summary>
        /// A list of known keys used for encrypting HCA files.
        /// </summary>
        /// <remarks>
        /// See the /docs/hca/encryption-keys.md file in this repository for a more detailed list.
        /// </remarks>
        public static List<CriHcaKey> Keys { get; } = KeyCodes.Select(key => new CriHcaKey(key)).ToList();
    }
}
