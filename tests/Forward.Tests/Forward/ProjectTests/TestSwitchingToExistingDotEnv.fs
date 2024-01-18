module Forward.Tests.Tests.TestSwitchingToExistingDotEnv

open NUnit.Framework

open Forward.Project.Core
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
  member this.testSwitchingToDotEnvWithValidInput() =
    let commandContext: Forward.CommandContext.FileCommandContext =
      this.tempTestEnv.CommandContext()

    let name: string = "existing"
    let expectedPath: string = this.tempTestEnv.GetPathToDotEnv name

    let expectedMessage: string =
      sprintf "Switched; backing symlink now `%s` → `%s`" (this.tempTestEnv.GetPathToDotEnvCurrent()) expectedPath

    let actual: Result<string, string> = switch commandContext name ReadOnly

    Assert.Result(actual).IsOkWith(expectedMessage)

  [<Test>]
  member this.testSwitchingUsingNonExistentName() =
    let commandContext: Forward.CommandContext.FileCommandContext =
      this.tempTestEnv.CommandContext()

    let name: string = "nonexistent"
    let expectedMessage: string = "Cannot switch to `nonexistent`; it does not exist"
    let actual: Result<string, string> = switch commandContext name ReadOnly

    Assert.Result(actual).IsErrorWith(expectedMessage)
