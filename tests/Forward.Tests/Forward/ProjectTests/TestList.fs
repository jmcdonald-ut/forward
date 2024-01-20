module Forward.Tests.Tests.TestList

open NUnit.Framework

open Forward.Project.Utils
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
  member this.testBasicUsageOfListDotEnvs() =
    let commandContext: Forward.CommandContext.FileCommandContext =
      this.tempTestEnv.CommandContext()

    let actual: seq<string> = commandContext |> listDotEnvs |> Seq.map _.Name
    let expected: seq<string> = [ "c"; "b"; "a"; "init" ]

    Assert.That(actual, Is.EqualTo(expected))
