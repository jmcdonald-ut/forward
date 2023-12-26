module Forward.Tests.Forward.FileHelpersTests

open NUnit.Framework
open Forward.FileHelpers

[<TestFixture>]
type Tests() =
  let mutable priorRootPath: string = null
  let mutable priorHomePath: string = null

  [<SetUp>]
  member this.setUpWithEnvVarSnapshot() =
    priorRootPath <- Environment.getEnvironmentVariable "FORWARD_ROOT_PATH"
    priorHomePath <- Environment.getEnvironmentVariable "HOME"

  [<TearDown>]
  member this.tearDownByRestoringEnvVarSnapshot() =
    Environment.setEnvironmentVariable "FORWARD_ROOT_PATH" priorRootPath
    Environment.setEnvironmentVariable "HOME" priorHomePath

  [<Test>]
  member this.testGetRootPathOptReturnsArgIfGiven() =
    let inputAndExpected: string option = Some "thing"
    let actual: string option = getRootPathOpt inputAndExpected

    Assert.That(actual, Is.EqualTo(inputAndExpected))

  [<Test>]
  member this.testGetRootPathOptFallsBackToEnvVariable() =
    Environment.setEnvironmentVariable "FORWARD_ROOT_PATH" "/foo/bar"
    let expected: string option = Some "/foo/bar"

    let actual: string option = getRootPathOpt None

    Assert.That(actual, Is.EqualTo(expected))

  [<Test>]
  member this.testGetRootPathOptFallsBackToHome() =
    Environment.setEnvironmentVariable "FORWARD_ROOT_PATH" null
    Environment.setEnvironmentVariable "HOME" "/home"

    let actual: string option = getRootPathOpt None

    Assert.That(actual, Is.EqualTo(Some "/home/.forward"))
