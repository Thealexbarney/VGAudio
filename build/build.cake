//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var verbosity = Argument("verbosity", "Minimal");

//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

var baseDiro = Directory(Context.Environment.WorkingDirectory.FullPath) + Directory("..");
ConvertableDirectoryPath baseDir = Directory(((DirectoryPath)(Directory(Context.Environment.WorkingDirectory.FullPath) + Directory(".."))).Collapse().FullPath);
var sourceDir = baseDir + Directory("src");
var publishDir = baseDir + Directory("publish");

var slnFile = sourceDir + File("VGAudio.sln");

var libraryDir = sourceDir + Directory("VGAudio");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Restore")
  .Does(() =>
{
    DotNetCoreRestore(slnFile);
});

Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("BuildNetStandardLib")
  .Does(() =>
{
	//DotNetCoreBuild(slnFile);
    Information(baseDir);
    Information(sourceDir);
    Information(slnFile);
});

Task("BuildNetStandardLib")
  .Does(() =>
{
	var settings = new DotNetCoreBuildSettings
    {
        Framework = "netstandard1.1",
        Configuration = configuration
    };

	DotNetCoreBuild(libraryDir, settings);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
