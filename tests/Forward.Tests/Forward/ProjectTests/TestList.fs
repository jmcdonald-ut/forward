module Forward.Tests.Forward.ProjectTests.TestList

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
  member this.testBasicUsageOfListDotEnvs() =
    let commandContext: Forward.CommandContext.FileCommandContext =
      this.tempTestEnv.CommandContext()

    let actual: string list = commandContext |> listDotEnvs |> List.map _.Name
    let expected: string list = [ ".env.c"; ".env.b"; ".env.a"; ".env.init" ]

    Assert.That(actual, Is.EqualTo(expected))
