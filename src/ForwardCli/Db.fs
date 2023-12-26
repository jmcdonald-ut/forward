module ForwardCli.Db

open Argu
open dotenv.net
open Spectre.Console

open Forward
open ForwardCli.OutputResult

// This is used by both `fwd backup` and `fwd restore`.
type DbArgs =
  | [<CustomCommandLine("--db-name")>] DbName of string

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | DbName _ -> "database name to backup; falls back to DB_NAME in current .env file"

type DbCommandFun = CommandContext.FileCommandContext -> string -> Result<MySqlHelpers.BackupContext, string>

// SUBCOMMANDS
//   fwd backup
//   fwd restore
// ****************************************************************************

let private fallbackToSystemEnv (key: string) =
  match Environment.getEnvironmentVariableOpt key with
  | Some(dbName) -> Ok(dbName)
  | None -> Error("Unable to resolve a DB name (no --db-name and no DB_NAME)")

let private extractDbName (cmdCtxt: CommandContext.FileCommandContext) (args: ParseResults<DbArgs>) =
  match args.TryGetResult(DbName) with
  | Some(dbName) -> Ok(dbName)
  | None ->
    match FileHelpers.actualPathToCurrentEnv cmdCtxt with
    | Error(_) -> fallbackToSystemEnv "DB_NAME"
    | Ok(path) ->
      let envVars: System.Collections.Generic.IDictionary<string, string> =
        DotEnv.Read(new DotEnvOptions(envFilePaths = [ path.FullName ]))

      match envVars.ContainsKey "DB_NAME" with
      | false -> fallbackToSystemEnv "DB_NAME"
      | true -> Ok(envVars["DB_NAME"])

let doDbCommand (dbCommand: DbCommandFun) (cmdCtxt: CommandContext.FileCommandContext) (args: ParseResults<DbArgs>) =
  args
  |> extractDbName cmdCtxt
  |> Result.bind (dbCommand cmdCtxt)
  |> OutputResult.recordResultOf

let handleBackupCommand = doDbCommand MySqlHelpers.backupDb

let handleRestoreCommand = doDbCommand MySqlHelpers.restoreDb

let private useEnv (cmdCtxt: CommandContext.FileCommandContext) =
  match FileHelpers.actualPathToCurrentEnv cmdCtxt with
  | Ok(path) -> DotEnv.Load(new DotEnvOptions(envFilePaths = [ path.FullName ]))
  | Error(_) -> ()

let handleCountsCommand (cmdCtxt: CommandContext.FileCommandContext) =
  useEnv cmdCtxt

  let folder (table: Table) (item: Forward.MySql.Counts.CountEntry) =
    let label: string = item.TableName
    let count: int64 = item.Count
    table.AddRow(label, count.ToString())

  let handleListCommandSuccess (rows: Forward.MySql.Counts.CountEntry seq) =
    TableResult(makeTableResult [| "Table"; "Row Count" |] folder (List.ofSeq rows))

  MySql.Connection.optionFiles ()
  |> MySql.Connection.buildConnection Environment.getEnvironmentVariableOpt
  |> MySql.Counts.allTableCountsTask
  |> Async.AwaitTask
  |> Async.RunSynchronously
  |> handleListCommandSuccess
