module Regex

let regexMatch (pattern: string) (input: string) =
  System.Text.RegularExpressions.Regex.Match(input, pattern)

let isMatch (pattern: string) (input: string) =
  let result = regexMatch pattern input
  result.Success

let anyMatch (patterns: string list) (input: string) =
  let rec doAnyMatch remaining =
    match remaining with
    | [] -> false
    | pattern :: remaining -> if isMatch pattern input then true else doAnyMatch remaining

  doAnyMatch patterns

let testTryGetNGroupMatch (pattern: string) (input: string) (n: int) =
  let result: System.Text.RegularExpressions.Match =
    System.Text.RegularExpressions.Regex.Match(input, pattern)

  match result.Groups.Count with
  | i when i >= n + 1 -> Some result.Groups[n].Value
  | _ -> None

let testTryGetFirstGroupMatchOfList (patterns: (string * int) list) (input: string) =
  match List.tryFind (fun (pattern, _) -> isMatch pattern input) patterns with
  | Some(pattern, n) -> testTryGetNGroupMatch pattern input n
  | None -> None

let testTryGetFirstGroupMatch (pattern: string) (input: string) = testTryGetNGroupMatch pattern input 1
