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

// COMMAND
//   fwd
// ****************************************************************************

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
  | Some(Init) -> context |> handleInitCommand |> formatPrintAndClassify StandardFormat
  | Some(Explain) -> context |> handleExplainCommand |> formatPrintAndClassify StandardFormat
  | Some(Config(args)) -> args |> handleConfigCommand context |> formatPrintAndClassify StandardFormat
  | Some(Backup(args)) -> args |> handleBackupCommand context |> formatPrintAndClassify StandardFormat
  | Some(Restore(args)) -> args |> handleRestoreCommand context |> formatPrintAndClassify StandardFormat
  | Some(List(args)) -> args |> handleListCommand context |> formatPrintAndClassify StandardFormat
  | Some(Switch(args)) -> args |> handleSwitchCommand context |> formatPrintAndClassify StandardFormat
  | Some(Remove(args)) -> args |> handleRemoveCommand context |> formatPrintAndClassify StandardFormat
  | Some _ ->
    results.GetAllResults()
    |> sprintf "Got invalid subcommand %O"
    |> ErrorResult
    |> formatPrintAndClassify StandardFormat
  | None ->
    results.GetAllResults()
    |> sprintf "Got none with %O"
    |> ErrorResult
    |> formatPrintAndClassify StandardFormat

let private parseAndExecuteCommand (argv: string[]) =
  let results: ParseResults<RootArgs> = parseCommandLine argv
  let maybeProjectName: string option = results.TryGetResult(Project)
  let maybeRootPath: string option = results.TryGetResult(Root)

  match Forward.FileHelpers.buildCommandContext maybeRootPath maybeProjectName with
  | Ok(context) -> routeCommand results context
  | Error(reason) -> reason |> ErrorResult |> formatPrintAndClassify StandardFormat

/// Main entry point that bootstraps and runs the CLI application.
[<EntryPoint>]
let main (argv: string[]) =
  try
    parseAndExecuteCommand argv
  with (ex: exn) ->
    AnsiConsole.WriteException(ex)
    1
