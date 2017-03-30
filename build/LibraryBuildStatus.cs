namespace Build
{
    public class LibraryBuildStatus
    {
        public LibraryBuildStatus(string libFramework, string cliFramework, string testFramework)
        {
            LibFramework = libFramework;
            CliFramework = cliFramework;
            TestFramework = testFramework;
        }

        public string LibFramework { get; }
        public string CliFramework { get; }
        public string TestFramework { get; }
        public bool? LibSuccess { get; set; }
        public bool? CliSuccess { get; set; }
        public bool? TestSuccess { get; set; }
    }
}
