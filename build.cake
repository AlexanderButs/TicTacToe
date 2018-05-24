#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
//#addin nuget:?package=Cake.NSwag.Console

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var projName = "TicTacToe";
var slnDir = Directory(".");
var srcDir = slnDir + Directory("src");
var projDir = srcDir + Directory(projName);
var binDir = srcDir + 
    Directory(projName) + 
    Directory("bin") + 
    Directory(configuration);
var publishDir = binDir + Directory("publish");
var testDir = slnDir + Directory("test");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    DotNetCoreClean(slnDir);
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => 
{
    DotNetCoreRestore(slnDir);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetCoreBuild(
            slnDir,
            new DotNetCoreBuildSettings()
            {
                Configuration = configuration,
                ArgumentCustomization = args => args.Append($"--no-restore"),
            });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
        var projects = GetFiles("./test/**/*Test.csproj");
        foreach(var project in projects)
        {
            Information("Testing project " + project);
            DotNetCoreTest(
                project.ToString(),
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoBuild = true,
                    ArgumentCustomization = args => args.Append($"--no-restore --verbosity=normal"),
                });
        }
});

Task("Publish")
    .IsDependentOn("Test")
    .Does(() =>
{
    DotNetCorePublish(
        projDir,
        new DotNetCorePublishSettings()
        {
            Configuration = configuration,
            OutputDirectory = publishDir
        });
});

Task("__Swagger")
    .IsDependentOn("Publish")
    .Does(() =>
    {
        // NSwag.FromWebApiAssembly(publishDir + File("TicTacToe.dll"))
        //     .ToSwaggerSpecification("./swagger.json");
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Publish");
    //.IsDependentOn("Swagger");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);