using VGAudio.Formats;

namespace VGAudio.Containers
{
    public class Configuration
    {
        public IProgressReport Progress { get; set; }
        /// <summary>
        /// If <c>true</c>, trims the output file length to the set LoopEnd.
        /// If <c>false</c> or if the <see cref="IAudioFormat"/> does not loop,
        /// the output file is not trimmed.
        /// Default is <c>true</c>.
        /// </summary>
        public bool TrimFile { get; set; } = true;
    }
}
