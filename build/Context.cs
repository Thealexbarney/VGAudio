using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build
{
    public class Context : FrostingContext
    {
        public Context(ICakeContext context) : base(context) { }

        public string Configuration { get; set; }

        public DirectoryPath BaseDir { get; set; }
        public DirectoryPath SourceDir { get; set; }
        public DirectoryPath PublishDir { get; set; }
        public DirectoryPath LibraryDir { get; set; }
        public FilePath SlnFile { get; set; }

        public bool RestoredCore { get; set; }
    }
}