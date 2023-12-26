module Forward.MySql.Connection

open Dapper.FSharp
open Dapper.FSharp.MySQL
open MySql.Data.MySqlClient

type ConnectionConfig =
  { User: string
    Password: string
    Host: string
    DbName: string }

let private readFileLinesIntoList (path: string) =
  path |> File.readFileLinesIn |> List.ofArray

let private tryFirstMatch (pattern: string) (lines: string list) =
  lines
  |> List.tryFind (Regex.isMatch pattern)
  |> Option.bind (Regex.testTryGetFirstGroupMatch pattern)

let private makeErrorString (user: string option) (password: string option) (dbName: string option) =
  let mutable messageParts = []

  if None = user then
    messageParts <- "Cannot extract username from MySQL option file(s)." :: messageParts

  if None = password then
    messageParts <- "Cannot extract password from MySQL option file(s)." :: messageParts

  if None = dbName then
    messageParts <- "Cannot find DB_NAME variable." :: messageParts

  String.concat " " messageParts

/// Builds a connection config using a mix of (environment) variables and
/// startup options extracted from option files.
let buildConfig (getVariable: (string) -> string option) (files: string list) =
  try
    let lines: string list = files |> List.map readFileLinesIntoList |> List.concat
    let userOpt: string option = tryFirstMatch @"^user=(\w+)$" lines
    let passwordOpt: string option = tryFirstMatch @"^password=(\w+)$" lines
    let dbNameOpt: string option = getVariable "DB_NAME"

    let host: string =
      match getVariable "DB_HOST" with
      | Some(string) -> string
      | None -> "127.0.0.1"

    match (userOpt, passwordOpt, dbNameOpt) with
    | (Some(user), Some(password), Some(dbName)) ->
      Ok(
        { User = user
          Password = password
          Host = host
          DbName = dbName }
      )
    | _ -> Error(makeErrorString userOpt passwordOpt dbNameOpt)
  with :? System.IO.FileNotFoundException as notFound ->
    Error(sprintf "%s cannot be found." notFound.FileName)
