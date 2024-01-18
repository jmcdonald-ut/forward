module Forward.Tests.Tests.TestExplainingCommandContext

open NUnit.Framework

open Forward.CommandContext
open Forward.Project.Core

open Forward.Tests.LibTest.AssertionExtensions
open Forward.Tests.LibTest.TempEnvSetUp

[<TestFixture>]
type Tests() =
  [<DefaultValue>]
  val mutable tempTestEnv: TempTestEnv

  [<OneTimeSetUp>]
  member this.setUpTestDirs() = this.tempTestEnv <- new TempTestEnv()

  [<OneTimeTearDown>]
  member this.tearDownTestDirs() = this.tempTestEnv.TearDown()

  [<Test>]
  member this.testExplainingCommandContext() =
    let commandContext: FileCommandContext = this.tempTestEnv.CommandContext()

    let expected: DotEnvCommandContext =
      { RootPath = commandContext.RootPath
        ProjectName = commandContext.ProjectName
        ProjectArtifactsPath = commandContext.ProjectArtifactsPath
        DotEnvSymLinkPath = Some(this.tempTestEnv.GetPathToDotEnvCurrent())
        DotEnvPath = Some(this.tempTestEnv.GetPathToDotEnv("init")) }

    let actual: Result<DotEnvCommandContext, string> = explain commandContext

    Assert.Result(actual).IsOkWith(expected)
