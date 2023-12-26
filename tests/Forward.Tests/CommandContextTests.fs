module Forward.Tests.CommandContextTests

open System.IO
open NUnit.Framework

open Forward.CommandContext

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
  member this.testContextWithoutProjectBuildReturnsRecord() =
    let expected = { RootPath = "/home/.forward" }
    let actual = ContextWithoutProject.Build("/home/.forward")

    Assert.That(actual, Is.EqualTo(expected))

  [<Test>]
  member this.testContextWithPotentialProjectBuildWithProject() =
    let actual =
      ContextWithPotentialProject.Build(forwardRoot.FullName, projectRoot.Name)

    let expected: ContextWithPotentialProject =
      { RootPath = forwardRoot.FullName
        ProjectName = projectRoot.Name
        ProjectArtifactsPath = File.joinPaths forwardRoot.FullName projectRoot.Name
        ProjectPath = None
        DotEnvSymLinkPath = None
        DotEnvPath = None }

    Assert.That(actual, Is.EqualTo(expected))
