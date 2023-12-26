module Forward.Tests.LibTest.AssertionExtensions

open NUnit.Framework
open System.Runtime.CompilerServices

let private failAssertion (overview: string) =
  overview |> sprintf "Assertion failed: %s" |> Assert.Fail

let private failAssertionGeneral (overview: string) (actual: 'actual) =
  actual
  |> sprintf "Assertion failed: %s\n    Got:\n      %A" overview
  |> Assert.Fail

let private failAssertionUnexpected1 (overview: string) (expected: 'expected) =
  expected
  |> sprintf "Assertion failed: %s\n    Expected:\n      %A" overview
  |> Assert.Fail

let private failAssertionUnexpected (overview: string) (expected: 'expected) (actual: 'actual) =
  actual
  |> sprintf "Assertion failed: %s\n    Expected:\n      %A\n    Got:\n      %A" overview expected
  |> Assert.Fail

type AssertResult<'ok, 'error when 'ok: equality and 'error: equality>(actual: Result<'ok, 'error>) =
  member this.IsOk =
    match actual with
    | Ok(_) -> ()
    | Error(_) -> failAssertionGeneral "Result is not Ok." actual

  member this.IsError =
    match actual with
    | Ok(_) -> failAssertionGeneral "Result is Ok, but an Error was expected." actual
    | Error(_) -> ()

  member this.IsErrorWith(expectedError: 'error) =
    match actual with
    | Error(actualError) when actualError = expectedError -> ()
    | Error(_) -> failAssertionUnexpected "Content of Error does not match." (Error expectedError) actual
    | Ok(_) -> failAssertionUnexpected "Result is Ok (expected Error)." (Error expectedError) actual

  member this.IsOkWith(content: 'ok) =
    match actual with
    | Ok(actualContent) when actualContent = content -> ()
    | Ok(unexpected) -> failAssertionUnexpected "Content of Ok does not match." content unexpected
    | Error(errorValue) -> failAssertionUnexpected "Result is not Ok." (Ok content) (Error errorValue)

type AssertOption<'some when 'some: equality>(actual: Option<'some>) =
  member this.IsSome =
    match actual with
    | Some(_) -> ()
    | None -> failAssertion "Option is None."

  member this.IsNone =
    match actual with
    | None -> ()
    | Some(_) -> failAssertionGeneral "Expected None." actual

  member this.IsSomeOf(content: 'some) =
    match actual with
    | Some(actualContent) when actualContent = content -> ()
    | Some(unexpected) -> failAssertionUnexpected "Content of Some does not match." content unexpected
    | None -> failAssertionUnexpected1 "Unexpected None." (Some content)

[<Extension>]
type Assert =
  [<Extension>]
  static member inline Result(actual: Result<'ok, 'error>) = AssertResult(actual)

  [<Extension>]
  static member inline Option(actual: Option<'some>) = AssertOption(actual)
