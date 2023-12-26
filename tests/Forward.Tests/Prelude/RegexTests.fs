module Forward.Tests.Prelude.RegexTests

open NUnit.Framework

[<TestFixture>]
type Tests() =
  [<TestCase(@"^user=(\w+)$", "user=fooo", "fooo")>]
  [<TestCase(@"^user=(\w+)$", "user=myuser", "myuser")>]
  [<TestCase(@"^password=(.+)$", "password=Snap crackle pop!", "Snap crackle pop!")>]
  [<TestCase(@"^password='((\w|\s|[.!$*!&@#$%^])+)'$", "password='Snap crackle pop!'", "Snap crackle pop!")>]
  member this.testTryGetFirstGroupMatchWithMatchingInput(pattern: string, input: string, output: string) =
    let expected: string option = Some(output)
    let actual: string option = Regex.testTryGetFirstGroupMatch pattern input

    Assert.That(actual, Is.EqualTo(expected))

  [<Test>]
  member this.testTryGetFirstGroupMatchOfList() =
    let patterns: (string * int) list =
      [ ("^password='((\w|\s|[.!$*!&@#$%^])+)'$", 1)
        ("^password=((\w|[.!$*!&@#$%^])+)$", 1) ]

    let tryGetWithPatterns = Regex.testTryGetFirstGroupMatchOfList patterns

    let expectedFirst: string = "Snap crackle pop!"
    let actualFirst: string option = tryGetWithPatterns "password='Snap crackle pop!'"
    Assert.That(actualFirst, Is.EqualTo(Some(expectedFirst)))

    let expectedSecond: string = "snapAndCrackle!"
    let actualSecond: string option = tryGetWithPatterns "password=snapAndCrackle!"
    Assert.That(actualSecond, Is.EqualTo(Some(expectedSecond)))

  [<TestCase(@"^user=(\w+)$", "password=nope")>]
  [<TestCase(@"^user=(\w+)$", "user=foo baz")>]
  member this.testTryGetFirstGroupMatchWithInvalidInput(pattern: string, input: string) =
    let expected: string option = None
    let actual: string option = Regex.testTryGetFirstGroupMatch pattern input

    Assert.That(actual, Is.EqualTo(expected))
