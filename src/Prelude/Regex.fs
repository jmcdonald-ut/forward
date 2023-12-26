module Regex

let regexMatch (pattern: string) (input: string) =
  System.Text.RegularExpressions.Regex.Match(input, pattern)

let isMatch (pattern: string) (input: string) =
  let result = regexMatch pattern input
  result.Success

let testTryGetFirstGroupMatch (pattern: string) (input: string) =
  let result: System.Text.RegularExpressions.Match =
    System.Text.RegularExpressions.Regex.Match(input, pattern)

  match result.Groups.Count with
  | i when i >= 2 -> Some result.Groups[1].Value
  | _ -> None
