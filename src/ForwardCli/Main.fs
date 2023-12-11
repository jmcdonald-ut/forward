open Argu
open Spectre.Console

open ForwardCli.OutputResult
open ForwardCli.Config
open ForwardCli.Db
open ForwardCli.Explain
open ForwardCli.Init
open ForwardCli.List
open ForwardCli.Rm
open ForwardCli.Switch

[<RequireSubcommand>]
type RootArgs =
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Init
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Explain
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Config of ParseResults<ConfigArgs>
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Backup of ParseResults<DbArgs>
  | [<SubCommand; CliPrefix(CliPrefix.None)>] Restore of ParseResults<DbArgs>
  | [<CliPrefix(CliPrefix.None); AltCommandLine("ls")>] List of ParseResults<ListArgs>
  | [<CliPrefix(CliPrefix.None); CustomCommandLine("rm")>] Remove of ParseResults<RemoveArgs>
  | [<CliPrefix(CliPrefix.None); AltCommandLine("s")>] Switch of ParseResults<SwitchArgs>
  | [<CliPrefix(CliPrefix.DoubleDash)>] Project of string
  | [<CliPrefix(CliPrefix.DoubleDash)>] Root of string
  | [<CliPrefix(CliPrefix.DoubleDash)>] Squelch
  | [<CliPrefix(CliPrefix.DoubleDash)>] Format of OutputFormat

  interface IArgParserTemplate with
    member arg.Usage =
      match arg with
      | Init -> "initialize a project."
      | Backup _ -> "backs up a DB."
      | Restore _ -> "restores a DB backup."
      | Config _ -> "gets or sets variables in dotenv file."
      | Explain -> "explains the current context."
      | List _ -> "list project dotenv files."
      | Remove _ -> "remove project dotenv file."
      | Switch _ -> "switch the project's dotenv file."
      // Shared/Root CLI Args
      | Project _ -> "specify the project name."
      | Root _ -> "specify the path to `fwd` artifacts."
      | Squelch -> "squelch errors"
      | Format _ -> "format"

let checkStructure =
#if DEBUG
  true
#else
  false
#endif

// COMMAND
//   fwd
// ****************************************************************************

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

let printAndExit (rootArgs: ParseResults<RootArgs>) (result: CommandResult<'row, 'record>) =
  let format: OutputFormat = rootArgs.GetResult(Format, Standard)
  formatPrintAndClassify format result

/// Match a parsed command to its handler function.
let routeCommand (rootArgs: ParseResults<RootArgs>) (context: Forward.Project.CommandContext) =
  match rootArgs.TryGetSubCommand() with
  | Some(Init) -> context |> handleInitCommand |> printAndExit rootArgs
  | Some(Explain) -> context |> handleExplainCommand |> printAndExit rootArgs
  | Some(Config(args)) -> args |> handleConfigCommand context |> printAndExit rootArgs
  | Some(Backup(args)) -> args |> handleBackupCommand context |> printAndExit rootArgs
  | Some(Restore(args)) -> args |> handleRestoreCommand context |> printAndExit rootArgs
  | Some(List(args)) -> args |> handleListCommand context |> printAndExit rootArgs
  | Some(Switch(args)) -> args |> handleSwitchCommand context |> printAndExit rootArgs
  | Some(Remove(args)) -> args |> handleRemoveCommand context |> printAndExit rootArgs
  | Some _ ->
    rootArgs.GetAllResults()
    |> sprintf "Got invalid subcommand %O"
    |> ErrorResult
    |> printAndExit rootArgs
  | None ->
    rootArgs.GetAllResults()
    |> sprintf "Got none with %O"
    |> ErrorResult
    |> printAndExit rootArgs

let private parseAndExecuteCommand (argv: string[]) =
  let rootArgs: ParseResults<RootArgs> = parseCommandLine argv
  let maybeProjectName: string option = rootArgs.TryGetResult(Project)
  let maybeRootPath: string option = rootArgs.TryGetResult(Root)

  match Forward.FileHelpers.buildCommandContext maybeRootPath maybeProjectName with
  | Ok(context) -> routeCommand rootArgs context
  | Error(reason) -> reason |> ErrorResult |> printAndExit rootArgs

/// Main entry point that bootstraps and runs the CLI application.
[<EntryPoint>]
let main (argv: string[]) =
  try
    parseAndExecuteCommand argv
  with (ex: exn) ->
    AnsiConsole.WriteException(ex)
    1
