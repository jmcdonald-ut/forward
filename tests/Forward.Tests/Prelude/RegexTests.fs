module Forward.Tests.Prelude.RegexTests

open Forward.Tests.LibTest.AssertionExtensions
open NUnit.Framework

[<TestFixture>]
type Tests() =
  [<TestCase(@"^user=(\w+)$", "user=fooo", "fooo")>]
  [<TestCase(@"^user=(\w+)$", "user=myuser", "myuser")>]
  [<TestCase(@"^password=(.+)$", "password=Snap crackle pop!", "Snap crackle pop!")>]
  [<TestCase(@"^password='((\w|\s|[.!$*!&@#$%^])+)'$", "password='Snap crackle pop!'", "Snap crackle pop!")>]
  member this.testTryGetFirstGroupMatchWithMatchingInput(pattern: string, input: string, output: string) =
    let actual: string option = Regex.testTryGetFirstGroupMatch pattern input

    Assert.Option(actual).IsSomeOf(output)

  [<Test>]
  member this.testTryGetFirstGroupMatchOfList() =
    let patterns: (string * int) list =
      [ ("^password='((\w|\s|[.!$*!&@#$%^])+)'$", 1)
        ("^password=((\w|[.!$*!&@#$%^])+)$", 1) ]

    let tryGetWithPatterns = Regex.testTryGetFirstGroupMatchOfList patterns

    let actualFirst: string option = tryGetWithPatterns "password='Snap crackle pop!'"
    Assert.Option(actualFirst).IsSomeOf("Snap crackle pop!")

    let actualSecond: string option = tryGetWithPatterns "password=snapAndCrackle!"
    Assert.Option(actualSecond).IsSomeOf("snapAndCrackle!")

  [<TestCase(@"^user=(\w+)$", "password=nope")>]
  [<TestCase(@"^user=(\w+)$", "user=foo baz")>]
  member this.testTryGetFirstGroupMatchWithInvalidInput(pattern: string, input: string) =
    let actual: string option = Regex.testTryGetFirstGroupMatch pattern input

    Assert.Option(actual).IsNone
