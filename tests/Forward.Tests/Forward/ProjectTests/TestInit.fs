module Forward.Tests.Forward.ProjectTests.TestInit

open NUnit.Framework

open Forward.Project
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
  member this.testInitIsolated() =
    let context: Forward.CommandContext.FileCommandContext =
      this.tempTestEnv.CommandContext()

    let expectedPath: string =
      File.combinePaths this.tempTestEnv.ProjectRoot.FullName ".env"

    let expectedMessage: string = sprintf "`%s` symlink created" expectedPath
    let actual: Result<string, string> = context |> init

    Assert.Result(actual).IsOkWith(expectedMessage)
