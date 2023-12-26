module Forward.Tests.Prelude.EnvironmentTests

open Forward.Tests.LibTest.AssertionExtensions
open NUnit.Framework

[<Literal>]
let policyKey = "__FORWARD_ENVIRONMENT_TESTS_POLICY"

[<Literal>]
let linkKey = "__FORWARD_ENVIRONMENT_TESTS_LINK"

[<Literal>]
let nonExistentKey1 = "__FORWARD_ENVIORNMENT_TESTS_NON_EXISTENT_KEY_1"

[<Literal>]
let nonExistentKey2 = "__FORWARD_ENVIORNMENT_TESTS_NON_EXISTENT_KEY_1"

[<TestFixture>]
type Tests() =
  let mutable policyVarBeforeTests = None
  let mutable linkVarBeforeTests = None

  [<OneTimeSetUp>]
  member this.setUpTestEnvironmentVariables() =
    let policyBefore = System.Environment.GetEnvironmentVariable policyKey
    policyVarBeforeTests <- if isNull policyBefore then None else Some policyBefore
    System.Environment.SetEnvironmentVariable(policyKey, "Elevated")

    let linkBefore = System.Environment.GetEnvironmentVariable linkKey
    linkVarBeforeTests <- if isNull linkBefore then None else Some linkBefore
    System.Environment.SetEnvironmentVariable(linkKey, "https://docs.nunit.org/")

  [<OneTimeTearDown>]
  member this.tearDownTestEnvironmentVariables() =
    match policyVarBeforeTests with
    | None -> System.Environment.SetEnvironmentVariable(policyKey, null)
    | Some(value) -> System.Environment.SetEnvironmentVariable(policyKey, value)

    match linkVarBeforeTests with
    | None -> System.Environment.SetEnvironmentVariable(linkKey, null)
    | Some(value) -> System.Environment.SetEnvironmentVariable(linkKey, value)

  [<TestCase(policyKey, "Elevated")>]
  [<TestCase(linkKey, "https://docs.nunit.org/")>]
  member this.testGetEnvironmentVariableOptWithExistingKey(existingKey: string, expectedValue: string) =
    let actual: string option = Environment.getEnvironmentVariableOpt existingKey
    Assert.Option(actual).IsSomeOf(expectedValue)

  [<TestCase(nonExistentKey1)>]
  [<TestCase(nonExistentKey2)>]
  member this.testGetEnvironmentVariableOptWithNonExistentKey(nonExistentKey: string) =
    let actual: string option = Environment.getEnvironmentVariableOpt nonExistentKey
    Assert.Option(actual).IsNone
