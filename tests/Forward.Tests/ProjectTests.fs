module Forward.Tests.ProjectTests

open NUnit.Framework
open System.IO

open Forward.Project

[<TestFixture>]
type Tests() =
  let mutable forwardRoot: DirectoryInfo = null
  let mutable projectsRoot: DirectoryInfo = null
  let mutable projectRoot: DirectoryInfo = null
  let mutable priorRoot: string = null
  let mutable priorProjectName: string = null

  [<OneTimeSetUp>]
  member this.setUpTestDirs() =
    forwardRoot <- Directory.CreateTempSubdirectory("fwd_root")
    projectsRoot <- Directory.CreateTempSubdirectory("fwd_projects")
    projectRoot <- Directory.CreateTempSubdirectory(prefix = "fwd_codebase")

    priorRoot <- Environment.getEnvironmentVariable "FORWARD_ROOT_PATH"
    priorProjectName <- Environment.getEnvironmentVariable "FORWARD_PROJECT_NAME"

    Environment.setEnvironmentVariable "FORWARD_ROOT_PATH" forwardRoot.FullName
    Environment.setEnvironmentVariable "FORWARD_PROJECT_NAME" projectRoot.Name

  [<OneTimeTearDown>]
  member this.tearDownTestDirs() =
    Directory.Delete(forwardRoot.FullName, true)
    Directory.Delete(projectsRoot.FullName, true)
    Directory.Delete(projectRoot.FullName, true)

    Environment.setEnvironmentVariable "FORWARD_ROOT_PATH" priorRoot
    Environment.setEnvironmentVariable "FORWARD_PROJECT_NAME" priorProjectName

  [<Test>]
  member this.testInitIsolated() =
    let context: Forward.CommandContext.FileCommandContext =
      { ProjectName = "testInitIsolated"
        ProjectArtifactsPath = Path.Join([| projectsRoot.FullName; "testInitIsolated" |])
        RootPath = forwardRoot.FullName
        ProjectPath = projectRoot.FullName }

    let expectedPath: string = File.combinePaths projectRoot.FullName ".env"

    let expected: string = sprintf "`%s` symlink created" expectedPath
    let actual: string = context |> init |> Result.defaultValue "FAIL"

    Assert.That(actual, Is.EqualTo(expected))
