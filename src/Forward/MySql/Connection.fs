module Forward.MySql.Connection

open MySql.Data.MySqlClient
open Forward.Project

type ConnectionConfig =
  { User: string
    Password: string
    Host: string
    DbName: string }

type ConnectionWithConfig =
  { Conn: MySqlConnection
    Config: ConnectionConfig }

let buildConnection (connectionString: string) = new MySqlConnection(connectionString)

let private passwordPatterns: string list =
  [ @"^password='((\w|\s|[.!$*!&@#$%^])+)'$"
    @"^password=((\w|[.!$*!&@#$%^])+)$" ]

let private readFileLinesIntoList (path: string) =
  path |> File.readFileLinesIn |> List.ofArray

let private tryFirstMatch (pattern: string) (lines: string list) =
  lines
  |> List.tryFind (Regex.isMatch pattern)
  |> Option.bind (Regex.testTryGetFirstGroupMatch pattern)

let private tryFirstMatchOfList (patterns: string list) (lines: string list) =
  let matchingPattern =
    patterns
    |> List.tryFind (fun pattern ->
      match List.tryFind (Regex.isMatch pattern) lines with
      | Some(_) -> true
      | None -> false)

  matchingPattern
  |> Option.bind (fun (pattern: string) -> tryFirstMatch pattern lines)

let private makeErrorString (user: string option) (password: string option) (dbName: string option) =
  let mutable messageParts: string list = []

  if None = user then
    messageParts <- "Cannot extract username from MySQL option file(s)." :: messageParts

  if None = password then
    messageParts <- "Cannot extract password from MySQL option file(s)." :: messageParts

  if None = dbName then
    messageParts <- "Cannot find DB_NAME variable." :: messageParts

  String.concat " " messageParts

let buildConnectionString (user: string) (password: string) (host: string) (dbName: string) =
  let builder: MySqlConnectionStringBuilder = new MySqlConnectionStringBuilder()
  builder.UserID <- user
  builder.Password <- password
  builder.Database <- dbName
  builder.CacheServerProperties <- true
  builder.Server <- host
  builder.ToString()

/// Builds a connection config using a mix of (environment) variables and
/// startup options extracted from option files.
let buildConfig (getVariable: (string) -> string option) (files: string list) =
  try
    let lines: string list = files |> List.map readFileLinesIntoList |> List.concat
    let userOpt: string option = tryFirstMatch @"^user=(\w+)$" lines
    let passwordOpt: string option = tryFirstMatchOfList passwordPatterns lines
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

let optionFiles: string list =
  let home: string = Environment.getEnvironmentVariable "HOME"

  [ "/etc/my.cnf"
    "/etc/mysql/my.cnf"
    File.joinPaths home ".my.cnf"
    File.joinPaths home ".mylogin.cnf" ]
  |> List.filter File.exists

let getUserPasswordHost () =
  // Load values that we don't derive from per DotEnv file. Do this prior to
  // splitting up work.
  let lines: string list =
    optionFiles |> List.map readFileLinesIntoList |> List.concat

  let userOpt: string option = tryFirstMatch @"^user=(\w+)$" lines
  let passwordOpt: string option = tryFirstMatchOfList passwordPatterns lines

  let host: string =
    match Environment.getEnvironmentVariableOpt "DB_HOST" with
    | Some(string) -> string
    | None -> "127.0.0.1"

  match (userOpt, passwordOpt) with
  | (Some(user), Some(password)) -> Ok(user, password, host)
  | (None, None) -> Error("Unable to extract MySQL username or password")
  | (None, _) -> Error("Unable to extract MySQL username")
  | (_, None) -> Error("Unable to extract MySQL password")

let prepareSingleConnectionStringAsync (user: string) (password: string) (host: string) (dotEnvFile: Utils.ListEntry) =
  async {
    let! (dict: System.Collections.Generic.IDictionary<string, string>) = DotEnv.readDotEnvAsync dotEnvFile.FullName
    let connString: string = buildConnectionString user password host dict["DB_NAME"]
    let envName: string = Regex.replace @"/^\.env\./" "" dotEnvFile.Name
    return (envName, connString)
  }

let prepareManyConnectionStringsAsync (dotEnvFiles: seq<Utils.ListEntry>) =
  async {
    // Load values that we don't derive from per DotEnv file. Do this prior to
    // splitting up work.
    let (user: string, password: string, host: string) =
      match getUserPasswordHost () with
      | Ok(user, password, host) -> (user, password, host)
      | Error(reason) -> failwith reason

    let prepareConnString = prepareSingleConnectionStringAsync user password host

    return! dotEnvFiles |> Seq.map prepareConnString |> Async.Parallel
  }
