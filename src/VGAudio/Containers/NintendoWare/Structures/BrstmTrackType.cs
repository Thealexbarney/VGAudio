namespace VGAudio.Containers.NintendoWare.Structures
{
    /// <summary>
    /// The different track description types used in BRSTM files.
    /// </summary>
    public enum BrstmTrackType
    {
        /// <summary>
        /// A shorter track description that does not include volume or panning values.
        /// Used in Super Smash Bros. Brawl. 
        /// </summary>
        Short = 0,
        /// <summary>
        /// A track description containing all the standard values.
        /// Used in most games other than Super Smash Bros. Brawl.
        /// </summary>
        Standard = 1
    }
}