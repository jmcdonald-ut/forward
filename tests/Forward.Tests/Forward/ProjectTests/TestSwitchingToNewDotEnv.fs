module Forward.Tests.Forward.ProjectTests.TestSwitchingToNewDotEnv

open NUnit.Framework

open Forward.Project
open Forward.Tests.LibTest.AssertionExtensions
open Forward.Tests.LibTest.TempEnvSetUp

[<TestFixture>]
type Tests() =
  [<DefaultValue>]
  val mutable tempTestEnv: TempTestEnv

  [<OneTimeSetUp>]
  member this.setUpTestDirs() =
    this.tempTestEnv <- new TempTestEnv()
    this.tempTestEnv.CreateDotEnv(".env.existing", "FILE=A")

  [<OneTimeTearDown>]
  member this.tearDownTestDirs() = this.tempTestEnv.TearDown()

  [<Test>]
  member this.testSwitchingToNewDotEnvWithValidInput() =
    let commandContext: Forward.CommandContext.FileCommandContext =
      this.tempTestEnv.CommandContext()

    let name: string = "new"
    let expectedPath: string = this.tempTestEnv.GetPathToDotEnv name

    let expectedMessage: string =
      sprintf
        "Switched; created DotEnv; backing symlink now `%s` → `%s`"
        (this.tempTestEnv.GetPathToDotEnvCurrent())
        expectedPath

    let actual: Result<string, string> = switch commandContext name Create

    Assert.Result(actual).IsOkWith(expectedMessage)

  [<Test>]
  member this.testSwitchingUsingExistingName() =
    let commandContext: Forward.CommandContext.FileCommandContext =
      this.tempTestEnv.CommandContext()

    let name: string = "existing"
    let expectedMessage: string = "Cannot create `existing`; it already exists"
    let actual: Result<string, string> = switch commandContext name Create

    Assert.Result(actual).IsErrorWith(expectedMessage)
