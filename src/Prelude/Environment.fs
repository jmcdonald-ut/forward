module Environment

let currentDirectory () = System.Environment.CurrentDirectory

let getEnvironmentVariable (key: string) =
  System.Environment.GetEnvironmentVariable(key)

let setEnvironmentVariable (key: string) (value: string) =
  System.Environment.SetEnvironmentVariable(key, value)

let getEnvironmentVariableOpt (key: string) =
  match getEnvironmentVariable key with
  | null
  | "" -> None
  | stringOrNull -> Some stringOrNull
