﻿open Argu
open Spectre.Console
open dotenv.net

let private bindResultAsUnitFunc func result =
  let captureResultAndBuildFunc resultValue =
    let wrappedFunc () = resultValue |> func |> ignore
    Ok wrappedFunc

  result |> Result.bind captureResultAndBuildFunc

// SUBCOMMAND: fwd list
// ****************************************************************************

type SortDir = Forward.Project.ListDirection

type SortCol =
  | [<CustomCommandLine("name")>] Name
  | [<CustomCommandLine("accessed")>] Accessed
  | [<CustomCommandLine("created")>] Created
  | [<CustomCommandLine("updated")>] Updated

type ListArgs =
  | [<CustomCommandLine("--sort-dir")>] SortDirection of SortDir
  | [<CustomCommandLine("--sort-col")>] SortColumn of SortCol

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | SortDirection _ -> "sort direction (asc|desc); defaults to desc"
      | SortColumn _ -> "sort column (name|created|accessed|updated); defaults to updated"

let handleListCommand (commandContext: Forward.Project.CommandContext) (listArgs: ParseResults<ListArgs>) =
  let sortCol: string =
    match listArgs.GetResult(SortColumn, defaultValue = Updated) with
    | Accessed -> "accessed"
    | Created -> "created"
    | Updated -> "updated"
    | _ -> "name"

  let sortDir: string =
    match listArgs.GetResult(SortDirection, defaultValue = SortDir.Desc) with
    | SortDir.Desc -> "desc"
    | SortDir.Asc -> "asc"

  let asFormattedDateTime (dateTime: System.DateTime) =
    dateTime.ToString("dd MMM yyyy @ HH:mm:ss")

  let handleListCommandSuccess (listResult: Forward.Project.ListEntry list) =
    let rowLabel (item: Forward.Project.ListEntry) =
      match item with
      | ({ IsCurrent = true; Name = name }) -> sprintf "[green]%s[/]" name
      | ({ Name = name }) -> sprintf "[blue]%s[/]" name

    let addRow (table: Table) (item: Forward.Project.ListEntry) =
      let indicator: string = if item.IsCurrent then "[green]»[/]" else ""
      let label: string = rowLabel item
      let updatedAt: string = asFormattedDateTime item.LastWriteTime
      table.AddRow(indicator, label, updatedAt)

    let initTable: Table =
      (new Table()).AddColumns([| " "; "Environment"; "Last Updated" |])

    listResult |> (List.fold addRow initTable) |> AnsiConsole.Write

  sortDir
  |> Forward.Project.buildListArgs sortCol
  |> Forward.Project.list commandContext
  |> bindResultAsUnitFunc handleListCommandSuccess

// SUBCOMMAND
//   fwd init
// ****************************************************************************

let handleInitCommand (commandContext: Forward.Project.CommandContext) =
  commandContext
  |> Forward.Project.init
  |> Result.bind (fun (initResult: string) ->
    let handleSuccess _ =
      initResult |> printfn "OK init → %O" |> ignore

    Ok handleSuccess)

// This is used by both `fwd backup` and `fwd restore`.
type DbArgs =
  | [<CustomCommandLine("--db-name")>] DbName of string

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | DbName _ -> "database name to backup; falls back to DB_NAME in current .env file"

let extractDbName (commandContext: Forward.FileHelpers.CommandFileContext) (args: ParseResults<DbArgs>) =
  match args.TryGetResult(DbName) with
  | Some(dbName) -> Ok(dbName)
  | None ->
    match Forward.FileHelpers.actualPathToCurrentEnv commandContext with
    | Error(_) ->
      match Forward.FileHelpers.getEnvironmentVariableOpt "DB_NAME" with
      | Some(dbName) -> Ok(dbName)
      | None -> Error("Unable to resolve a DB name (no --db-name and no DB_NAME)")
    | Ok(path) ->
      let envVars: System.Collections.Generic.IDictionary<string, string> =
        DotEnv.Read(new DotEnvOptions(envFilePaths = [ path.FullName ]))

      match envVars.ContainsKey "DB_NAME" with
      | false ->
        match Forward.FileHelpers.getEnvironmentVariableOpt "DB_NAME" with
        | Some(dbName) -> Ok(dbName)
        | None -> Error("Unable to resolve a DB name (no --db-name and no DB_NAME)")
      | true -> Ok(envVars["DB_NAME"])

// SUBCOMMAND
//   fwd backup
// ****************************************************************************

let handleBackupCommand (commandContext: Forward.Project.CommandContext) (args: ParseResults<DbArgs>) =
  args
  |> extractDbName commandContext
  |> Result.bind (Forward.MySqlHelpers.backupDb commandContext)
  |> bindResultAsUnitFunc (fun (res: Forward.MySqlHelpers.BackupContext) ->
    let handler _ = printfn "OK: %O" res |> ignore
    Ok handler)

// SUBCOMMAND
//   fwd restore
// ****************************************************************************

let handleRestoreCommand (commandContext: Forward.Project.CommandContext) (args: ParseResults<DbArgs>) =
  args
  |> extractDbName commandContext
  |> Result.bind (Forward.MySqlHelpers.restoreDb commandContext)
  |> bindResultAsUnitFunc (fun (res: Forward.MySqlHelpers.BackupContext) ->
    let handler _ = printfn "OK: %O" res |> ignore
    Ok handler)

// SUBCOMMAND
//   fwd explain
// ****************************************************************************

let handleExplainCommand (commandContext: Forward.Project.CommandContext) =
  commandContext
  |> Forward.Project.explain
  |> Result.bind (fun (explainResult: Forward.Project.ExplainOutput) ->
    let handleSuccess _ = explainResult |> printfn "%O" |> ignore

    Ok handleSuccess)

// SUBCOMMAND
//   fwd rm
// ****************************************************************************

type RemoveArgs =
  | [<MainCommand; ExactlyOnce>] Name of string

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Name _ -> "dotenv name"

let handleRemoveCommand (commandContext: Forward.Project.CommandContext) (removeArgs: ParseResults<RemoveArgs>) =
  let name: string = removeArgs.GetResult Name
  let args: Forward.Project.RemoveArgs = { Name = name }
  let handleRemoveCommandSuccess _ = name |> printfn "OK rm → %s" |> ignore

  args
  |> Forward.Project.remove commandContext
  |> bindResultAsUnitFunc handleRemoveCommandSuccess

// SUBCOMMAND
//   fwd switch
// ****************************************************************************

type SwitchArgs =
  | [<MainCommand; ExactlyOnce>] Name of name: string
  | [<CustomCommandLine("-b")>] Create

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Name _ -> "environment name."
      | Create -> "create a new environment using <name>."

let handleSwitchCommand (commandContext: Forward.Project.CommandContext) (switchArgs: ParseResults<SwitchArgs>) =
  let normalizedSwitchArgs: Forward.Project.SwitchArgs =
    { Forward.Project.SwitchArgs.Name = switchArgs.GetResult(Name)
      Forward.Project.SwitchArgs.Mode =
        match switchArgs.Contains(Create) with
        | true -> Forward.Project.SwitchMode.Create
        | _ -> Forward.Project.SwitchMode.ReadOnly }

  let handleSuccess _ = printfn "SUCCESS"

  normalizedSwitchArgs
  |> Forward.Project.switch commandContext
  |> bindResultAsUnitFunc handleSuccess

// COMMAND
//   fwd
// ****************************************************************************

[<RequireSubcommand>]
type RootArgs =
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Init
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Explain
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Backup of ParseResults<DbArgs>
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Restore of ParseResults<DbArgs>
  | [<CliPrefix(CliPrefix.None); AltCommandLine("ls")>] List of ParseResults<ListArgs>
  | [<CliPrefix(CliPrefix.None); CustomCommandLine("rm")>] Remove of ParseResults<RemoveArgs>
  | [<CliPrefix(CliPrefix.None); AltCommandLine("s")>] Switch of ParseResults<SwitchArgs>
  | [<CliPrefix(CliPrefix.DoubleDash)>] Project of string
  | [<CliPrefix(CliPrefix.DoubleDash)>] Root of string

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Init -> "initialize a project."
      | Backup _ -> "backs up a DB."
      | Restore _ -> "restores a DB backup."
      | Explain -> "explains the current context."
      | List _ -> "list project dotenv files."
      | Remove _ -> "remove project dotenv file."
      | Switch _ -> "switch the project's dotenv file."
      // Shared/Root CLI Args
      | Project _ -> "specify the project name."
      | Root _ -> "specify the path to `fwd` artifacts."

let checkStructure =
#if DEBUG
  true
#else
  false
#endif

/// Parses command line arguments into a structure that's easier to map into the
/// business logic.
let parseCommandLine (argv: string[]) =
  let errorHandler: ProcessExiter =
    ProcessExiter(
      colorizer =
        function
        | ErrorCode.HelpText -> None
        | _ -> Some System.ConsoleColor.Red
    )

  ArgumentParser
    .Create<RootArgs>(programName = "fwd", errorHandler = errorHandler, checkStructure = checkStructure)
    .ParseCommandLine(argv)

/// Match a parsed command to its handler function.
let routeCommand (results: ParseResults<RootArgs>) (context: Forward.Project.CommandContext) =
  match results.TryGetSubCommand() with
  | Some(Init) -> handleInitCommand context
  | Some(Explain) -> handleExplainCommand context
  | Some(Backup(args)) -> handleBackupCommand context args
  | Some(Restore(args)) -> handleRestoreCommand context args
  | Some(List(args)) -> handleListCommand context args
  | Some(Switch(args)) -> handleSwitchCommand context args
  | Some(Remove(args)) -> handleRemoveCommand context args
  | Some _ -> Error(sprintf "Got invalid subcommand %O" (results.GetAllResults()))
  | None -> Error(sprintf "Got none with %O" (results.GetAllResults()))

let private parseAndExecuteCommand (argv: string[]) =
  let results: ParseResults<RootArgs> = parseCommandLine argv
  let maybeProjectName: string option = results.TryGetResult(Project)
  let maybeRootPath: string option = results.TryGetResult(Root)

  let commandContext: Result<Forward.FileHelpers.CommandFileContext, string> =
    Forward.FileHelpers.buildCommandContext maybeRootPath maybeProjectName

  match Result.bind (routeCommand results) commandContext with
  | Ok unitFunc ->
    unitFunc ()
    0
  | Error(reason: string) ->
    printfn "ERR: %s" reason
    1

/// Main entry point that bootstraps and runs the CLI application.
[<EntryPoint>]
let main (argv: string[]) =
  try
    parseAndExecuteCommand argv
  with (ex: exn) ->
    AnsiConsole.WriteException(ex)
    1
