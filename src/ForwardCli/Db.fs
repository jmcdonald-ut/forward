module ForwardCli.Db

open Argu
open dotenv.net

open ForwardCli.OutputResult

// This is used by both `fwd backup` and `fwd restore`.
type DbArgs =
  | [<CustomCommandLine("--db-name")>] DbName of string

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | DbName _ -> "database name to backup; falls back to DB_NAME in current .env file"

// SUBCOMMANDS
//   fwd backup
//   fwd restore
// ****************************************************************************

let private fallbackToSystemEnv (key: string) =
  match Forward.FileHelpers.getEnvironmentVariableOpt key with
  | Some(dbName) -> Ok(dbName)
  | None -> Error("Unable to resolve a DB name (no --db-name and no DB_NAME)")

let private extractDbName (commandContext: Forward.FileHelpers.CommandFileContext) (args: ParseResults<DbArgs>) =
  match args.TryGetResult(DbName) with
  | Some(dbName) -> Ok(dbName)
  | None ->
    match Forward.FileHelpers.actualPathToCurrentEnv commandContext with
    | Error(_) -> fallbackToSystemEnv "DB_NAME"
    | Ok(path) ->
      let envVars: System.Collections.Generic.IDictionary<string, string> =
        DotEnv.Read(new DotEnvOptions(envFilePaths = [ path.FullName ]))

      match envVars.ContainsKey "DB_NAME" with
      | false -> fallbackToSystemEnv "DB_NAME"
      | true -> Ok(envVars["DB_NAME"])

let private asOutputResult (result: Result<'a, string>) =
  match result with
  | Ok(a) -> RecordResult(a)
  | Error(reason) -> ErrorResult(reason)

let handleBackupCommand (commandContext: Forward.Project.CommandContext) (args: ParseResults<DbArgs>) =
  args
  |> extractDbName commandContext
  |> Result.bind (Forward.MySqlHelpers.backupDb commandContext)
  |> asOutputResult

let handleRestoreCommand (commandContext: Forward.Project.CommandContext) (args: ParseResults<DbArgs>) =
  args
  |> extractDbName commandContext
  |> Result.bind (Forward.MySqlHelpers.restoreDb commandContext)
  |> asOutputResult
