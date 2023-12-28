module Regex

open System.Text

let regexMatch (pattern: string) (input: string) =
  RegularExpressions.Regex.Match(input, pattern)

let isMatch (pattern: string) (input: string) = (regexMatch pattern input).Success

let replace (pattern: string) (replacement: string) (input: string) =
  RegularExpressions.Regex.Replace(input, pattern, replacement)

[<TailCall>]
let rec anyMatch (patterns: string list) (input: string) =
  match patterns with
  | [] -> false
  | pattern :: remaining -> (isMatch pattern input) || (anyMatch remaining input)

let testTryGetNGroupMatch (pattern: string) (input: string) (n: int) =
  let result: RegularExpressions.Match =
    RegularExpressions.Regex.Match(input, pattern)

  match result.Groups.Count with
  | i when i >= n + 1 -> Some result.Groups[n].Value
  | _ -> None

let testTryGetFirstGroupMatchOfList (patterns: (string * int) list) (input: string) =
  match List.tryFind (fun (pattern, _) -> isMatch pattern input) patterns with
  | Some(pattern, n) -> testTryGetNGroupMatch pattern input n
  | None -> None

let testTryGetFirstGroupMatch (pattern: string) (input: string) = testTryGetNGroupMatch pattern input 1
