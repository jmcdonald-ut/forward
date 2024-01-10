module Forward.Tests.Forward.ProjectTests

open NUnit.Framework
open System.IO

open Forward.Project
open Forward.Tests.LibTest.TempEnvSetUp

[<TestFixture>]
type Tests() =
  [<DefaultValue>]
  val mutable tempTestEnv: TempTestEnv

  [<OneTimeSetUp>]
  member this.setUpTestDirs() =
    this.tempTestEnv <- new TempTestEnv()
    this.tempTestEnv.CreateDotEnv(".env.a", "FILE=A")
    this.tempTestEnv.CreateDotEnv(".env.b", "FILE=B")
    this.tempTestEnv.CreateDotEnv(".env.c", "FILE=C")

  [<OneTimeTearDown>]
  member this.tearDownTestDirs() = this.tempTestEnv.TearDown()

  [<Test>]
  member this.testInitIsolated() =
    let context: Forward.CommandContext.FileCommandContext =
      this.tempTestEnv.CommandContext()

    let expectedPath: string =
      File.combinePaths this.tempTestEnv.ProjectRoot.FullName ".env"

    let expected: string = sprintf "`%s` symlink created" expectedPath
    let actual: string = context |> init |> Result.defaultValue "FAIL"

    Assert.That(actual, Is.EqualTo(expected))

  [<Test>]
  member this.testBasicUsageOfListDotEnvs() =
    let commandContext: Forward.CommandContext.FileCommandContext =
      this.tempTestEnv.CommandContext()

    let actual: string list = commandContext |> listDotEnvs |> List.map _.Name
    let expected: string list = [ ".env.c"; ".env.b"; ".env.a" ]

    Assert.That(actual, Is.EqualTo(expected))
