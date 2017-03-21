namespace Build
{
    public class LibraryBuildStatus
    {
        public LibraryBuildStatus(string libFramework, string cliFramework)
        {
            LibFramework = libFramework;
            CliFramework = cliFramework;
        }

        public string LibFramework { get; }
        public string CliFramework { get; }
        public bool? LibSuccess { get; set; } = null;
        public bool? CliSuccess { get; set; } = null;
        public bool? TestSuccess { get; set; } = null;
    }
}
