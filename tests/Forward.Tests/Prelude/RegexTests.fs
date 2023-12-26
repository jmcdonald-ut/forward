module Forward.Tests.Prelude.RegexTests

open NUnit.Framework

[<TestFixture>]
type Tests() =
  [<TestCase(@"^user=(\w+)$", "user=fooo", "fooo")>]
  [<TestCase(@"^user=(\w+)$", "user=myuser", "myuser")>]
  [<TestCase(@"^password=(.+)$", "password=Snap crackle pop!", "Snap crackle pop!")>]
  member this.testTryGetFirstGroupMatchWithMatchingInput(pattern: string, input: string, output: string) =
    let expected: string option = Some(output)
    let actual: string option = Regex.testTryGetFirstGroupMatch pattern input

    Assert.That(actual, Is.EqualTo(expected))

  [<TestCase(@"^user=(\w+)$", "password=nope")>]
  [<TestCase(@"^user=(\w+)$", "user=foo baz")>]
  member this.testTryGetFirstGroupMatchWithInvalidInput(pattern: string, input: string) =
    let expected: string option = None
    let actual: string option = Regex.testTryGetFirstGroupMatch pattern input

    Assert.That(actual, Is.EqualTo(expected))
