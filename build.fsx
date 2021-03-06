/// FAKE Build script

#r "packages/build/FAKE/tools/FakeLib.dll"
open Fake
open Fake.AssemblyInfoFile
open Fake.Git
open Fake.NuGetHelper
open Fake.RestorePackageHelper
open Fake.ReleaseNotesHelper

// Version info
let projectName = "Meerkat.Logging"
let projectSummary = ""
let projectDescription = "Simple logging framework for use internally withing the Meerkat libaries"
let authors = ["Paul Hatcher"]

let release = LoadReleaseNotes "RELEASE_NOTES.md"

// Properties
let buildDir = "./build"
let toolsDir = getBuildParamOrDefault "tools" "./tools"
let nugetDir = "./nuget"
let solutionFile = "Meerkat.Logging.sln"

let nunitPath = toolsDir @@ "NUnit-2.6.3/bin"

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir;]
)

Target "PackageRestore" (fun _ ->
    RestorePackages()
)

Target "SetVersion" (fun _ ->
    let commitHash = Information.getCurrentHash()
    let infoVersion = String.concat " " [release.AssemblyVersion; commitHash]
    CreateCSharpAssemblyInfo "./code/SolutionInfo.cs"
        [Attribute.Version release.AssemblyVersion
         Attribute.FileVersion release.AssemblyVersion
         Attribute.InformationalVersion infoVersion]
)

Target "Build" (fun _ ->
    !! solutionFile
    |> MSBuild buildDir "Build"
       [
            "Configuration", "Release"
            "Platform", "Any CPU"
            "DefineConstants", "LIBLOG_PUBLIC"
       ]
    |> Log "AppBuild-Output: "
)

Target "Test" (fun _ ->
    // Exclude the packge integrated version as it will find the wrong version in the build directory
    !! (buildDir + "/Meerkat.Logging.Test.dll")
    |> NUnit (fun p ->
       {p with
          ToolPath = nunitPath
          DisableShadowCopy = true
          OutputFile = buildDir @@ "TestResults.xml"})
)

Target "Pack" (fun _ ->
    let nugetParams p = 
      { p with 
          Authors = authors
          Version = release.AssemblyVersion
          ReleaseNotes = release.Notes |> toLines
          OutputPath = buildDir 
          AccessKey = getBuildParamOrDefault "nugetkey" ""
          Publish = hasBuildParam "nugetkey" }

    NuGet nugetParams "nuget/Meerkat.Logging.nuspec"
)

Target "Release" (fun _ ->
    let tag = String.concat "" ["v"; release.AssemblyVersion] 
    Branches.tag "" tag
    Branches.pushTag "" "origin" tag
)

Target "Default" DoNothing

// Dependencies
"Clean"
    ==> "SetVersion"
    ==> "PackageRestore"
    ==> "Build"
    ==> "Test"
    ==> "Default"
    ==> "Pack"
    ==> "Release"

RunTargetOrDefault "Default"