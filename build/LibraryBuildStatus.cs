namespace Build
{
    public class LibraryBuildStatus
    {
        public LibraryBuildStatus(string libFramework, string cliFramework, string toolsFramework, string testFramework)
        {
            LibFramework = libFramework;
            CliFramework = cliFramework;
            ToolsFramework = toolsFramework;
            TestFramework = testFramework;
        }

        public string LibFramework { get; }
        public string CliFramework { get; }
        public string ToolsFramework { get; }
        public string TestFramework { get; }
    }
}
