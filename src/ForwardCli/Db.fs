module ForwardCli.Db

open Argu
open dotenv.net
open Spectre.Console

open Forward
open Forward.Helpers
open ForwardCli.OutputResult

// Internal "counts" type. This is useful for coercing the output of different
// count command branches so the F# compiler is appeased.
type Counts = { Label: string; Counts: string list }

// This is used by both `fwd backup` and `fwd restore`.
type DbArgs =
  | [<CustomCommandLine("--db-name")>] DbName of string

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | DbName _ -> "database name to backup; falls back to DB_NAME in current .env file"

type DbTableCountsArgs =
  | [<MainCommand; Last>] Tables of string list

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Tables _ -> "list of tables to compare per env; omit to print all from current env"

type DbCommandFun = CommandContext.FileCommandContext -> string -> Result<MySqlHelpers.BackupContext, string>

// SUBCOMMANDS
//   fwd backup
//   fwd restore
//   fwd counts [<table>...]
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

let handleRestoreCommand = doDbCommand MySqlHelpers.backupDb

let private runCountsCommand (columns: string array) (bindResult: ('t) -> Counts list) (asyncCommand: Async<'t>) =
  let folder (table: Table) (item: Counts) =
    table.AddRow((item.Label :: item.Counts) |> Array.ofList)

  asyncCommand
  |> Async.RunSynchronously
  |> bindResult
  |> makeTableResult columns folder
  |> TableResult

let private doHandleTableBreakdown (commandContext: CommandContext.FileCommandContext) (tableNames: string list) =
  let columns: string array = "DotEnv" :: tableNames |> Array.ofList

  let bind (rows: Forward.MySql.Counts.DotEnvWithTableCounts array) =
    rows
    |> List.ofArray
    |> List.map (fun e ->
      { Label = e.DotEnvName
        Counts =
          e.TableCounts
          |> Seq.sortBy (fun entry -> List.findIndex (fun tName -> tName = entry.TableName) tableNames)
          |> Seq.map (fun entry -> sprintf "%i" entry.Count)
          |> List.ofSeq })

  tableNames
  |> MySql.Counts.collectTableCountsPerDotEnvAsync commandContext
  |> runCountsCommand columns bind

let private doHandleAllTableBreakdown (commandContext: CommandContext.FileCommandContext) =
  let columns: string array = [| "Table"; "Row Count" |]

  let bind (rows: Forward.MySql.Counts.CountEntry seq) =
    rows
    |> List.ofSeq
    |> List.map (fun e ->
      { Label = e.TableName
        Counts = [ (sprintf "%i" e.Count) ] })

  commandContext
  |> MySql.Counts.revisedAllTableCountsTask
  |> runCountsCommand columns bind

let handleOtherCountsCommand
  (commandContext: CommandContext.FileCommandContext)
  (args: ParseResults<DbTableCountsArgs>)
  : CommandResult<Counts> =
  match args.TryGetResult(Tables) with
  | None -> doHandleAllTableBreakdown commandContext
  | Some(tableNames) -> doHandleTableBreakdown commandContext tableNames
