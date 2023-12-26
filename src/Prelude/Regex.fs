module Regex

let testTryGetFirstGroupMatch (pattern: string) (input: string) =
  let result: System.Text.RegularExpressions.Match =
    System.Text.RegularExpressions.Regex.Match(input, pattern)

  match result.Groups.Count with
  | i when i >= 2 -> Some result.Groups[1].Value
  | _ -> None
